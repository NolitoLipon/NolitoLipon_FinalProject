using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace VehicleServiceRecordsApp
{
    // This class represents a single vehicle service record
    class ServiceRecord
    {
        public int RecordId;           // Unique ID for each record
        public string VehicleOwner;    // Name of vehicle owner
        public string VehicleModel;    // Vehicle model
        public string ServiceType;     // Type of service performed
        public DateTime ServiceDate;   // When the service was done
        public DateTime CreatedAt;     // When this record was first created
        public DateTime UpdatedAt;     // Last time this record was updated
        public bool IsActive;          // True if record is active, false if soft-deleted
        public string Checksum;        // Simple hash to detect changes
    }

    // Handles validation of user input
    class Validator
    {
        // Check if a string is not empty
        public static bool ValidateNonEmpty(string input)
        {
            return !string.IsNullOrEmpty(input);
        }

        // Try to parse a date from user input
        public static bool ValidateDate(string input, out DateTime result)
        {
            return DateTime.TryParse(input, out result);
        }

        // Validate all required fields in a ServiceRecord
        public static void ValidateRecord(ServiceRecord record)
        {
            if (!ValidateNonEmpty(record.VehicleOwner))
                throw new Exception("Owner name cannot be empty.");
            if (!ValidateNonEmpty(record.VehicleModel))
                throw new Exception("Vehicle model cannot be empty.");
            if (!ValidateNonEmpty(record.ServiceType))
                throw new Exception("Service type cannot be empty.");
        }
    }

    // Handles reading and writing records to files
    class FileRepository
    {
        private string dataFilePath = "Data\\ServiceRecords.csv";
        private string auditFilePath = "Data\\AuditLog.txt";

        // Ensure the data folder and files exist
        public void InitializeStorage()
        {
            if (!Directory.Exists("Data"))
                Directory.CreateDirectory("Data");

            if (!File.Exists(dataFilePath))
                File.Create(dataFilePath).Close();

            if (!File.Exists(auditFilePath))
                File.Create(auditFilePath).Close();

            LogAction("InitializeStorage", "Storage folder and files created.");
        }

        // Read all records from the CSV file
        public List<ServiceRecord> GetAllRecords()
        {
            List<ServiceRecord> records = new List<ServiceRecord>();

            try
            {
                string[] lines = File.ReadAllLines(dataFilePath);
                foreach (string line in lines)
                {
                    if (string.IsNullOrEmpty(line)) continue;
                    string[] parts = line.Split('|');

                    ServiceRecord record = new ServiceRecord();
                    record.RecordId = int.Parse(parts[0]);
                    record.VehicleOwner = parts[1];
                    record.VehicleModel = parts[2];
                    record.ServiceType = parts[3];
                    record.ServiceDate = DateTime.Parse(parts[4]);
                    record.CreatedAt = DateTime.Parse(parts[5]);
                    record.UpdatedAt = DateTime.Parse(parts[6]);
                    record.IsActive = bool.Parse(parts[7]);
                    record.Checksum = parts[8];

                    // Verify data integrity
                    if (ComputeChecksum(record) != record.Checksum)
                    {
                        LogAction("Error", "Checksum mismatch for RecordId " + record.RecordId);
                        continue;
                    }

                    records.Add(record);
                }

                LogAction("Read", records.Count + " records loaded.");
            }
            catch (Exception ex)
            {
                LogAction("Error", "Error reading records: " + ex.Message);
            }

            return records;
        }

        // Save all records back to the CSV file
        public void SaveAllRecords(List<ServiceRecord> records)
        {
            try
            {
                List<string> lines = new List<string>();
                foreach (ServiceRecord r in records)
                {
                    string line = r.RecordId + "|" + r.VehicleOwner + "|" + r.VehicleModel + "|" +
                                  r.ServiceType + "|" + r.ServiceDate + "|" + r.CreatedAt + "|" +
                                  r.UpdatedAt + "|" + r.IsActive + "|" + r.Checksum;
                    lines.Add(line);
                }

                File.WriteAllLines(dataFilePath, lines.ToArray());
                LogAction("Save", records.Count + " records saved.");
            }
            catch (Exception ex)
            {
                LogAction("Error", "Error saving records: " + ex.Message);
            }
        }

        // Append a message to the audit log
        public void LogAction(string action, string details)
        {
            try
            {
                string log = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " | " + action + " | " + details;
                File.AppendAllText(auditFilePath, log + Environment.NewLine);
            }
            catch
            {
                // Logging should never crash the program
            }
        }

        // Compute a SHA256 checksum for the record
        public string ComputeChecksum(ServiceRecord record)
        {
            string raw = record.RecordId + record.VehicleOwner + record.VehicleModel +
                         record.ServiceType + record.ServiceDate + record.CreatedAt +
                         record.UpdatedAt + record.IsActive;

            SHA256 sha = SHA256.Create();
            byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
            StringBuilder sb = new StringBuilder();
            foreach (byte b in bytes)
                sb.Append(b.ToString("X2"));

            return sb.ToString();
        }
    }

    // Handles simple report generation
    class ReportGenerator
    {
        public void GenerateReport(List<ServiceRecord> records)
        {
            int activeCount = 0;
            Dictionary<string, int> serviceCounts = new Dictionary<string, int>();

            foreach (ServiceRecord r in records)
            {
                if (!r.IsActive) continue;
                activeCount++;

                if (!serviceCounts.ContainsKey(r.ServiceType))
                    serviceCounts[r.ServiceType] = 1;
                else
                    serviceCounts[r.ServiceType]++;
            }

            Console.WriteLine("\n--- Vehicle Service Report ---");
            Console.WriteLine("Total Active Records: " + activeCount);
            Console.WriteLine("Records by Service Type:");
            foreach (string key in serviceCounts.Keys)
            {
                Console.WriteLine("  " + key + ": " + serviceCounts[key]);
            }
        }
    }

    // Main program class
    class Program
    {
        static FileRepository repository = new FileRepository();
        static ReportGenerator reportGenerator = new ReportGenerator();

        static void Main()
        {
            // Make sure data folder and files exist
            repository.InitializeStorage();

            while (true)
            {
                // Display menu
                Console.WriteLine("\n--- Vehicle Service Records ---");
                Console.WriteLine("1. Add Record");
                Console.WriteLine("2. View Records");
                Console.WriteLine("3. Update Record");
                Console.WriteLine("4. Delete Record (Soft)");
                Console.WriteLine("5. Hard Delete Record");
                Console.WriteLine("6. Generate Report");
                Console.WriteLine("0. Exit");
                Console.Write("Select an option: ");

                string choice = Console.ReadLine();

                if (choice == "1") AddRecord();
                else if (choice == "2") ViewRecords();
                else if (choice == "3") UpdateRecord();
                else if (choice == "4") DeleteRecord(true);
                else if (choice == "5") DeleteRecord(false);
                else if (choice == "6") GenerateReport();
                else if (choice == "0") break;
                else Console.WriteLine("Invalid choice, please try again!");
            }
        }

        // Human-friendly method for adding a record
        static void AddRecord()
        {
            try
            {
                List<ServiceRecord> records = repository.GetAllRecords();
                ServiceRecord r = new ServiceRecord();

                r.RecordId = (records.Count > 0) ? records[records.Count - 1].RecordId + 1 : 1;

                Console.Write("Enter Vehicle Owner: ");
                r.VehicleOwner = Console.ReadLine();

                Console.Write("Enter Vehicle Model: ");
                r.VehicleModel = Console.ReadLine();

                Console.Write("Enter Service Type: ");
                r.ServiceType = Console.ReadLine();

                Console.Write("Enter Service Date (yyyy-mm-dd): ");
                string dateInput = Console.ReadLine();
                DateTime date;
                if (!Validator.ValidateDate(dateInput, out date))
                {
                    Console.WriteLine("Invalid date. Record not added.");
                    return;
                }
                r.ServiceDate = date;

                r.CreatedAt = DateTime.Now;
                r.UpdatedAt = DateTime.Now;
                r.IsActive = true;

                // Validate fields
                Validator.ValidateRecord(r);

                // Compute checksum
                r.Checksum = repository.ComputeChecksum(r);

                // Save record
                records.Add(r);
                repository.SaveAllRecords(records);
                repository.LogAction("Add", "Added RecordId " + r.RecordId);

                Console.WriteLine("Record added successfully!");
            }
            catch (Exception ex)
            {
                repository.LogAction("Error", "AddRecord failed: " + ex.Message);
                Console.WriteLine("Error: " + ex.Message);
            }
        }

        // View all active records in a table format
        static void ViewRecords()
        {
            var records = repository.GetAllRecords();

            // Print table headers
            Console.WriteLine("\nID    Owner             Vehicle          Service           Service Date");
            Console.WriteLine("----------------------------------------------------------------------");

            // Print each active record in aligned columns
            foreach (var r in records)
            {
                if (!r.IsActive) continue;
                Console.WriteLine(
                    r.RecordId.ToString().PadRight(5) + " " +
                    r.VehicleOwner.PadRight(17) + " " +
                    r.VehicleModel.PadRight(15) + " " +
                    r.ServiceType.PadRight(17) + " " +
                    r.ServiceDate.ToString("yyyy-MM-dd")
                );
            }

    // Optional search by Owner Name
    Console.Write("\nSearch by Owner Name (leave empty to skip): ");
    string search = Console.ReadLine();
    if (!string.IsNullOrEmpty(search))
    {
        Console.WriteLine("\nSearch Results:");
        Console.WriteLine("ID    Owner             Vehicle          Service           Service Date");
        Console.WriteLine("----------------------------------------------------------------------");

        foreach (var r in records)
        {
            if (!r.IsActive) continue;
            if (r.VehicleOwner.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Console.WriteLine(
                    r.RecordId.ToString().PadRight(5) + " " +
                    r.VehicleOwner.PadRight(17) + " " +
                    r.VehicleModel.PadRight(15) + " " +
                    r.ServiceType.PadRight(17) + " " +
                    r.ServiceDate.ToString("yyyy-MM-dd")
                );
            }
        }
    }
}
        // Update a record by ID
        static void UpdateRecord()
        {
            var records = repository.GetAllRecords();
            Console.Write("Enter Record ID to update: ");
            int id;
            if (!int.TryParse(Console.ReadLine(), out id)) { Console.WriteLine("Invalid ID"); return; }

            ServiceRecord record = null;
            foreach (var r in records)
                if (r.RecordId == id && r.IsActive) record = r;

            if (record == null) { Console.WriteLine("Record not found"); return; }

            Console.Write("New Vehicle Owner (" + record.VehicleOwner + "): ");
            string owner = Console.ReadLine();
            if (!string.IsNullOrEmpty(owner)) record.VehicleOwner = owner;

            Console.Write("New Vehicle Model (" + record.VehicleModel + "): ");
            string model = Console.ReadLine();
            if (!string.IsNullOrEmpty(model)) record.VehicleModel = model;

            Console.Write("New Service Type (" + record.ServiceType + "): ");
            string service = Console.ReadLine();
            if (!string.IsNullOrEmpty(service)) record.ServiceType = service;

            Console.Write("New Service Date (" + record.ServiceDate.ToString("Month-Day-Year") + "): ");
            string dateInput = Console.ReadLine();
            DateTime date;
            if (!string.IsNullOrEmpty(dateInput) && DateTime.TryParse(dateInput, out date))
                record.ServiceDate = date;

            record.UpdatedAt = DateTime.Now;
            record.Checksum = repository.ComputeChecksum(record);
            repository.SaveAllRecords(records);
            repository.LogAction("Update", "Updated RecordId " + record.RecordId);
            Console.WriteLine("Record updated successfully!");
        }

        // Delete a record (soft or hard)
        static void DeleteRecord(bool soft)
        {
            var records = repository.GetAllRecords();
            Console.Write("Enter Record ID to delete: ");
            int id;
            if (!int.TryParse(Console.ReadLine(), out id)) { Console.WriteLine("Invalid ID"); return; }

            ServiceRecord record = null;
            foreach (var r in records)
                if (r.RecordId == id) record = r;

            if (record == null) { Console.WriteLine("Record not found"); return; }

            if (soft)
            {
                record.IsActive = false;
                record.UpdatedAt = DateTime.Now;
                record.Checksum = repository.ComputeChecksum(record);
                repository.LogAction("SoftDelete", "Soft deleted RecordId " + record.RecordId);
                Console.WriteLine("Record soft deleted.");
            }
            else
            {
                records.Remove(record);
                repository.LogAction("HardDelete", "Hard deleted RecordId " + record.RecordId);
                Console.WriteLine("Record hard deleted.");
            }

            repository.SaveAllRecords(records);
        }

        // Generate a simple report
        static void GenerateReport()
        {
            var records = repository.GetAllRecords();
            reportGenerator.GenerateReport(records);
        }
    }
}
