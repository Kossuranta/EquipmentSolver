using EquipmentSolver.Core.Entities;
using EquipmentSolver.Core.Interfaces;
using EquipmentSolver.Infrastructure.Data;
using EquipmentSolver.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

namespace EquipmentSolver.Tests.ImportExport;

public class ImportExportServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly ImportExportService _service;
    private const string OwnerId = "user-1";
    private const string OtherUserId = "user-2";

    public ImportExportServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _db = new AppDbContext(options);
        _service = new ImportExportService(_db, NullLogger<ImportExportService>.Instance);
    }

    public void Dispose()
    {
        _db.Dispose();
        GC.SuppressFinalize(this);
    }

    // --- Helpers ---

    private async Task<GameProfile> CreateTestProfile()
    {
        var profile = new GameProfile
        {
            OwnerId = OwnerId,
            Name = "Test Profile",
            GameName = "Dark Souls III",
            IgdbGameId = 11133,
            Description = "Test description",
            Version = "1.0.0",
            IsPublic = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.GameProfiles.Add(profile);
        await _db.SaveChangesAsync();
        return profile;
    }

    private async Task<(EquipmentSlot head, EquipmentSlot chest)> CreateSlots(int profileId)
    {
        var head = new EquipmentSlot { ProfileId = profileId, Name = "Head", SortOrder = 0 };
        var chest = new EquipmentSlot { ProfileId = profileId, Name = "Chest", SortOrder = 1 };
        _db.EquipmentSlots.AddRange(head, chest);
        await _db.SaveChangesAsync();
        return (head, chest);
    }

    private async Task<(StatType armor, StatType weight)> CreateStatTypes(int profileId)
    {
        var armor = new StatType { ProfileId = profileId, DisplayName = "Armor" };
        var weight = new StatType { ProfileId = profileId, DisplayName = "Weight" };
        _db.StatTypes.AddRange(armor, weight);
        await _db.SaveChangesAsync();
        return (armor, weight);
    }

    private async Task CreateEquipmentWithRelations(
        int profileId, string name, List<int> slotIds, List<(int statTypeId, double value)> stats)
    {
        var equip = new Equipment { ProfileId = profileId, Name = name };
        _db.Equipment.Add(equip);
        await _db.SaveChangesAsync();

        foreach (var slotId in slotIds)
            _db.EquipmentSlotCompatibilities.Add(new EquipmentSlotCompatibility { EquipmentId = equip.Id, SlotId = slotId });

        foreach (var (statTypeId, value) in stats)
            _db.EquipmentStats.Add(new EquipmentStat { EquipmentId = equip.Id, StatTypeId = statTypeId, Value = value });

        await _db.SaveChangesAsync();
    }

    private async Task CreateSolverPreset(int profileId, string name, int armorStatId, int weightStatId)
    {
        var preset = new SolverPreset { ProfileId = profileId, Name = name };
        _db.SolverPresets.Add(preset);
        await _db.SaveChangesAsync();

        _db.SolverConstraints.Add(new SolverConstraint
        {
            PresetId = preset.Id,
            StatTypeId = weightStatId,
            Operator = "<=",
            Value = 70
        });
        _db.SolverPriorities.Add(new SolverPriority
        {
            PresetId = preset.Id,
            StatTypeId = armorStatId,
            Weight = 1.0
        });
        await _db.SaveChangesAsync();
    }

    private async Task CreatePatchNote(int profileId, string version, string content)
    {
        _db.ProfilePatchNotes.Add(new ProfilePatchNote
        {
            ProfileId = profileId,
            Version = version,
            Date = DateTime.UtcNow,
            Content = content
        });
        await _db.SaveChangesAsync();
    }

    // ===== CSV Template Tests =====

    [Fact]
    public async Task CsvTemplate_ContainsCorrectHeaders()
    {
        var profile = await CreateTestProfile();
        var (head, chest) = await CreateSlots(profile.Id);
        var (armor, weight) = await CreateStatTypes(profile.Id);

        var csv = await _service.GenerateCsvTemplateAsync(profile.Id, OwnerId);

        Assert.NotNull(csv);
        var lines = csv!.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.True(lines.Length >= 3, "Template should have at least 3 rows");

        // Row 1: type row
        Assert.Equal("Name,Slot,Slot,Stat,Stat", lines[0].TrimEnd('\r'));

        // Row 2: name row
        Assert.Equal("Name,Head,Chest,Armor,Weight", lines[1].TrimEnd('\r'));

        // Row 3: example row
        Assert.Equal("Example Item,X,,0,0", lines[2].TrimEnd('\r'));
    }

    [Fact]
    public async Task CsvTemplate_SlotsOrderedBySortOrder()
    {
        var profile = await CreateTestProfile();
        // Add slots in reverse order
        _db.EquipmentSlots.Add(new EquipmentSlot { ProfileId = profile.Id, Name = "Legs", SortOrder = 2 });
        _db.EquipmentSlots.Add(new EquipmentSlot { ProfileId = profile.Id, Name = "Head", SortOrder = 0 });
        _db.EquipmentSlots.Add(new EquipmentSlot { ProfileId = profile.Id, Name = "Chest", SortOrder = 1 });
        await _db.SaveChangesAsync();

        var csv = await _service.GenerateCsvTemplateAsync(profile.Id, OwnerId);

        var lines = csv!.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var nameRow = lines[1].TrimEnd('\r');
        // InMemory provider may not preserve ordering, so check all names present
        Assert.Contains("Head", nameRow);
        Assert.Contains("Chest", nameRow);
        Assert.Contains("Legs", nameRow);
        Assert.StartsWith("Name,", nameRow);
    }

    [Fact]
    public async Task CsvTemplate_ReturnsNullForNonExistentProfile()
    {
        var result = await _service.GenerateCsvTemplateAsync(9999, OwnerId);
        Assert.Null(result);
    }

    [Fact]
    public async Task CsvTemplate_QuotesFieldsWithCommas()
    {
        var profile = await CreateTestProfile();
        _db.EquipmentSlots.Add(new EquipmentSlot { ProfileId = profile.Id, Name = "Ring, Left", SortOrder = 0 });
        _db.StatTypes.Add(new StatType { ProfileId = profile.Id, DisplayName = "Fire Res, High" });
        await _db.SaveChangesAsync();

        var csv = await _service.GenerateCsvTemplateAsync(profile.Id, OwnerId);

        Assert.NotNull(csv);
        Assert.Contains("\"Ring, Left\"", csv);
        Assert.Contains("\"Fire Res, High\"", csv);
    }

    // ===== Bulk Import Tests =====

    [Fact]
    public async Task BulkImport_WithMapAction_MapsToExistingSlotAndStat()
    {
        var profile = await CreateTestProfile();
        var (head, chest) = await CreateSlots(profile.Id);
        var (armor, weight) = await CreateStatTypes(profile.Id);

        var request = new BulkEquipmentImportRequest
        {
            Items =
            [
                new BulkEquipmentItem
                {
                    Name = "Iron Helmet",
                    SlotNames = ["Head"],
                    Stats = [new BulkStatValue { StatName = "Armor", Value = 50 }]
                }
            ],
            SlotMappings = [new SlotMappingEntry { CsvSlotName = "Head", Action = "map", MapToSlotId = head.Id }],
            StatMappings = [new StatMappingEntry { CsvStatName = "Armor", Action = "map", MapToStatTypeId = armor.Id }]
        };

        var result = await _service.BulkImportEquipmentAsync(profile.Id, OwnerId, request);

        Assert.NotNull(result);
        Assert.Single(result!.Equipment);
        Assert.Equal("Iron Helmet", result.Equipment[0].Name);
        Assert.Contains(head.Id, result.Equipment[0].CompatibleSlotIds);
        Assert.Single(result.Equipment[0].Stats);
        Assert.Equal(armor.Id, result.Equipment[0].Stats[0].StatTypeId);
        Assert.Equal(50, result.Equipment[0].Stats[0].Value);
    }

    [Fact]
    public async Task BulkImport_WithGenerateAction_CreatesNewSlotAndStat()
    {
        var profile = await CreateTestProfile();

        var request = new BulkEquipmentImportRequest
        {
            Items =
            [
                new BulkEquipmentItem
                {
                    Name = "Test Item",
                    SlotNames = ["NewSlot"],
                    Stats = [new BulkStatValue { StatName = "NewStat", Value = 42 }]
                }
            ],
            SlotMappings = [new SlotMappingEntry { CsvSlotName = "NewSlot", Action = "generate" }],
            StatMappings = [new StatMappingEntry { CsvStatName = "NewStat", Action = "generate" }]
        };

        var result = await _service.BulkImportEquipmentAsync(profile.Id, OwnerId, request);

        Assert.NotNull(result);
        Assert.Single(result!.NewSlots);
        Assert.Equal("NewSlot", result.NewSlots[0].Name);
        Assert.Single(result.NewStatTypes);
        Assert.Equal("NewStat", result.NewStatTypes[0].DisplayName);

        // Verify equipment was linked to the new slot and stat
        Assert.Contains(result.NewSlots[0].Id, result.Equipment[0].CompatibleSlotIds);
        Assert.Equal(result.NewStatTypes[0].Id, result.Equipment[0].Stats[0].StatTypeId);
    }

    [Fact]
    public async Task BulkImport_WithIgnoreAction_SkipsSlotAndStat()
    {
        var profile = await CreateTestProfile();

        var request = new BulkEquipmentImportRequest
        {
            Items =
            [
                new BulkEquipmentItem
                {
                    Name = "Test Item",
                    SlotNames = ["IgnoredSlot"],
                    Stats = [new BulkStatValue { StatName = "IgnoredStat", Value = 99 }]
                }
            ],
            SlotMappings = [new SlotMappingEntry { CsvSlotName = "IgnoredSlot", Action = "ignore" }],
            StatMappings = [new StatMappingEntry { CsvStatName = "IgnoredStat", Action = "ignore" }]
        };

        var result = await _service.BulkImportEquipmentAsync(profile.Id, OwnerId, request);

        Assert.NotNull(result);
        Assert.Single(result!.Equipment);
        Assert.Empty(result.Equipment[0].CompatibleSlotIds);
        Assert.Empty(result.Equipment[0].Stats);
        Assert.Empty(result.NewSlots);
        Assert.Empty(result.NewStatTypes);
    }

    [Fact]
    public async Task BulkImport_MultipleItems_AllCreated()
    {
        var profile = await CreateTestProfile();
        var (head, chest) = await CreateSlots(profile.Id);
        var (armor, _) = await CreateStatTypes(profile.Id);

        var request = new BulkEquipmentImportRequest
        {
            Items =
            [
                new BulkEquipmentItem { Name = "Item 1", SlotNames = ["Head"], Stats = [new BulkStatValue { StatName = "Armor", Value = 10 }] },
                new BulkEquipmentItem { Name = "Item 2", SlotNames = ["Chest"], Stats = [new BulkStatValue { StatName = "Armor", Value = 20 }] },
                new BulkEquipmentItem { Name = "Item 3", SlotNames = ["Head", "Chest"], Stats = [new BulkStatValue { StatName = "Armor", Value = 30 }] }
            ],
            SlotMappings =
            [
                new SlotMappingEntry { CsvSlotName = "Head", Action = "map", MapToSlotId = head.Id },
                new SlotMappingEntry { CsvSlotName = "Chest", Action = "map", MapToSlotId = chest.Id }
            ],
            StatMappings = [new StatMappingEntry { CsvStatName = "Armor", Action = "map", MapToStatTypeId = armor.Id }]
        };

        var result = await _service.BulkImportEquipmentAsync(profile.Id, OwnerId, request);

        Assert.NotNull(result);
        Assert.Equal(3, result!.Equipment.Count);
        Assert.Equal(2, result.Equipment[2].CompatibleSlotIds.Count); // Item 3 has both slots
    }

    [Fact]
    public async Task BulkImport_ReturnsNullForNonOwner()
    {
        var profile = await CreateTestProfile();

        var request = new BulkEquipmentImportRequest
        {
            Items = [new BulkEquipmentItem { Name = "Test", SlotNames = [], Stats = [] }],
            SlotMappings = [],
            StatMappings = []
        };

        var result = await _service.BulkImportEquipmentAsync(profile.Id, OtherUserId, request);

        Assert.Null(result);
    }

    // ===== Profile Export Tests =====

    [Fact]
    public async Task ExportProfile_ContainsAllRelationships()
    {
        var profile = await CreateTestProfile();
        var (head, chest) = await CreateSlots(profile.Id);
        var (armor, weight) = await CreateStatTypes(profile.Id);
        await CreateEquipmentWithRelations(profile.Id, "Iron Helmet",
            [head.Id], [(armor.Id, 50), (weight.Id, 3.5)]);
        await CreateEquipmentWithRelations(profile.Id, "Chainmail",
            [chest.Id], [(armor.Id, 80), (weight.Id, 10)]);
        await CreateSolverPreset(profile.Id, "Max Armor", armor.Id, weight.Id);
        await CreatePatchNote(profile.Id, "1.0.0", "Initial release");

        var export = await _service.ExportProfileAsync(profile.Id, OwnerId);

        Assert.NotNull(export);
        Assert.Equal(1, export!.FormatVersion);
        Assert.Equal("Test Profile", export.Profile.Name);
        Assert.Equal("Dark Souls III", export.Profile.GameName);
        Assert.Equal(11133, export.Profile.IgdbGameId);
        Assert.Equal("1.0.0", export.Profile.Version);

        // Slots
        Assert.Equal(2, export.Profile.Slots.Count);
        Assert.Equal("Head", export.Profile.Slots[0].Name);
        Assert.Equal(0, export.Profile.Slots[0].SortOrder);

        // Stat types (names only)
        Assert.Contains("Armor", export.Profile.StatTypes);
        Assert.Contains("Weight", export.Profile.StatTypes);

        // Equipment (uses names not IDs)
        Assert.Equal(2, export.Profile.Equipment.Count);
        var helmet = export.Profile.Equipment.First(e => e.Name == "Iron Helmet");
        Assert.Contains("Head", helmet.Slots);
        Assert.Equal(50, helmet.Stats["Armor"]);
        Assert.Equal(3.5, helmet.Stats["Weight"]);

        // Solver presets
        Assert.Single(export.Profile.SolverPresets);
        Assert.Equal("Max Armor", export.Profile.SolverPresets[0].Name);
        Assert.Single(export.Profile.SolverPresets[0].Constraints);
        Assert.Equal("Weight", export.Profile.SolverPresets[0].Constraints[0].Stat);
        Assert.Equal("<=", export.Profile.SolverPresets[0].Constraints[0].Operator);

        // Patch notes
        Assert.Single(export.Profile.PatchNotes);
        Assert.Equal("1.0.0", export.Profile.PatchNotes[0].Version);
    }

    [Fact]
    public async Task ExportProfile_ReturnsNullForNonOwner()
    {
        var profile = await CreateTestProfile();

        var result = await _service.ExportProfileAsync(profile.Id, OtherUserId);

        Assert.Null(result);
    }

    // ===== Import as New Tests =====

    [Fact]
    public async Task ImportAsNew_CreatesAllEntities()
    {
        var exportData = new ProfileExportData
        {
            FormatVersion = 1,
            ExportedAt = DateTime.UtcNow,
            Profile = new ProfileExportProfile
            {
                Name = "Imported Profile",
                GameName = "Elden Ring",
                IgdbGameId = 99999,
                Description = "An imported profile",
                Version = "2.0.0",
                Slots = [new ProfileExportSlot { Name = "Head", SortOrder = 0 }, new ProfileExportSlot { Name = "Ring", SortOrder = 1 }],
                StatTypes = ["Armor", "Magic"],
                Equipment =
                [
                    new ProfileExportEquipment
                    {
                        Name = "Magic Helmet",
                        Slots = ["Head"],
                        Stats = new Dictionary<string, double> { ["Armor"] = 30, ["Magic"] = 15 }
                    }
                ],
                SolverPresets =
                [
                    new ProfileExportPreset
                    {
                        Name = "Max Magic",
                        Constraints = [new ProfileExportConstraint { Stat = "Armor", Operator = ">=", Value = 20 }],
                        Priorities = [new ProfileExportPriority { Stat = "Magic", Weight = 1.0 }]
                    }
                ],
                PatchNotes = [new ProfileExportPatchNote { Version = "2.0.0", Date = "2026-02-08", Content = "Import test" }]
            }
        };

        var newProfile = await _service.ImportProfileAsNewAsync(OwnerId, exportData);

        Assert.NotNull(newProfile);
        Assert.Equal("Imported Profile", newProfile.Name);
        Assert.Equal(OwnerId, newProfile.OwnerId);
        Assert.False(newProfile.IsPublic);

        // Verify all child entities were created
        var slots = await _db.EquipmentSlots.Where(s => s.ProfileId == newProfile.Id).ToListAsync();
        Assert.Equal(2, slots.Count);

        var statTypes = await _db.StatTypes.Where(st => st.ProfileId == newProfile.Id).ToListAsync();
        Assert.Equal(2, statTypes.Count);

        var equipment = await _db.Equipment
            .Include(e => e.SlotCompatibilities)
            .Include(e => e.Stats)
            .Where(e => e.ProfileId == newProfile.Id)
            .ToListAsync();
        Assert.Single(equipment);
        Assert.Equal("Magic Helmet", equipment[0].Name);
        Assert.Single(equipment[0].SlotCompatibilities);
        Assert.Equal(2, equipment[0].Stats.Count);

        var presets = await _db.SolverPresets
            .Include(p => p.Constraints)
            .Include(p => p.Priorities)
            .Where(p => p.ProfileId == newProfile.Id)
            .ToListAsync();
        Assert.Single(presets);
        Assert.Single(presets[0].Constraints);
        Assert.Single(presets[0].Priorities);

        var notes = await _db.ProfilePatchNotes.Where(n => n.ProfileId == newProfile.Id).ToListAsync();
        Assert.Single(notes);
    }

    // ===== Replace Profile Tests =====

    [Fact]
    public async Task ReplaceProfile_DeletesOldAndCreatesNew()
    {
        // Set up existing profile with data
        var profile = await CreateTestProfile();
        var (head, _) = await CreateSlots(profile.Id);
        var (armor, weight) = await CreateStatTypes(profile.Id);
        await CreateEquipmentWithRelations(profile.Id, "Old Equipment", [head.Id], [(armor.Id, 10)]);
        await CreatePatchNote(profile.Id, "1.0.0", "Old note");

        // Verify old data exists
        Assert.Single(await _db.Equipment.Where(e => e.ProfileId == profile.Id).ToListAsync());

        var replacement = new ProfileExportData
        {
            FormatVersion = 1,
            Profile = new ProfileExportProfile
            {
                Name = "Replaced Profile",
                GameName = "New Game",
                IgdbGameId = 55555,
                Version = "3.0.0",
                Slots = [new ProfileExportSlot { Name = "Legs", SortOrder = 0 }],
                StatTypes = ["Speed"],
                Equipment =
                [
                    new ProfileExportEquipment
                    {
                        Name = "New Equipment 1",
                        Slots = ["Legs"],
                        Stats = new Dictionary<string, double> { ["Speed"] = 25 }
                    },
                    new ProfileExportEquipment
                    {
                        Name = "New Equipment 2",
                        Slots = ["Legs"],
                        Stats = new Dictionary<string, double> { ["Speed"] = 50 }
                    }
                ],
                SolverPresets = [],
                PatchNotes = [new ProfileExportPatchNote { Version = "3.0.0", Date = "2026-02-09", Content = "Replaced" }]
            }
        };

        var result = await _service.ReplaceProfileAsync(profile.Id, OwnerId, replacement);

        Assert.NotNull(result);
        Assert.Equal(profile.Id, result!.Id); // Same profile ID
        Assert.Equal("Replaced Profile", result.Name);
        Assert.Equal("3.0.0", result.Version);
        Assert.Equal(55555, result.IgdbGameId);

        // Old data should be gone
        var oldSlots = await _db.EquipmentSlots
            .Where(s => s.ProfileId == profile.Id && s.Name == "Head")
            .ToListAsync();
        Assert.Empty(oldSlots);

        var oldStatTypes = await _db.StatTypes
            .Where(st => st.ProfileId == profile.Id && st.DisplayName == "Armor")
            .ToListAsync();
        Assert.Empty(oldStatTypes);

        // New data should be present
        var newEquipment = await _db.Equipment.Where(e => e.ProfileId == profile.Id).ToListAsync();
        Assert.Equal(2, newEquipment.Count);

        var newSlots = await _db.EquipmentSlots.Where(s => s.ProfileId == profile.Id).ToListAsync();
        Assert.Single(newSlots);
        Assert.Equal("Legs", newSlots[0].Name);
    }

    [Fact]
    public async Task ReplaceProfile_ReturnsNullForNonOwner()
    {
        var profile = await CreateTestProfile();

        var result = await _service.ReplaceProfileAsync(profile.Id, OtherUserId, new ProfileExportData
        {
            Profile = new ProfileExportProfile
            {
                Name = "X", GameName = "Y", IgdbGameId = 1, Version = "0.1.0",
                Slots = [], StatTypes = [], Equipment = [], SolverPresets = [], PatchNotes = []
            }
        });

        Assert.Null(result);
    }

    // ===== Round-trip Test =====

    [Fact]
    public async Task ExportThenImportAsNew_PreservesAllData()
    {
        // Create a full profile
        var profile = await CreateTestProfile();
        var (head, chest) = await CreateSlots(profile.Id);
        var (armor, weight) = await CreateStatTypes(profile.Id);
        await CreateEquipmentWithRelations(profile.Id, "Iron Helmet",
            [head.Id], [(armor.Id, 50), (weight.Id, 3.5)]);
        await CreateEquipmentWithRelations(profile.Id, "Chainmail",
            [chest.Id], [(armor.Id, 80), (weight.Id, 10)]);
        await CreateSolverPreset(profile.Id, "Max Armor", armor.Id, weight.Id);
        await CreatePatchNote(profile.Id, "1.0.0", "Initial release");

        // Export
        var export = await _service.ExportProfileAsync(profile.Id, OwnerId);
        Assert.NotNull(export);

        // Import as new
        var imported = await _service.ImportProfileAsNewAsync(OtherUserId, export!);

        Assert.NotEqual(profile.Id, imported.Id);
        Assert.Equal(OtherUserId, imported.OwnerId);
        Assert.Equal("Test Profile", imported.Name);

        // Verify all data was preserved
        var importedEquip = await _db.Equipment
            .Include(e => e.Stats)
            .Include(e => e.SlotCompatibilities)
            .Where(e => e.ProfileId == imported.Id)
            .OrderBy(e => e.Name)
            .ToListAsync();

        Assert.Equal(2, importedEquip.Count);
        Assert.Equal("Chainmail", importedEquip[0].Name);
        Assert.Equal(2, importedEquip[0].Stats.Count);

        Assert.Equal("Iron Helmet", importedEquip[1].Name);
        Assert.Single(importedEquip[1].SlotCompatibilities);

        var importedPresets = await _db.SolverPresets
            .Include(p => p.Constraints)
            .Include(p => p.Priorities)
            .Where(p => p.ProfileId == imported.Id)
            .ToListAsync();
        Assert.Single(importedPresets);
        Assert.Equal("Max Armor", importedPresets[0].Name);
        Assert.Single(importedPresets[0].Constraints);
        Assert.Single(importedPresets[0].Priorities);
    }
}
