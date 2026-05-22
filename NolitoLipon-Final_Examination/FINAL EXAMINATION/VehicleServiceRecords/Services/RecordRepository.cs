using System;
using System.Collections.Generic;
using System.IO;
using VehicleServiceRecords.Models;

namespace VehicleServiceRecords.Services
{
    public class RecordRepository
    {
        private readonly string _dataFile;
        private readonly string _backupFile;
        private readonly AuditLogger _audit;

        public RecordRepository(string dataFile, string backupFile, AuditLogger audit)
        {
            _dataFile   = dataFile;
            _backupFile = backupFile;
            _audit      = audit;
        }

        // ------------------------------------------------------------------ Load
        public List<ServiceRecord> LoadAll()
        {
            var records = new List<ServiceRecord>();
            if (!File.Exists(_dataFile))
                return records;

            string[] lines;
            try
            {
                lines = File.ReadAllLines(_dataFile);
            }
            catch (IOException ex)
            {
                _audit.LogError("LoadAll", ex.Message);
                throw;
            }

            int lineNo = 0;
            foreach (string line in lines)
            {
                lineNo++;
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    continue;
                try
                {
                    ServiceRecord rec = ServiceRecord.FromCsvLine(line);
                    // Validate checksum on load
                    string expectedChecksum = rec.ComputeChecksum();
                    if (rec.Checksum != expectedChecksum)
                    {
                        _audit.LogError("LoadAll",
                            string.Format("Checksum mismatch on line {0} (RecordId={1}). Expected={2} Got={3}",
                                lineNo, rec.RecordId, expectedChecksum, rec.Checksum));
                        Console.WriteLine(string.Format(
                            "[WARNING] Checksum mismatch on record {0} (line {1}). Record loaded but flagged.",
                            rec.RecordId, lineNo));
                    }
                    records.Add(rec);
                }
                catch (Exception ex)
                {
                    _audit.LogError("LoadAll", string.Format("Malformed line {0}: {1}", lineNo, ex.Message));
                    Console.WriteLine(string.Format("[WARNING] Skipping malformed line {0}: {1}", lineNo, ex.Message));
                }
            }
            return records;
        }

        // ------------------------------------------------------------------ Save (full overwrite)
        public void SaveAll(List<ServiceRecord> records)
        {
            // Backup existing file first
            if (File.Exists(_dataFile))
            {
                try { File.Copy(_dataFile, _backupFile, true); }
                catch (IOException ex)
                {
                    _audit.LogError("SaveAll/Backup", ex.Message);
                }
            }

            try
            {
                using (StreamWriter sw = new StreamWriter(_dataFile, false))
                {
                    sw.WriteLine("# VehicleServiceRecords Data File — DO NOT EDIT MANUALLY");
                    sw.WriteLine("# Format: RecordId|Plate|Owner|ServiceType|Cost|Technician|Notes|ServiceDate|CreatedAt|UpdatedAt|IsActive|Checksum");
                    foreach (ServiceRecord rec in records)
                        sw.WriteLine(rec.ToCsvLine());
                }
            }
            catch (IOException ex)
            {
                _audit.LogError("SaveAll", ex.Message);
                throw;
            }
        }

        // ------------------------------------------------------------------ Add
        public void Add(List<ServiceRecord> records, ServiceRecord newRec)
        {
            newRec.Checksum = newRec.ComputeChecksum();
            records.Add(newRec);
            SaveAll(records);
            _audit.LogAdd(string.Format("RecordId={0} Plate={1} Owner={2} ServiceType={3} Cost={4:F2}",
                newRec.RecordId, newRec.VehiclePlate, newRec.OwnerName,
                newRec.ServiceType, newRec.ServiceCost));
        }

        // ------------------------------------------------------------------ Update
        public bool Update(List<ServiceRecord> records, ServiceRecord updated)
        {
            for (int i = 0; i < records.Count; i++)
            {
                if (records[i].RecordId == updated.RecordId)
                {
                    updated.UpdatedAt = DateTime.Now;
                    updated.Checksum  = updated.ComputeChecksum();
                    records[i] = updated;
                    SaveAll(records);
                    _audit.LogUpdate(string.Format("RecordId={0} Plate={1} ServiceType={2} Cost={3:F2}",
                        updated.RecordId, updated.VehiclePlate, updated.ServiceType, updated.ServiceCost));
                    return true;
                }
            }
            return false;
        }

        // ------------------------------------------------------------------ Soft Delete
        public bool SoftDelete(List<ServiceRecord> records, string recordId)
        {
            foreach (ServiceRecord rec in records)
            {
                if (rec.RecordId == recordId && rec.IsActive)
                {
                    rec.IsActive  = false;
                    rec.UpdatedAt = DateTime.Now;
                    rec.Checksum  = rec.ComputeChecksum();
                    SaveAll(records);
                    _audit.LogDelete(string.Format("SOFT RecordId={0} Plate={1}", recordId, rec.VehiclePlate));
                    return true;
                }
            }
            return false;
        }

        // ------------------------------------------------------------------ Hard Delete
        public bool HardDelete(List<ServiceRecord> records, string recordId)
        {
            for (int i = 0; i < records.Count; i++)
            {
                if (records[i].RecordId == recordId)
                {
                    string plate = records[i].VehiclePlate;
                    records.RemoveAt(i);
                    SaveAll(records);
                    _audit.LogDelete(string.Format("HARD RecordId={0} Plate={1}", recordId, plate));
                    return true;
                }
            }
            return false;
        }

        // ------------------------------------------------------------------ ID Generator
        public string GenerateId(List<ServiceRecord> existing)
        {
            int max = 0;
            foreach (ServiceRecord r in existing)
            {
                string idPart = r.RecordId.Replace("VSR-", "");
                int num;
                if (int.TryParse(idPart, out num) && num > max)
                    max = num;
            }
            return "VSR-" + (max + 1).ToString("D5");
        }
    }
}
