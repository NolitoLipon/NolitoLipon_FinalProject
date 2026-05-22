# Vehicle Service Records Management System
### CS Console Application — File-Based Record Management

---

## System: Vehicle Service Records (Option 5)

A C# .NET 4.5 console application that manages vehicle service records using **file handling only** — no database, no GUI, no web framework.

---

## Features

| Requirement | Implementation |
|---|---|
| Persistent storage | Pipe-delimited `.dat` file; automatic backup `.bak` on every write |
| Recovery after restart | Records loaded from file at startup; malformed lines are skipped with warnings |
| Validation | Plate format, owner name, service type (enum-based), cost range, date range |
| Unique ID | `VSR-00001` format, auto-incremented from existing records |
| Checksum | Custom polynomial hash over key fields; verified on load and after every write |
| Soft delete | `IsActive = false`; record kept in file, excluded from views |
| Hard delete | Permanent removal from file (Advanced Options menu) |
| Restore | Reactivate a soft-deleted record (Advanced Options menu) |
| Audit log | Every Add/Update/Delete/Read/Error/System event appended to `audit.log` |
| Reports | 4 report types saved to timestamped `.txt` files in `reports/` folder |
| Error handling | Try/catch on every I/O operation; malformed lines are skipped gracefully |

---

## Record Model

| Field | Type | Description |
|---|---|---|
| `RecordId` | string | Auto-assigned, format `VSR-NNNNN` |
| `VehiclePlate` | string | 2–10 alphanumeric chars (domain field 1) |
| `OwnerName` | string | 2–80 characters (domain field 2) |
| `ServiceType` | string | Enum: oil change, brake service, etc. (domain field 3) |
| `ServiceCost` | decimal | 0 – 9,999,999 (domain field 4) |
| `Technician` | string | 2–60 characters (domain field 5) |
| `Notes` | string | Optional free text (domain field 6) |
| `ServiceDate` | DateTime | 1990-01-01 to today (domain field 7) |
| `CreatedAt` | DateTime | Set on insert |
| `UpdatedAt` | DateTime | Refreshed on every update |
| `IsActive` | bool | `false` = soft-deleted |
| `Checksum` | string | 8-digit hex hash of key fields |

---

## Project Structure

```
VehicleServiceRecords.sln
VehicleServiceRecords/
├── Program.cs                      ← Menu controller (all menu actions)
├── VehicleServiceRecords.csproj    ← VS2012-compatible project file
├── Properties/
│   └── AssemblyInfo.cs
├── Models/
│   └── ServiceRecord.cs            ← Record model + serialization
├── Services/
│   ├── AuditLogger.cs              ← Append-only audit log writer
│   ├── Validator.cs                ← Input validation (all fields)
│   ├── RecordRepository.cs         ← File I/O: load, save, CRUD
│   └── ReportGenerator.cs          ← 4 report types → .txt files
└── Helpers/
    ├── ConsoleHelper.cs            ← UI formatting, prompts, colors
    └── StorageInitializer.cs       ← Folder/file creation at startup
```

---

## Components (as required)

| Requirement | Class |
|---|---|
| Program / Menu Controller | `Program.cs` |
| Validation Component | `Services/Validator.cs` |
| File Repository / Data Service | `Services/RecordRepository.cs` |
| Report Generator | `Services/ReportGenerator.cs` |
| Audit Logger | `Services/AuditLogger.cs` |

---

## Main Menu

```
============================================================
  VEHICLE SERVICE RECORDS MANAGEMENT SYSTEM
============================================================

   [1]  Add Service Record
   [2]  View / Search Records
   [3]  Update Record
   [4]  Delete Record (Soft)
   [5]  Reports
   [6]  View Audit Log
   [7]  Advanced Options
   [0]  Exit
```

### View / Search Sub-menu
```
   [1]  View All Active Records
   [2]  Search by Vehicle Plate
   [3]  Search by Owner Name
   [4]  Filter by Service Type
   [5]  View Record Detail (by ID)
   [6]  View Inactive / Deleted Records
```

### Reports Sub-menu
```
   [1]  Service Type Summary       (count + total cost per type)
   [2]  Monthly Revenue Report     (revenue + avg cost per month)
   [3]  Top 10 Vehicles by Spend   (ranked by total service cost)
   [4]  Full Active Records Listing
```

### Advanced Options Sub-menu
```
   [1]  Hard Delete Record (PERMANENT)
   [2]  Restore Soft-Deleted Record
   [3]  Verify All Checksums
   [4]  Show Data File Path
```

---

## Data Storage

All data is saved in the user's **Documents** folder:

```
%USERPROFILE%\Documents\VehicleServiceRecords\
├── data\
│   ├── records.dat     ← main data file (pipe-delimited)
│   ├── records.bak     ← automatic backup (overwritten on each save)
│   └── audit.log       ← append-only audit trail
└── reports\
    ├── service_type_summary_20240615_143022.txt
    ├── monthly_revenue_20240615_143055.txt
    └── ...
```

### Data File Format

```
# VehicleServiceRecords Data File — DO NOT EDIT MANUALLY
# Format: RecordId|Plate|Owner|ServiceType|Cost|Technician|Notes|ServiceDate|CreatedAt|UpdatedAt|IsActive|Checksum
VSR-00001|ABC 123|Juan dela Cruz|oil change|850.00|Mario Santos||2024-06-01|2024-06-01T09:00:00|2024-06-01T09:00:00|1|1A2B3C4D
```

Pipe characters inside field values are escaped as `[PIPE]`; newlines as `[NL]`.

---

## How to Open in Visual Studio 2012

1. Open **Visual Studio 2012**
2. File → Open → Project/Solution
3. Select `VehicleServiceRecords.sln`
4. Press **F5** to build and run

**Target Framework:** .NET 4.5 (included with VS2012)  
**No NuGet packages required** — only `System` and `System.Core` references.

---

## Validation Rules

| Field | Rule |
|---|---|
| Vehicle Plate | 2–10 chars, alphanumeric + hyphen + space |
| Owner Name | 2–80 characters, non-empty |
| Service Type | Must match one of 13 allowed types |
| Service Cost | Numeric, 0 – 9,999,999 |
| Technician | 2–60 characters, non-empty |
| Service Date | Valid date, 1990-01-01 to today |

---

## Audit Log Format

```
[2024-06-15 14:30:22] ACTION=SYSTEM   | Application started. Existing data loaded.
[2024-06-15 14:30:45] ACTION=ADD      | RecordId=VSR-00001 Plate=ABC 123 Owner=Juan dela Cruz ServiceType=oil change Cost=850.00
[2024-06-15 14:31:10] ACTION=READ     | ViewAll: 1 active record(s) listed.
[2024-06-15 14:31:55] ACTION=UPDATE   | RecordId=VSR-00001 Plate=ABC 123 ServiceType=oil change Cost=950.00
[2024-06-15 14:32:30] ACTION=DELETE   | SOFT RecordId=VSR-00001 Plate=ABC 123
[2024-06-15 14:33:00] ACTION=REPORT   | Generated: service_type_summary_20240615_143300.txt
[2024-06-15 14:33:15] ACTION=ERROR    | Context=LoadAll | Error=Checksum mismatch on line 3
```
