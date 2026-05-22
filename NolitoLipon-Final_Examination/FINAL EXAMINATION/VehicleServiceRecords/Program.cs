using System;
using System.Collections.Generic;
using System.IO;
using VehicleServiceRecords.Helpers;
using VehicleServiceRecords.Models;
using VehicleServiceRecords.Services;

namespace VehicleServiceRecords
{
    class Program
    {
        // ------------------------------------------------------------------ Paths
        static readonly string BaseDir = @"C:\VehicleServiceRecords";

        static readonly string DataDir    = Path.Combine(BaseDir, "data");
        static readonly string ReportDir  = Path.Combine(BaseDir, "reports");
        static readonly string DataFile   = Path.Combine(DataDir, "records.dat");
        static readonly string BackupFile = Path.Combine(DataDir, "records.bak");
        static readonly string AuditFile  = Path.Combine(DataDir, "audit.log");

        // ------------------------------------------------------------------ Services
        static AuditLogger      _audit;
        static RecordRepository _repo;
        static ReportGenerator  _reporter;
        static List<ServiceRecord> _records;

        // ------------------------------------------------------------------ Entry Point
        static void Main(string[] args)
        {
            Console.Title = "Vehicle Service Records Management System";

            _audit    = new AuditLogger(AuditFile);
            _repo     = new RecordRepository(DataFile, BackupFile, _audit);
            _reporter = new ReportGenerator(ReportDir, _audit);

            // Initialize storage
            try
            {
                StorageInitializer.Initialize(DataDir, ReportDir, DataFile, AuditFile, _audit);
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError("Failed to initialize storage: " + ex.Message);
                Console.ReadKey();
                return;
            }

            // Load records
            try
            {
                _records = _repo.LoadAll();
                _audit.LogRead(string.Format("Startup load: {0} record(s) found.", _records.Count));
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError("Failed to load records: " + ex.Message);
                _audit.LogError("Startup", ex.Message);
                _records = new List<ServiceRecord>();
            }

            RunMenuLoop();
        }

        // ------------------------------------------------------------------ Main Menu Loop
        static void RunMenuLoop()
        {
            bool running = true;
            while (running)
            {
                Console.Clear();
                ConsoleHelper.WriteHeader("Vehicle Service Records Management System");
                Console.WriteLine();
                Console.WriteLine("   [1]  Add Service Record");
                Console.WriteLine("   [2]  View / Search Records");
                Console.WriteLine("   [3]  Update Record");
                Console.WriteLine("   [4]  Delete Record (Soft)");
                Console.WriteLine("   [5]  Reports");
                Console.WriteLine("   [6]  View Audit Log");
                Console.WriteLine("   [7]  Advanced Options");
                Console.WriteLine("   [0]  Exit");
                Console.WriteLine();
                Console.Write("  Select an option: ");

                string choice = Console.ReadLine();
                switch (choice)
                {
                    case "1": MenuAdd();            break;
                    case "2": MenuViewSearch();      break;
                    case "3": MenuUpdate();          break;
                    case "4": MenuSoftDelete();      break;
                    case "5": MenuReports();         break;
                    case "6": MenuAuditLog();        break;
                    case "7": MenuAdvanced();        break;
                    case "0": running = false;       break;
                    default:
                        ConsoleHelper.WriteError("Invalid option. Please enter 0-7.");
                        ConsoleHelper.PressAnyKey();
                        break;
                }
            }

            _audit.LogSystem("Application exited normally.");
            Console.Clear();
            ConsoleHelper.WriteInfo("Goodbye! All data has been saved.");
            Console.WriteLine();
        }

        // ------------------------------------------------------------------ [1] Add Record
        static void MenuAdd()
        {
            Console.Clear();
            ConsoleHelper.WriteHeader("Add New Service Record");

            string plate, owner, serviceType, technician, notes, dateStr, costStr;
            decimal cost;
            DateTime serviceDate;
            ValidationResult vr;

            // Vehicle Plate
            while (true)
            {
                plate = ConsoleHelper.PromptRequired("Vehicle Plate (e.g. ABC 123)");
                vr = Validator.ValidatePlate(plate);
                if (vr.IsValid) break;
                ConsoleHelper.WriteError(vr.Error);
            }
            plate = plate.ToUpper();

            // Owner Name
            while (true)
            {
                owner = ConsoleHelper.PromptRequired("Owner Name");
                vr = Validator.ValidateOwnerName(owner);
                if (vr.IsValid) break;
                ConsoleHelper.WriteError(vr.Error);
            }

            // Service Type
            Console.WriteLine();
            Console.WriteLine("  Available service types:");
            Console.WriteLine("  oil change, tire rotation, brake service, engine repair,");
            Console.WriteLine("  transmission, ac service, battery replacement, inspection,");
            Console.WriteLine("  wheel alignment, clutch repair, suspension, electrical, other");
            Console.WriteLine();
            while (true)
            {
                serviceType = ConsoleHelper.PromptRequired("Service Type");
                vr = Validator.ValidateServiceType(serviceType);
                if (vr.IsValid) { serviceType = serviceType.ToLower(); break; }
                ConsoleHelper.WriteError(vr.Error);
            }

            // Cost
            while (true)
            {
                costStr = ConsoleHelper.PromptRequired("Service Cost (e.g. 1500.00)");
                vr = Validator.ValidateCost(costStr, out cost);
                if (vr.IsValid) break;
                ConsoleHelper.WriteError(vr.Error);
            }

            // Technician
            while (true)
            {
                technician = ConsoleHelper.PromptRequired("Technician Name");
                vr = Validator.ValidateTechnician(technician);
                if (vr.IsValid) break;
                ConsoleHelper.WriteError(vr.Error);
            }

            // Service Date
            while (true)
            {
                dateStr = ConsoleHelper.PromptRequired("Service Date (YYYY-MM-DD, e.g. 2024-06-15)");
                vr = Validator.ValidateServiceDate(dateStr, out serviceDate);
                if (vr.IsValid) break;
                ConsoleHelper.WriteError(vr.Error);
            }

            // Notes (optional)
            notes = ConsoleHelper.PromptOptional("Notes (optional)", "");

            // Preview
            Console.WriteLine();
            ConsoleHelper.WriteSeparator();
            Console.WriteLine("  Please review the new record:");
            Console.WriteLine("  Plate       : " + plate);
            Console.WriteLine("  Owner       : " + owner);
            Console.WriteLine("  Service     : " + serviceType);
            Console.WriteLine("  Cost        : " + cost.ToString("F2"));
            Console.WriteLine("  Technician  : " + technician);
            Console.WriteLine("  Service Date: " + serviceDate.ToString("yyyy-MM-dd"));
            Console.WriteLine("  Notes       : " + notes);
            ConsoleHelper.WriteSeparator();

            if (!ConsoleHelper.Confirm("Save this record?"))
            {
                ConsoleHelper.WriteWarning("Add cancelled.");
                ConsoleHelper.PressAnyKey();
                return;
            }

            try
            {
                DateTime now = DateTime.Now;
                ServiceRecord rec = new ServiceRecord
                {
                    RecordId    = _repo.GenerateId(_records),
                    VehiclePlate = plate,
                    OwnerName   = owner,
                    ServiceType = serviceType,
                    ServiceCost = cost,
                    Technician  = technician,
                    Notes       = notes,
                    ServiceDate = serviceDate,
                    CreatedAt   = now,
                    UpdatedAt   = now,
                    IsActive    = true
                };

                _repo.Add(_records, rec);
                ConsoleHelper.WriteSuccess("Record saved! ID: " + rec.RecordId);
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError("Failed to save record: " + ex.Message);
                _audit.LogError("MenuAdd", ex.Message);
            }

            ConsoleHelper.PressAnyKey();
        }

        // ------------------------------------------------------------------ [2] View / Search
        static void MenuViewSearch()
        {
            bool back = false;
            while (!back)
            {
                Console.Clear();
                ConsoleHelper.WriteHeader("View / Search Records");
                Console.WriteLine();
                Console.WriteLine("   [1]  View All Active Records");
                Console.WriteLine("   [2]  Search by Vehicle Plate");
                Console.WriteLine("   [3]  Search by Owner Name");
                Console.WriteLine("   [4]  Filter by Service Type");
                Console.WriteLine("   [5]  View Record Detail (by ID)");
                Console.WriteLine("   [6]  View Inactive / Deleted Records");
                Console.WriteLine("   [0]  Back to Main Menu");
                Console.WriteLine();
                Console.Write("  Select: ");

                switch (Console.ReadLine())
                {
                    case "1": ViewActiveRecords();                    break;
                    case "2": SearchByPlate();                        break;
                    case "3": SearchByOwner();                        break;
                    case "4": FilterByServiceType();                  break;
                    case "5": ViewRecordDetail();                     break;
                    case "6": ViewInactiveRecords();                  break;
                    case "0": back = true;                            break;
                    default:
                        ConsoleHelper.WriteError("Invalid option.");
                        ConsoleHelper.PressAnyKey();
                        break;
                }
            }
        }

        static void ViewActiveRecords()
        {
            Console.Clear();
            ConsoleHelper.WriteHeader("All Active Records");
            List<ServiceRecord> active = GetActive();
            if (active.Count == 0)
            {
                ConsoleHelper.WriteInfo("No active records found.");
            }
            else
            {
                ConsoleHelper.PrintRecordTableHeader();
                foreach (ServiceRecord r in active)
                    ConsoleHelper.PrintRecordShort(r);
                Console.WriteLine();
                ConsoleHelper.WriteInfo(string.Format("{0} record(s) found.", active.Count));
            }
            _audit.LogRead(string.Format("ViewAll: {0} active record(s) listed.", active.Count));
            ConsoleHelper.PressAnyKey();
        }

        static void SearchByPlate()
        {
            Console.Clear();
            ConsoleHelper.WriteHeader("Search by Vehicle Plate");
            string query = ConsoleHelper.PromptRequired("Enter plate (or partial)").ToUpper();
            var found = new List<ServiceRecord>();
            foreach (ServiceRecord r in _records)
                if (r.IsActive && r.VehiclePlate.ToUpper().Contains(query))
                    found.Add(r);
            PrintSearchResults(found, "plate contains '" + query + "'");
            _audit.LogRead("SearchByPlate: query='" + query + "' results=" + found.Count);
            ConsoleHelper.PressAnyKey();
        }

        static void SearchByOwner()
        {
            Console.Clear();
            ConsoleHelper.WriteHeader("Search by Owner Name");
            string query = ConsoleHelper.PromptRequired("Enter owner name (or partial)").ToLower();
            var found = new List<ServiceRecord>();
            foreach (ServiceRecord r in _records)
                if (r.IsActive && r.OwnerName.ToLower().Contains(query))
                    found.Add(r);
            PrintSearchResults(found, "owner contains '" + query + "'");
            _audit.LogRead("SearchByOwner: query='" + query + "' results=" + found.Count);
            ConsoleHelper.PressAnyKey();
        }

        static void FilterByServiceType()
        {
            Console.Clear();
            ConsoleHelper.WriteHeader("Filter by Service Type");
            string query = ConsoleHelper.PromptRequired("Enter service type").ToLower();
            var found = new List<ServiceRecord>();
            foreach (ServiceRecord r in _records)
                if (r.IsActive && r.ServiceType.ToLower().Contains(query))
                    found.Add(r);
            PrintSearchResults(found, "service type contains '" + query + "'");
            _audit.LogRead("FilterByServiceType: query='" + query + "' results=" + found.Count);
            ConsoleHelper.PressAnyKey();
        }

        static void ViewRecordDetail()
        {
            Console.Clear();
            ConsoleHelper.WriteHeader("View Record Detail");
            string id = ConsoleHelper.PromptRequired("Enter Record ID (e.g. VSR-00001)").ToUpper();
            ServiceRecord found = FindById(id);
            if (found == null)
            {
                ConsoleHelper.WriteError("Record not found: " + id);
            }
            else
            {
                ConsoleHelper.PrintRecordDetail(found);
                _audit.LogRead("ViewDetail: RecordId=" + id);
            }
            ConsoleHelper.PressAnyKey();
        }

        static void ViewInactiveRecords()
        {
            Console.Clear();
            ConsoleHelper.WriteHeader("Inactive (Soft-Deleted) Records");
            var inactive = new List<ServiceRecord>();
            foreach (ServiceRecord r in _records)
                if (!r.IsActive) inactive.Add(r);
            if (inactive.Count == 0)
            {
                ConsoleHelper.WriteInfo("No inactive records found.");
            }
            else
            {
                ConsoleHelper.PrintRecordTableHeader();
                foreach (ServiceRecord r in inactive)
                    ConsoleHelper.PrintRecordShort(r);
                Console.WriteLine();
                ConsoleHelper.WriteInfo(string.Format("{0} inactive record(s).", inactive.Count));
            }
            _audit.LogRead("ViewInactive: " + inactive.Count + " record(s).");
            ConsoleHelper.PressAnyKey();
        }

        static void PrintSearchResults(List<ServiceRecord> results, string filter)
        {
            if (results.Count == 0)
            {
                ConsoleHelper.WriteInfo("No records found where " + filter + ".");
            }
            else
            {
                ConsoleHelper.PrintRecordTableHeader();
                foreach (ServiceRecord r in results)
                    ConsoleHelper.PrintRecordShort(r);
                Console.WriteLine();
                ConsoleHelper.WriteInfo(string.Format("{0} record(s) found where {1}.", results.Count, filter));
            }
        }

        // ------------------------------------------------------------------ [3] Update Record
        static void MenuUpdate()
        {
            Console.Clear();
            ConsoleHelper.WriteHeader("Update Service Record");

            string id = ConsoleHelper.PromptRequired("Enter Record ID to update").ToUpper();
            ServiceRecord rec = FindById(id);

            if (rec == null)
            {
                ConsoleHelper.WriteError("Record not found: " + id);
                ConsoleHelper.PressAnyKey();
                return;
            }
            if (!rec.IsActive)
            {
                ConsoleHelper.WriteError("Record is inactive (soft-deleted) and cannot be updated.");
                ConsoleHelper.PressAnyKey();
                return;
            }

            ConsoleHelper.PrintRecordDetail(rec);
            Console.WriteLine("  Leave blank to keep existing value.");
            Console.WriteLine();

            // Work on a copy
            ServiceRecord updated = new ServiceRecord
            {
                RecordId    = rec.RecordId,
                VehiclePlate = rec.VehiclePlate,
                OwnerName   = rec.OwnerName,
                ServiceType = rec.ServiceType,
                ServiceCost = rec.ServiceCost,
                Technician  = rec.Technician,
                Notes       = rec.Notes,
                ServiceDate = rec.ServiceDate,
                CreatedAt   = rec.CreatedAt,
                UpdatedAt   = rec.UpdatedAt,
                IsActive    = rec.IsActive
            };

            ValidationResult vr;

            // Vehicle Plate
            while (true)
            {
                string input = ConsoleHelper.PromptOptional("Vehicle Plate", rec.VehiclePlate);
                vr = Validator.ValidatePlate(input);
                if (vr.IsValid) { updated.VehiclePlate = input.ToUpper(); break; }
                ConsoleHelper.WriteError(vr.Error);
            }

            // Owner
            while (true)
            {
                string input = ConsoleHelper.PromptOptional("Owner Name", rec.OwnerName);
                vr = Validator.ValidateOwnerName(input);
                if (vr.IsValid) { updated.OwnerName = input; break; }
                ConsoleHelper.WriteError(vr.Error);
            }

            // Service Type
            while (true)
            {
                string input = ConsoleHelper.PromptOptional("Service Type", rec.ServiceType);
                vr = Validator.ValidateServiceType(input);
                if (vr.IsValid) { updated.ServiceType = input.ToLower(); break; }
                ConsoleHelper.WriteError(vr.Error);
            }

            // Cost
            while (true)
            {
                decimal cost;
                string input = ConsoleHelper.PromptOptional("Service Cost", rec.ServiceCost.ToString("F2"));
                vr = Validator.ValidateCost(input, out cost);
                if (vr.IsValid) { updated.ServiceCost = cost; break; }
                ConsoleHelper.WriteError(vr.Error);
            }

            // Technician
            while (true)
            {
                string input = ConsoleHelper.PromptOptional("Technician", rec.Technician);
                vr = Validator.ValidateTechnician(input);
                if (vr.IsValid) { updated.Technician = input; break; }
                ConsoleHelper.WriteError(vr.Error);
            }

            // Service Date
            while (true)
            {
                DateTime dt;
                string input = ConsoleHelper.PromptOptional("Service Date (YYYY-MM-DD)", rec.ServiceDate.ToString("yyyy-MM-dd"));
                vr = Validator.ValidateServiceDate(input, out dt);
                if (vr.IsValid) { updated.ServiceDate = dt; break; }
                ConsoleHelper.WriteError(vr.Error);
            }

            // Notes
            updated.Notes = ConsoleHelper.PromptOptional("Notes", rec.Notes);

            if (!ConsoleHelper.Confirm("Save changes?"))
            {
                ConsoleHelper.WriteWarning("Update cancelled.");
                ConsoleHelper.PressAnyKey();
                return;
            }

            try
            {
                _repo.Update(_records, updated);
                ConsoleHelper.WriteSuccess("Record updated successfully.");
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError("Failed to update: " + ex.Message);
                _audit.LogError("MenuUpdate", ex.Message);
            }

            ConsoleHelper.PressAnyKey();
        }

        // ------------------------------------------------------------------ [4] Soft Delete
        static void MenuSoftDelete()
        {
            Console.Clear();
            ConsoleHelper.WriteHeader("Soft Delete Record");

            string id = ConsoleHelper.PromptRequired("Enter Record ID to deactivate").ToUpper();
            ServiceRecord rec = FindById(id);

            if (rec == null)
            {
                ConsoleHelper.WriteError("Record not found: " + id);
                ConsoleHelper.PressAnyKey();
                return;
            }
            if (!rec.IsActive)
            {
                ConsoleHelper.WriteWarning("Record is already inactive.");
                ConsoleHelper.PressAnyKey();
                return;
            }

            ConsoleHelper.PrintRecordDetail(rec);

            if (!ConsoleHelper.Confirm("Mark this record as INACTIVE (soft delete)?"))
            {
                ConsoleHelper.WriteWarning("Delete cancelled.");
                ConsoleHelper.PressAnyKey();
                return;
            }

            try
            {
                _repo.SoftDelete(_records, id);
                ConsoleHelper.WriteSuccess("Record deactivated (soft deleted). ID: " + id);
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError("Delete failed: " + ex.Message);
                _audit.LogError("MenuSoftDelete", ex.Message);
            }

            ConsoleHelper.PressAnyKey();
        }

        // ------------------------------------------------------------------ [5] Reports
        static void MenuReports()
        {
            bool back = false;
            while (!back)
            {
                Console.Clear();
                ConsoleHelper.WriteHeader("Report Generation");
                Console.WriteLine();
                Console.WriteLine("   [1]  Service Type Summary");
                Console.WriteLine("   [2]  Monthly Revenue Report");
                Console.WriteLine("   [3]  Top 10 Vehicles by Spend");
                Console.WriteLine("   [4]  Full Active Records Listing");
                Console.WriteLine("   [0]  Back");
                Console.WriteLine();
                Console.Write("  Select: ");

                switch (Console.ReadLine())
                {
                    case "1": RunReport(() => _reporter.GenerateServiceTypeSummary(_records),  "Service Type Summary");  break;
                    case "2": RunReport(() => _reporter.GenerateMonthlyRevenue(_records),       "Monthly Revenue");       break;
                    case "3": RunReport(() => _reporter.GenerateTopVehicles(_records, 10),      "Top 10 Vehicles");       break;
                    case "4": RunReport(() => _reporter.GenerateFullListing(_records),          "Full Listing");          break;
                    case "0": back = true; break;
                    default:
                        ConsoleHelper.WriteError("Invalid option.");
                        ConsoleHelper.PressAnyKey();
                        break;
                }
            }
        }

        static void RunReport(Func<string> generateFn, string reportName)
        {
            Console.Clear();
            ConsoleHelper.WriteHeader("Generating: " + reportName);
            try
            {
                string path = generateFn();
                ConsoleHelper.WriteSuccess("Report saved to: " + path);
                Console.WriteLine();

                // Display report inline
                if (File.Exists(path))
                {
                    Console.WriteLine(File.ReadAllText(path));
                }
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError("Report generation failed: " + ex.Message);
                _audit.LogError("RunReport/" + reportName, ex.Message);
            }
            ConsoleHelper.PressAnyKey();
        }

        // ------------------------------------------------------------------ [6] Audit Log
        static void MenuAuditLog()
        {
            Console.Clear();
            ConsoleHelper.WriteHeader("Audit Log (Last 30 Entries)");

            try
            {
                if (!File.Exists(AuditFile))
                {
                    ConsoleHelper.WriteInfo("Audit log is empty.");
                }
                else
                {
                    string[] lines = File.ReadAllLines(AuditFile);
                    int start = Math.Max(0, lines.Length - 30);
                    for (int i = start; i < lines.Length; i++)
                        Console.WriteLine("  " + lines[i]);
                    Console.WriteLine();
                    ConsoleHelper.WriteInfo(string.Format("Showing {0} of {1} total log entries.", lines.Length - start, lines.Length));
                }
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError("Could not read audit log: " + ex.Message);
            }

            ConsoleHelper.PressAnyKey();
        }

        // ------------------------------------------------------------------ [7] Advanced Options
        static void MenuAdvanced()
        {
            bool back = false;
            while (!back)
            {
                Console.Clear();
                ConsoleHelper.WriteHeader("Advanced Options");
                Console.WriteLine();
                Console.WriteLine("   [1]  Hard Delete Record (PERMANENT)");
                Console.WriteLine("   [2]  Restore Soft-Deleted Record");
                Console.WriteLine("   [3]  Verify All Checksums");
                Console.WriteLine("   [4]  Show Data File Path");
                Console.WriteLine("   [0]  Back");
                Console.WriteLine();
                Console.Write("  Select: ");

                switch (Console.ReadLine())
                {
                    case "1": MenuHardDelete();      break;
                    case "2": MenuRestore();         break;
                    case "3": MenuVerifyChecksums(); break;
                    case "4":
                        Console.Clear();
                        ConsoleHelper.WriteHeader("Data Paths");
                        Console.WriteLine("  Data File  : " + DataFile);
                        Console.WriteLine("  Backup File: " + BackupFile);
                        Console.WriteLine("  Audit File : " + AuditFile);
                        Console.WriteLine("  Reports Dir: " + ReportDir);
                        ConsoleHelper.PressAnyKey();
                        break;
                    case "0": back = true; break;
                    default:
                        ConsoleHelper.WriteError("Invalid option.");
                        ConsoleHelper.PressAnyKey();
                        break;
                }
            }
        }

        static void MenuHardDelete()
        {
            Console.Clear();
            ConsoleHelper.WriteHeader("HARD DELETE — PERMANENT REMOVAL");
            ConsoleHelper.WriteWarning("This action CANNOT be undone!");
            Console.WriteLine();

            string id = ConsoleHelper.PromptRequired("Enter Record ID to permanently delete").ToUpper();
            ServiceRecord rec = FindById(id);

            if (rec == null)
            {
                ConsoleHelper.WriteError("Record not found: " + id);
                ConsoleHelper.PressAnyKey();
                return;
            }

            ConsoleHelper.PrintRecordDetail(rec);
            ConsoleHelper.WriteWarning("This will PERMANENTLY remove the record from all files.");

            if (!ConsoleHelper.Confirm("Are you SURE you want to hard delete " + id + "?"))
            {
                ConsoleHelper.WriteWarning("Hard delete cancelled.");
                ConsoleHelper.PressAnyKey();
                return;
            }

            try
            {
                _repo.HardDelete(_records, id);
                ConsoleHelper.WriteSuccess("Record permanently deleted: " + id);
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError("Hard delete failed: " + ex.Message);
                _audit.LogError("MenuHardDelete", ex.Message);
            }

            ConsoleHelper.PressAnyKey();
        }

        static void MenuRestore()
        {
            Console.Clear();
            ConsoleHelper.WriteHeader("Restore Soft-Deleted Record");

            string id = ConsoleHelper.PromptRequired("Enter Record ID to restore").ToUpper();
            ServiceRecord rec = FindById(id);

            if (rec == null)
            {
                ConsoleHelper.WriteError("Record not found: " + id);
                ConsoleHelper.PressAnyKey();
                return;
            }
            if (rec.IsActive)
            {
                ConsoleHelper.WriteWarning("Record is already active — no restore needed.");
                ConsoleHelper.PressAnyKey();
                return;
            }

            ConsoleHelper.PrintRecordDetail(rec);

            if (!ConsoleHelper.Confirm("Restore this record to ACTIVE?"))
            {
                ConsoleHelper.WriteWarning("Restore cancelled.");
                ConsoleHelper.PressAnyKey();
                return;
            }

            try
            {
                rec.IsActive  = true;
                rec.UpdatedAt = DateTime.Now;
                rec.Checksum  = rec.ComputeChecksum();
                _repo.Update(_records, rec);
                _audit.LogUpdate("RESTORE RecordId=" + id);
                ConsoleHelper.WriteSuccess("Record restored: " + id);
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError("Restore failed: " + ex.Message);
                _audit.LogError("MenuRestore", ex.Message);
            }

            ConsoleHelper.PressAnyKey();
        }

        static void MenuVerifyChecksums()
        {
            Console.Clear();
            ConsoleHelper.WriteHeader("Checksum Verification");
            Console.WriteLine();

            int ok = 0, bad = 0;
            foreach (ServiceRecord rec in _records)
            {
                string expected = rec.ComputeChecksum();
                if (rec.Checksum == expected)
                {
                    ok++;
                }
                else
                {
                    bad++;
                    ConsoleHelper.WriteWarning(string.Format(
                        "Mismatch: {0} | Expected={1} | Stored={2}",
                        rec.RecordId, expected, rec.Checksum));
                }
            }

            Console.WriteLine();
            ConsoleHelper.WriteInfo(string.Format("Total: {0} | OK: {1} | Mismatches: {2}", _records.Count, ok, bad));
            _audit.LogSystem(string.Format("ChecksumVerify: total={0} ok={1} bad={2}", _records.Count, ok, bad));
            ConsoleHelper.PressAnyKey();
        }

        // ------------------------------------------------------------------ Helpers
        static List<ServiceRecord> GetActive()
        {
            var active = new List<ServiceRecord>();
            foreach (ServiceRecord r in _records)
                if (r.IsActive) active.Add(r);
            return active;
        }

        static ServiceRecord FindById(string id)
        {
            foreach (ServiceRecord r in _records)
                if (string.Equals(r.RecordId, id, StringComparison.OrdinalIgnoreCase))
                    return r;
            return null;
        }
    }
}
