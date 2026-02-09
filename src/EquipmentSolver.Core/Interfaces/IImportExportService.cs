using EquipmentSolver.Core.Entities;

namespace EquipmentSolver.Core.Interfaces;

/// <summary>
/// Handles CSV equipment import/export and JSON profile import/export.
/// </summary>
public interface IImportExportService
{
    /// <summary>
    /// Generate a CSV template for a profile (type row, name row, example row).
    /// </summary>
    Task<string?> GenerateCsvTemplateAsync(int profileId, string userId);

    /// <summary>
    /// Bulk import equipment from parsed CSV data with slot/stat mappings.
    /// </summary>
    Task<BulkImportResult?> BulkImportEquipmentAsync(int profileId, string userId, BulkEquipmentImportRequest request);

    /// <summary>
    /// Export a full profile as a portable JSON-serializable object.
    /// </summary>
    Task<ProfileExportData?> ExportProfileAsync(int profileId, string userId);

    /// <summary>
    /// Import a profile from JSON data, creating a new profile for the user.
    /// </summary>
    Task<GameProfile> ImportProfileAsNewAsync(string userId, ProfileExportData data);

    /// <summary>
    /// Replace an existing profile with imported JSON data.
    /// </summary>
    Task<GameProfile?> ReplaceProfileAsync(int profileId, string userId, ProfileExportData data);
}

// --- Request/Response models used by the service ---

public class BulkEquipmentImportRequest
{
    public List<BulkEquipmentItem> Items { get; set; } = [];
    public List<SlotMappingEntry> SlotMappings { get; set; } = [];
    public List<StatMappingEntry> StatMappings { get; set; } = [];
}

public class BulkEquipmentItem
{
    public string Name { get; set; } = null!;
    public List<string> SlotNames { get; set; } = [];
    public List<BulkStatValue> Stats { get; set; } = [];
}

public class BulkStatValue
{
    public string StatName { get; set; } = null!;
    public double Value { get; set; }
}

public class SlotMappingEntry
{
    public string CsvSlotName { get; set; } = null!;
    /// <summary>"generate", "map", or "ignore"</summary>
    public string Action { get; set; } = null!;
    public int? MapToSlotId { get; set; }
}

public class StatMappingEntry
{
    public string CsvStatName { get; set; } = null!;
    /// <summary>"generate", "map", or "ignore"</summary>
    public string Action { get; set; } = null!;
    public int? MapToStatTypeId { get; set; }
}

public class BulkImportResult
{
    public List<EquipmentImportedDto> Equipment { get; set; } = [];
    public List<SlotCreatedDto> NewSlots { get; set; } = [];
    public List<StatTypeCreatedDto> NewStatTypes { get; set; } = [];
}

public class EquipmentImportedDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public List<int> CompatibleSlotIds { get; set; } = [];
    public List<EquipmentStatImportedDto> Stats { get; set; } = [];
}

public class EquipmentStatImportedDto
{
    public int StatTypeId { get; set; }
    public double Value { get; set; }
}

public class SlotCreatedDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public int SortOrder { get; set; }
}

public class StatTypeCreatedDto
{
    public int Id { get; set; }
    public string DisplayName { get; set; } = null!;
}

/// <summary>
/// Portable profile export format using names instead of IDs.
/// </summary>
public class ProfileExportData
{
    public int FormatVersion { get; set; } = 1;
    public DateTime ExportedAt { get; set; } = DateTime.UtcNow;
    public ProfileExportProfile Profile { get; set; } = null!;
}

public class ProfileExportProfile
{
    public string Name { get; set; } = null!;
    public string GameName { get; set; } = null!;
    public int IgdbGameId { get; set; }
    public string? GameCoverUrl { get; set; }
    public string? Description { get; set; }
    public string Version { get; set; } = null!;
    public List<ProfileExportSlot> Slots { get; set; } = [];
    public List<string> StatTypes { get; set; } = [];
    public List<ProfileExportEquipment> Equipment { get; set; } = [];
    public List<ProfileExportPreset> SolverPresets { get; set; } = [];
    public List<ProfileExportPatchNote> PatchNotes { get; set; } = [];
}

public class ProfileExportSlot
{
    public string Name { get; set; } = null!;
    public int SortOrder { get; set; }
}

public class ProfileExportEquipment
{
    public string Name { get; set; } = null!;
    public List<string> Slots { get; set; } = [];
    public Dictionary<string, double> Stats { get; set; } = [];
}

public class ProfileExportPreset
{
    public string Name { get; set; } = null!;
    public List<ProfileExportConstraint> Constraints { get; set; } = [];
    public List<ProfileExportPriority> Priorities { get; set; } = [];
}

public class ProfileExportConstraint
{
    public string Stat { get; set; } = null!;
    public string Operator { get; set; } = null!;
    public double Value { get; set; }
}

public class ProfileExportPriority
{
    public string Stat { get; set; } = null!;
    public double Weight { get; set; }
}

public class ProfileExportPatchNote
{
    public string Version { get; set; } = null!;
    public string Date { get; set; } = null!;
    public string Content { get; set; } = null!;
}
