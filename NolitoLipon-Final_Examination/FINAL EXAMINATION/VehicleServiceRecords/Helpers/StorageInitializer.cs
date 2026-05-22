using System;
using System.IO;
using VehicleServiceRecords.Services;

namespace VehicleServiceRecords.Helpers
{
    public static class StorageInitializer
    {
        public static void Initialize(string dataDir, string reportDir, string dataFile, string auditFile, AuditLogger audit)
        {
            bool firstRun = false;

            // Create directories
            if (!Directory.Exists(dataDir))
            {
                Directory.CreateDirectory(dataDir);
                firstRun = true;
            }
            if (!Directory.Exists(reportDir))
                Directory.CreateDirectory(reportDir);

            // Create data file with header if missing
            if (!File.Exists(dataFile))
            {
                File.WriteAllLines(dataFile, new string[]
                {
                    "# VehicleServiceRecords Data File — DO NOT EDIT MANUALLY",
                    "# Format: RecordId|Plate|Owner|ServiceType|Cost|Technician|Notes|ServiceDate|CreatedAt|UpdatedAt|IsActive|Checksum"
                });
                firstRun = true;
            }

            // Create audit file if missing
            if (!File.Exists(auditFile))
            {
                File.WriteAllText(auditFile, "# VehicleServiceRecords Audit Log" + Environment.NewLine);
            }

            if (firstRun)
                audit.LogSystem("Storage initialized. DataDir=" + dataDir);
            else
                audit.LogSystem("Application started. Existing data loaded.");
        }
    }
}
