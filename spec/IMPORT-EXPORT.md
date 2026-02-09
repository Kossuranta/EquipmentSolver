# Import / Export — Implementation Plan

## Overview

Three deferred features implemented as two cohesive capabilities:

1. **Equipment CSV import/export** — bulk manage equipment within a profile, with a stat mapping dialog on import
2. **Profile JSON export/import** — full profile serialization, combined with version rollback (import can replace an existing profile)

```
Equipment CSV flow:

  Export CSV ──► Browser downloads .csv
  Download Template ──► Empty .csv with slot + stat headers
  Import CSV ──► Parse CSV on frontend ──► Import Mapping Dialog (slots + stats) ──► POST bulk equipment to API

Profile JSON flow:

  Export Profile ──► Browser downloads .json
  Import as New ──► Upload .json ──► POST new profile from JSON
  Replace Existing ──► Show backup warning ──► Upload .json ──► PUT replace profile from JSON
```

---

## Feature 1: Equipment CSV Import/Export

### CSV Format (wide format)

Each row = one equipment piece. Slot columns use `X` / empty for compatibility. Stat columns use numeric values (empty = 0).

The CSV has **two header rows**:
- **Row 1 (type row)**: `Name,Slot,Slot,...,Stat,Stat,...` — defines whether each column is a slot or stat
- **Row 2 (name row)**: `Name,Head,Chest,...,Armor,Fire Res,...` — the actual slot/stat names

This removes all ambiguity about which columns are slots vs stats, and allows importing CSVs from other profiles where slot/stat names might overlap or be unfamiliar.

```csv
Name,Slot,Slot,Slot,Slot,Stat,Stat,Stat
Name,Head,Chest,Ring1,Ring2,Armor,Weight,Fire Res
Iron Helmet,X,,,,50,3.5,0
Chainmail,,X,,,80,10,5
Gold Ring,,,X,X,5,0.5,20
```

### CSV Template Download

- Endpoint: `GET /api/v1/profiles/{id}/equipment/csv-template`
- Returns a 3-row CSV: type row, name row, and one example row with placeholder values
- Slots ordered by `SortOrder`, stat types in DB order
- Example row uses `Example Item` as name, `X` in the first slot, and `0` for all stats
- Content-Type: `text/csv` with `Content-Disposition: attachment`

### CSV Parsing Rules

The CSV parser must handle **quoted fields** per RFC 4180:
- Fields containing commas, quotes, or newlines must be wrapped in double quotes
- Double quotes inside a quoted field are escaped as `""` (e.g., `"Fire Res ""high"""`)
- Both export and template generation must quote field values that contain commas
- The import parser must correctly handle quoted fields

This ensures names like `"Armor, Heavy"` or `"Ring of Fire, +1"` work correctly in equipment names, slot names, and stat names.

**Decimal handling**: Stat values must accept both `.` and `,` as decimal separators (e.g., `10.5` and `10,5` are both valid and equivalent). The parser normalizes commas to dots before parsing the number. Note: when a stat value contains a comma (e.g., `10,5`), the CSV field must be quoted to avoid conflict with the CSV delimiter — e.g., `"10,5"`. The template and export always use `.` as the decimal separator.

### CSV Export

Handled entirely on the frontend — the data is already loaded in `ProfileDetailResponse`. Builds CSV from current `equipment`, `slots`, `statTypes` and triggers browser download. Values containing commas are automatically quoted.

### CSV Import — Import Mapping Dialog

When the user imports a CSV file:

1. Frontend parses the CSV (built-in `FileReader` API, no library needed)
2. Row 1 (type row) identifies which columns are `Slot` vs `Stat`
3. Slot columns trigger the **Slot Mapping Dialog** (same concept as stat mapping — see below)
4. Stat columns trigger the **Stat Mapping Dialog**

#### Import Mapping Dialog UI

A single dialog with two sections — **Slots** and **Stats** — each showing the CSV names and how they map to the current profile. If all slots/stats auto-match perfectly, the corresponding section is collapsed or shows a summary.

```
┌──────────────────────────────────────────────────────────────┐
│  Import Equipment — Column Mapping                           │
│                                                              │
│  Found 15 equipment items.                                   │
│                                                              │
│  SLOTS (3 columns)                                           │
│                                                              │
│  CSV Slot Name    │ Action           │ Map To                │
│  ─────────────────┼──────────────────┼───────────────────────│
│  Head             │ [Map to existing▾] │ [Head          ▾]   │
│  Body             │ [Map to existing▾] │ [Chest         ▾]   │
│  Legs             │ [Map to existing▾] │ [Legs          ▾]   │
│                                                              │
│  STATS (4 columns)                                           │
│                                                              │
│  CSV Stat Name    │ Action           │ Map To                │
│  ─────────────────┼──────────────────┼───────────────────────│
│  Armor            │ [Map to existing▾] │ [Armor         ▾]   │
│  Armour           │ [Map to existing▾] │ [Armor         ▾]   │
│  Weight           │ [Map to existing▾] │ [Weight        ▾]   │
│  Luck             │ [Generate       ▾] │ (disabled)          │
│                                                              │
│                              [Cancel]  [Import 15 items]     │
└──────────────────────────────────────────────────────────────┘
```

Both slots and stats use the same three actions per row:

| Action | Behavior (Slots) | Behavior (Stats) |
|--------|-------------------|-------------------|
| **Generate** | Creates a new slot with that name | Creates a new stat type with that name |
| **Map to existing** | Maps to an existing slot (second dropdown active) | Maps to an existing stat type (second dropdown active) |
| **Ignore** | Skips this slot — equipment won't be assigned to it | Skips this stat — equipment imported without it |

#### Bulk mapping prompt

When the user selects "Map to existing" and picks a target from the second dropdown, if there are **other unmapped rows with the same CSV name** (within the same section), the dialog prompts:

> *Map all 3 rows named "Armour" to "Armor"?*  [Yes] [No]

- **Yes** — sets all rows with that CSV name to "Map to existing" with the same target
- **No** — only applies to the current row

This avoids repetitive mapping when the same name appears multiple times (e.g., duplicate column headers from a messy CSV) while still letting the user override individual rows if needed.

#### Fuzzy Matching Logic (sets defaults on dialog open)

The same matching logic applies to both slots and stats:

1. **Exact case-insensitive match** → default to "Map to existing" with that item selected
2. **Close match** (normalize: lowercase, strip spaces/underscores/hyphens, then Levenshtein distance ≤ 2) → default to "Map to existing"
3. **No match** → default to "Generate"

```typescript
function normalizeName(name: string): string {
  return name.toLowerCase().replace(/[\s_-]/g, '');
}

function findBestMatch(csvName: string, existing: { name: string }[]): { name: string } | null {
  const normalized = normalizeName(csvName);
  // Exact normalized match
  const match = existing.find(s => normalizeName(s.name) === normalized);
  if (match) return match;
  // Short names (≤ 4 chars after normalization): only accept exact match (already checked above)
  if (normalized.length <= 4) return null;
  // Longer names: Levenshtein distance ≤ 2 on normalized names
  return existing.find(s => levenshtein(normalizeName(s.name), normalized) <= 2) ?? null;
}
```

### Bulk Import Endpoint

- Endpoint: `POST /api/v1/profiles/{id}/equipment/import`
- Accepts JSON (not multipart — CSV is parsed on frontend):

```csharp
public class BulkEquipmentImportRequest
{
    public List<BulkEquipmentItem> Items { get; set; }
    public List<SlotMapping> SlotMappings { get; set; }
    public List<StatMapping> StatMappings { get; set; }
}

public class BulkEquipmentItem
{
    public string Name { get; set; }
    public List<string> SlotNames { get; set; }      // original CSV slot names
    public List<BulkStatValue> Stats { get; set; }
}

public class BulkStatValue
{
    public string StatName { get; set; }              // original CSV stat name
    public double Value { get; set; }
}

public class SlotMapping
{
    public string CsvSlotName { get; set; }
    public string Action { get; set; }                // "generate", "map", "ignore"
    public int? MapToSlotId { get; set; }             // only when Action = "map"
}

public class StatMapping
{
    public string CsvStatName { get; set; }
    public string Action { get; set; }                // "generate", "map", "ignore"
    public int? MapToStatTypeId { get; set; }         // only when Action = "map"
}
```

Backend logic:
1. Process slot mappings: create new slots for "generate", resolve IDs for "map", skip "ignore"
2. Process stat mappings: create new stat types for "generate", resolve IDs for "map", skip "ignore"
3. Create all equipment records in a single transaction
4. Return created `EquipmentDto[]` + any new `SlotDto[]` + any new `StatTypeDto[]`

### Equipment Tab UI Changes

Add buttons next to "Add Equipment" in `profile-equipment-tab`:

- **Import CSV** — opens file picker → parses → mapping dialog (slots + stats) → bulk import
- **Export CSV** — builds CSV from loaded data, triggers download
- **Download Template** — calls template endpoint, triggers download

---

## Feature 2: Profile JSON Export/Import (with Rollback)

### JSON Format

Uses **names instead of IDs** so the export is portable and human-readable:

```json
{
  "formatVersion": 1,
  "exportedAt": "2026-02-08T12:00:00Z",
  "profile": {
    "name": "Dark Souls 3 — PvP",
    "gameName": "Dark Souls III",
    "igdbGameId": 11133,
    "gameCoverUrl": "https://...",
    "description": "Optimized PvP build",
    "version": "1.2.0",
    "slots": [
      { "name": "Head", "sortOrder": 0 },
      { "name": "Chest", "sortOrder": 1 }
    ],
    "statTypes": ["Armor", "Weight", "Fire Res"],
    "equipment": [
      {
        "name": "Iron Helmet",
        "slots": ["Head"],
        "stats": { "Armor": 50, "Weight": 3.5 }
      }
    ],
    "solverPresets": [
      {
        "name": "Max Armor",
        "constraints": [
          { "stat": "Weight", "operator": "<=", "value": 70 }
        ],
        "priorities": [
          { "stat": "Armor", "weight": 1.0 }
        ]
      }
    ],
    "patchNotes": [
      { "version": "0.1.0", "date": "2026-02-07", "content": "Initial release" }
    ]
  }
}
```

### Export Endpoint

- `GET /api/v1/profiles/{id}/export`
- Owner-only
- Loads full profile with all relationships
- Serializes to JSON format above using names (not IDs)
- Returns `application/json` with `Content-Disposition: attachment`

### Import as New Endpoint

- `POST /api/v1/profiles/import`
- Accepts JSON body (the export format)
- Creates a new profile owned by the current user
- Creates all child entities (slots, stat types, equipment, presets, patch notes)
- Uses name-based mapping (same strategy as `SocialService.CopyProfileAsync`)
- Returns new profile ID + name

### Replace Existing Endpoint

- `PUT /api/v1/profiles/{id}/import`
- Owner-only
- In a single transaction:
  1. Clean up user states (equipment states, slot states) for all users of this profile
  2. Delete all child data (equipment, presets, slots, stat types, patch notes)
  3. Re-create everything from the JSON
  4. Update profile metadata (name, description, version, game info) from JSON
- Returns updated `ProfileDetailResponse`

### Profile Import Dialog UI (Replace mode)

```
┌──────────────────────────────────────────────────────────────┐
│  Replace Profile                                             │
│                                                              │
│  ⚠ This will replace all data in this profile with the       │
│  imported file. This includes equipment, slots, stat types,  │
│  solver presets, and patch notes.                             │
│                                                              │
│  It is recommended to export the current version as a backup │
│  before replacing.                                           │
│                                                              │
│  [Export Current Version]                                     │
│                                                              │
│  ──────────────────────────────────────────────────────────── │
│                                                              │
│  Select file to import:                                      │
│  [Choose File]  profile-backup.json                          │
│                                                              │
│                              [Cancel]  [Replace Profile]     │
└──────────────────────────────────────────────────────────────┘
```

### UI Placement

**Profile editor header**: Menu button (`more_vert` icon) with:
- Export Profile (JSON)
- Import / Replace Profile (JSON)

**Dashboard page**: "Import Profile" button next to "Create Profile":
- Opens file picker, uploads JSON, navigates to the new profile on success

---

## New Files

### Backend

| File | Purpose |
|------|---------|
| `Core/Interfaces/IImportExportService.cs` | Service interface |
| `Infrastructure/Services/ImportExportService.cs` | CSV + JSON import/export logic |
| `Api/Controllers/ImportExportController.cs` | All 5 endpoints |
| `Api/DTOs/ImportExport/BulkEquipmentImportRequest.cs` | Bulk import request DTO |
| `Api/DTOs/ImportExport/BulkImportResponse.cs` | Bulk import response DTO |
| `Api/DTOs/ImportExport/ProfileExportDto.cs` | JSON export/import format DTO |

### Frontend

| File | Purpose |
|------|---------|
| `utils/csv.utils.ts` | CSV parse/generate, file download, Levenshtein |
| `components/import-mapping-dialog/` | Slot + stat mapping dialog for CSV import |
| `components/profile-import-dialog/` | Profile replace dialog with backup warning |

### Modified Files

| File | Change |
|------|--------|
| `profile-equipment-tab.component.*` | Add Import/Export/Template buttons |
| `profile-editor.page.*` | Add export/import menu in header |
| `dashboard.page.*` | Add "Import Profile" button |
| `profile.service.ts` | Add import/export API methods |
| `profile.models.ts` | Add new DTOs |

---

## No Database Migrations Needed

All features use existing entities. New stat types and equipment are created through existing entity types. No schema changes required.

---

## Implementation Order

| Step | What |
|------|------|
| 1 | Core interface + DTOs |
| 2 | CSV template + bulk import service |
| 3 | Profile export/import service |
| 4 | API controller with all 5 endpoints |
| 5 | Frontend CSV utils + stat matching |
| 6 | Import Mapping Dialog (slots + stats) |
| 7 | Equipment tab — export/import/template buttons |
| 8 | Profile Import Dialog component |
| 9 | Profile editor — export/import menu |
| 10 | Dashboard — import as new button |
| 11 | Update specs (PROGRESS.md, README.md, INDEX.md) |

---

## Test Plan

**Backend (xUnit)**:
- CSV template generation (correct headers, ordering)
- Bulk import with stat mappings: generate, map, ignore
- Profile export serialization (all relationships, names not IDs)
- Import as new (verify all entities created)
- Replace existing (verify old data deleted, new data created, transaction safety)

**Frontend (manual)**:
- Download CSV template → headers match profile slots + stats
- Export equipment CSV → re-import → stat mapping dialog appears
- Fuzzy matching: "armour" auto-maps to "Armor", unknown stat defaults to Generate
- Export profile JSON → import as new → verify all data preserved
- Replace profile with exported JSON → verify data replaced
- Replace flow shows backup warning, "Export Current Version" button works
- Import invalid file → meaningful error messages
