using System.Globalization;
using System.Text;
using EquipmentSolver.Core.Entities;
using EquipmentSolver.Core.Interfaces;
using EquipmentSolver.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EquipmentSolver.Infrastructure.Services;

/// <summary>
/// Handles CSV equipment import/export and JSON profile import/export.
/// </summary>
public class ImportExportService : IImportExportService
{
    private readonly AppDbContext _db;
    private readonly ILogger<ImportExportService> _logger;

    public ImportExportService(AppDbContext db, ILogger<ImportExportService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string?> GenerateCsvTemplateAsync(int profileId, string userId)
    {
        var profile = await _db.GameProfiles
            .Include(p => p.Slots.OrderBy(s => s.SortOrder))
            .Include(p => p.StatTypes)
            .FirstOrDefaultAsync(p => p.Id == profileId);

        if (profile is null)
            return null;

        // Verify access: owner or using
        if (profile.OwnerId != userId)
        {
            var isUsing = await _db.ProfileUsages
                .AnyAsync(u => u.UserId == userId && u.ProfileId == profileId);
            if (!isUsing && !profile.IsPublic)
                return null;
        }

        var sb = new StringBuilder();

        // Row 1: type row
        sb.Append(CsvQuote("Name"));
        foreach (var _ in profile.Slots)
            sb.Append(',').Append(CsvQuote("Slot"));
        foreach (var _ in profile.StatTypes)
            sb.Append(',').Append(CsvQuote("Stat"));
        sb.AppendLine();

        // Row 2: name row
        sb.Append(CsvQuote("Name"));
        foreach (var slot in profile.Slots)
            sb.Append(',').Append(CsvQuote(slot.Name));
        foreach (var st in profile.StatTypes)
            sb.Append(',').Append(CsvQuote(st.DisplayName));
        sb.AppendLine();

        // Row 3: example row
        sb.Append(CsvQuote("Example Item"));
        for (int i = 0; i < profile.Slots.Count; i++)
            sb.Append(',').Append(i == 0 ? "X" : "");
        foreach (var _ in profile.StatTypes)
            sb.Append(',').Append('0');
        sb.AppendLine();

        return sb.ToString();
    }

    /// <inheritdoc />
    public async Task<BulkImportResult?> BulkImportEquipmentAsync(
        int profileId, string userId, BulkEquipmentImportRequest request)
    {
        var profile = await _db.GameProfiles
            .Include(p => p.Slots.OrderBy(s => s.SortOrder))
            .Include(p => p.StatTypes)
            .FirstOrDefaultAsync(p => p.Id == profileId && p.OwnerId == userId);

        if (profile is null)
            return null;

        var result = new BulkImportResult();

        // Process slot mappings: build csvName → slotId dictionary
        var slotNameToId = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        int maxSortOrder = profile.Slots.Count > 0 ? profile.Slots.Max(s => s.SortOrder) : -1;

        foreach (var mapping in request.SlotMappings)
        {
            switch (mapping.Action.ToLowerInvariant())
            {
                case "generate":
                    maxSortOrder++;
                    var newSlot = new EquipmentSlot
                    {
                        ProfileId = profileId,
                        Name = mapping.CsvSlotName,
                        SortOrder = maxSortOrder
                    };
                    _db.EquipmentSlots.Add(newSlot);
                    await _db.SaveChangesAsync();
                    slotNameToId[mapping.CsvSlotName] = newSlot.Id;
                    result.NewSlots.Add(new SlotCreatedDto
                    {
                        Id = newSlot.Id,
                        Name = newSlot.Name,
                        SortOrder = newSlot.SortOrder
                    });
                    break;

                case "map":
                    if (mapping.MapToSlotId.HasValue)
                        slotNameToId[mapping.CsvSlotName] = mapping.MapToSlotId.Value;
                    break;

                // "ignore" — don't add to the dictionary
            }
        }

        // Process stat mappings: build csvName → statTypeId dictionary
        var statNameToId = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var mapping in request.StatMappings)
        {
            switch (mapping.Action.ToLowerInvariant())
            {
                case "generate":
                    var newStat = new StatType
                    {
                        ProfileId = profileId,
                        DisplayName = mapping.CsvStatName
                    };
                    _db.StatTypes.Add(newStat);
                    await _db.SaveChangesAsync();
                    statNameToId[mapping.CsvStatName] = newStat.Id;
                    result.NewStatTypes.Add(new StatTypeCreatedDto
                    {
                        Id = newStat.Id,
                        DisplayName = newStat.DisplayName
                    });
                    break;

                case "map":
                    if (mapping.MapToStatTypeId.HasValue)
                        statNameToId[mapping.CsvStatName] = mapping.MapToStatTypeId.Value;
                    break;
            }
        }

        // Create equipment records
        foreach (var item in request.Items)
        {
            var equipment = new Equipment
            {
                ProfileId = profileId,
                Name = item.Name
            };
            _db.Equipment.Add(equipment);
            await _db.SaveChangesAsync();

            var importedEquip = new EquipmentImportedDto
            {
                Id = equipment.Id,
                Name = equipment.Name
            };

            // Slot compatibilities
            foreach (var slotName in item.SlotNames)
            {
                if (slotNameToId.TryGetValue(slotName, out int slotId))
                {
                    _db.EquipmentSlotCompatibilities.Add(new EquipmentSlotCompatibility
                    {
                        EquipmentId = equipment.Id,
                        SlotId = slotId
                    });
                    importedEquip.CompatibleSlotIds.Add(slotId);
                }
            }

            // Stats
            foreach (var stat in item.Stats)
            {
                if (statNameToId.TryGetValue(stat.StatName, out int statTypeId))
                {
                    _db.EquipmentStats.Add(new EquipmentStat
                    {
                        EquipmentId = equipment.Id,
                        StatTypeId = statTypeId,
                        Value = stat.Value
                    });
                    importedEquip.Stats.Add(new EquipmentStatImportedDto
                    {
                        StatTypeId = statTypeId,
                        Value = stat.Value
                    });
                }
            }

            result.Equipment.Add(importedEquip);
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Bulk imported {Count} equipment items into profile {ProfileId} (new slots: {SlotCount}, new stats: {StatCount})",
            result.Equipment.Count, profileId, result.NewSlots.Count, result.NewStatTypes.Count);

        return result;
    }

    /// <inheritdoc />
    public async Task<ProfileExportData?> ExportProfileAsync(int profileId, string userId)
    {
        var profile = await _db.GameProfiles
            .Include(p => p.Slots.OrderBy(s => s.SortOrder))
            .Include(p => p.StatTypes)
            .Include(p => p.Equipment)
                .ThenInclude(e => e.Stats)
            .Include(p => p.Equipment)
                .ThenInclude(e => e.SlotCompatibilities)
            .Include(p => p.SolverPresets)
                .ThenInclude(sp => sp.Constraints)
            .Include(p => p.SolverPresets)
                .ThenInclude(sp => sp.Priorities)
            .Include(p => p.PatchNotes.OrderByDescending(pn => pn.Date))
            .FirstOrDefaultAsync(p => p.Id == profileId && p.OwnerId == userId);

        if (profile is null)
            return null;

        // Build name lookup maps
        var slotIdToName = profile.Slots.ToDictionary(s => s.Id, s => s.Name);
        var statIdToName = profile.StatTypes.ToDictionary(st => st.Id, st => st.DisplayName);

        return new ProfileExportData
        {
            FormatVersion = 1,
            ExportedAt = DateTime.UtcNow,
            Profile = new ProfileExportProfile
            {
                Name = profile.Name,
                GameName = profile.GameName,
                IgdbGameId = profile.IgdbGameId,
                GameCoverUrl = profile.GameCoverUrl,
                Description = profile.Description,
                Version = profile.Version,
                Slots = profile.Slots.Select(s => new ProfileExportSlot
                {
                    Name = s.Name,
                    SortOrder = s.SortOrder
                }).ToList(),
                StatTypes = profile.StatTypes.Select(st => st.DisplayName).ToList(),
                Equipment = profile.Equipment.Select(e => new ProfileExportEquipment
                {
                    Name = e.Name,
                    Slots = e.SlotCompatibilities
                        .Where(sc => slotIdToName.ContainsKey(sc.SlotId))
                        .Select(sc => slotIdToName[sc.SlotId])
                        .ToList(),
                    Stats = e.Stats
                        .Where(es => statIdToName.ContainsKey(es.StatTypeId))
                        .ToDictionary(es => statIdToName[es.StatTypeId], es => es.Value)
                }).ToList(),
                SolverPresets = profile.SolverPresets.Select(sp => new ProfileExportPreset
                {
                    Name = sp.Name,
                    Constraints = sp.Constraints
                        .Where(c => statIdToName.ContainsKey(c.StatTypeId))
                        .Select(c => new ProfileExportConstraint
                        {
                            Stat = statIdToName[c.StatTypeId],
                            Operator = c.Operator,
                            Value = c.Value
                        }).ToList(),
                    Priorities = sp.Priorities
                        .Where(p => statIdToName.ContainsKey(p.StatTypeId))
                        .Select(p => new ProfileExportPriority
                        {
                            Stat = statIdToName[p.StatTypeId],
                            Weight = p.Weight
                        }).ToList()
                }).ToList(),
                PatchNotes = profile.PatchNotes.Select(pn => new ProfileExportPatchNote
                {
                    Version = pn.Version,
                    Date = pn.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    Content = pn.Content
                }).ToList()
            }
        };
    }

    /// <inheritdoc />
    public async Task<GameProfile> ImportProfileAsNewAsync(string userId, ProfileExportData data)
    {
        var p = data.Profile;

        var newProfile = new GameProfile
        {
            OwnerId = userId,
            Name = p.Name,
            GameName = p.GameName,
            IgdbGameId = p.IgdbGameId,
            GameCoverUrl = p.GameCoverUrl,
            Description = p.Description,
            Version = p.Version,
            IsPublic = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.GameProfiles.Add(newProfile);
        await _db.SaveChangesAsync();

        await PopulateProfileFromExport(newProfile.Id, p);

        _logger.LogInformation("User {UserId} imported new profile {ProfileId} '{Name}'",
            userId, newProfile.Id, newProfile.Name);

        return newProfile;
    }

    /// <inheritdoc />
    public async Task<GameProfile?> ReplaceProfileAsync(int profileId, string userId, ProfileExportData data)
    {
        var profile = await _db.GameProfiles
            .FirstOrDefaultAsync(p => p.Id == profileId && p.OwnerId == userId);

        if (profile is null)
            return null;

        await using var transaction = await _db.Database.BeginTransactionAsync();

        try
        {
            // 1. Clean up user states for all users of this profile
            var equipStates = await _db.UserEquipmentStates
                .Where(s => s.Equipment.ProfileId == profileId)
                .ToListAsync();
            _db.UserEquipmentStates.RemoveRange(equipStates);

            var slotStates = await _db.UserSlotStates
                .Where(s => s.Slot.ProfileId == profileId)
                .ToListAsync();
            _db.UserSlotStates.RemoveRange(slotStates);

            // 2. Delete all child data
            var presets = await _db.SolverPresets
                .Include(sp => sp.Constraints)
                .Include(sp => sp.Priorities)
                .Where(sp => sp.ProfileId == profileId)
                .ToListAsync();
            foreach (var preset in presets)
            {
                _db.SolverConstraints.RemoveRange(preset.Constraints);
                _db.SolverPriorities.RemoveRange(preset.Priorities);
            }
            _db.SolverPresets.RemoveRange(presets);

            var equipment = await _db.Equipment
                .Include(e => e.Stats)
                .Include(e => e.SlotCompatibilities)
                .Where(e => e.ProfileId == profileId)
                .ToListAsync();
            foreach (var e in equipment)
            {
                _db.EquipmentStats.RemoveRange(e.Stats);
                _db.EquipmentSlotCompatibilities.RemoveRange(e.SlotCompatibilities);
            }
            _db.Equipment.RemoveRange(equipment);

            var patchNotes = await _db.ProfilePatchNotes
                .Where(pn => pn.ProfileId == profileId)
                .ToListAsync();
            _db.ProfilePatchNotes.RemoveRange(patchNotes);

            var slots = await _db.EquipmentSlots
                .Where(s => s.ProfileId == profileId)
                .ToListAsync();
            _db.EquipmentSlots.RemoveRange(slots);

            var statTypes = await _db.StatTypes
                .Where(st => st.ProfileId == profileId)
                .ToListAsync();
            _db.StatTypes.RemoveRange(statTypes);

            await _db.SaveChangesAsync();

            // 3. Re-create everything from JSON
            var p = data.Profile;
            await PopulateProfileFromExport(profileId, p);

            // 4. Update profile metadata
            profile.Name = p.Name;
            profile.GameName = p.GameName;
            profile.IgdbGameId = p.IgdbGameId;
            profile.GameCoverUrl = p.GameCoverUrl;
            profile.Description = p.Description;
            profile.Version = p.Version;
            profile.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("User {UserId} replaced profile {ProfileId} from import",
                userId, profileId);

            return profile;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Populates a profile with slots, stat types, equipment, presets, and patch notes from export data.
    /// </summary>
    private async Task PopulateProfileFromExport(int profileId, ProfileExportProfile data)
    {
        // Create slots
        var slotNameToId = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var slotData in data.Slots)
        {
            var slot = new EquipmentSlot
            {
                ProfileId = profileId,
                Name = slotData.Name,
                SortOrder = slotData.SortOrder
            };
            _db.EquipmentSlots.Add(slot);
            await _db.SaveChangesAsync();
            slotNameToId[slot.Name] = slot.Id;
        }

        // Create stat types
        var statNameToId = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var statName in data.StatTypes)
        {
            var statType = new StatType
            {
                ProfileId = profileId,
                DisplayName = statName
            };
            _db.StatTypes.Add(statType);
            await _db.SaveChangesAsync();
            statNameToId[statName] = statType.Id;
        }

        // Create equipment
        foreach (var equipData in data.Equipment)
        {
            var equip = new Equipment
            {
                ProfileId = profileId,
                Name = equipData.Name
            };
            _db.Equipment.Add(equip);
            await _db.SaveChangesAsync();

            foreach (var slotName in equipData.Slots)
            {
                if (slotNameToId.TryGetValue(slotName, out int slotId))
                {
                    _db.EquipmentSlotCompatibilities.Add(new EquipmentSlotCompatibility
                    {
                        EquipmentId = equip.Id,
                        SlotId = slotId
                    });
                }
            }

            foreach (var (statName, value) in equipData.Stats)
            {
                if (statNameToId.TryGetValue(statName, out int statTypeId))
                {
                    _db.EquipmentStats.Add(new EquipmentStat
                    {
                        EquipmentId = equip.Id,
                        StatTypeId = statTypeId,
                        Value = value
                    });
                }
            }
        }

        await _db.SaveChangesAsync();

        // Create solver presets
        foreach (var presetData in data.SolverPresets)
        {
            var preset = new SolverPreset
            {
                ProfileId = profileId,
                Name = presetData.Name
            };
            _db.SolverPresets.Add(preset);
            await _db.SaveChangesAsync();

            foreach (var c in presetData.Constraints)
            {
                if (statNameToId.TryGetValue(c.Stat, out int statTypeId))
                {
                    _db.SolverConstraints.Add(new SolverConstraint
                    {
                        PresetId = preset.Id,
                        StatTypeId = statTypeId,
                        Operator = c.Operator,
                        Value = c.Value
                    });
                }
            }

            foreach (var p in presetData.Priorities)
            {
                if (statNameToId.TryGetValue(p.Stat, out int statTypeId))
                {
                    _db.SolverPriorities.Add(new SolverPriority
                    {
                        PresetId = preset.Id,
                        StatTypeId = statTypeId,
                        Weight = p.Weight
                    });
                }
            }
        }

        // Create patch notes
        foreach (var noteData in data.PatchNotes)
        {
            DateTime.TryParse(noteData.Date, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal, out var noteDate);

            _db.ProfilePatchNotes.Add(new ProfilePatchNote
            {
                ProfileId = profileId,
                Version = noteData.Version,
                Date = noteDate != default ? noteDate.ToUniversalTime() : DateTime.UtcNow,
                Content = noteData.Content
            });
        }

        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Quote a CSV field if it contains commas, quotes, or newlines (RFC 4180).
    /// </summary>
    private static string CsvQuote(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
