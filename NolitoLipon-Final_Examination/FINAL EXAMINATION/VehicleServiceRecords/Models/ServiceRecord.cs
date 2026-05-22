using System;

namespace VehicleServiceRecords.Models
{
    public class ServiceRecord
    {
        public string RecordId       { get; set; }
        public string VehiclePlate   { get; set; }   // domain field 1
        public string OwnerName      { get; set; }   // domain field 2
        public string ServiceType    { get; set; }   // domain field 3
        public decimal ServiceCost   { get; set; }   // domain field 4
        public string Technician     { get; set; }   // domain field 5
        public string Notes          { get; set; }   // domain field 6
        public DateTime ServiceDate  { get; set; }   // domain field 7
        public DateTime CreatedAt    { get; set; }
        public DateTime UpdatedAt    { get; set; }
        public bool   IsActive       { get; set; }
        public string Checksum       { get; set; }

        // Serialize to pipe-delimited line
        public string ToCsvLine()
        {
            return string.Join("|", new string[]
            {
                Escape(RecordId),
                Escape(VehiclePlate),
                Escape(OwnerName),
                Escape(ServiceType),
                ServiceCost.ToString("F2"),
                Escape(Technician),
                Escape(Notes),
                ServiceDate.ToString("yyyy-MM-dd"),
                CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss"),
                UpdatedAt.ToString("yyyy-MM-ddTHH:mm:ss"),
                IsActive ? "1" : "0",
                Escape(Checksum)
            });
        }

        public static ServiceRecord FromCsvLine(string line)
        {
            string[] p = line.Split('|');
            if (p.Length < 12)
                throw new FormatException("Malformed record line: expected 12 fields, got " + p.Length);

            return new ServiceRecord
            {
                RecordId      = Unescape(p[0]),
                VehiclePlate  = Unescape(p[1]),
                OwnerName     = Unescape(p[2]),
                ServiceType   = Unescape(p[3]),
                ServiceCost   = decimal.Parse(p[4]),
                Technician    = Unescape(p[5]),
                Notes         = Unescape(p[6]),
                ServiceDate   = DateTime.Parse(p[7]),
                CreatedAt     = DateTime.Parse(p[8]),
                UpdatedAt     = DateTime.Parse(p[9]),
                IsActive      = p[10] == "1",
                Checksum      = Unescape(p[11])
            };
        }

        // Simple escape: replace | with [PIPE] and newline with [NL]
        private static string Escape(string s)
        {
            if (s == null) return "";
            return s.Replace("|", "[PIPE]").Replace("\n", "[NL]").Replace("\r", "");
        }

        private static string Unescape(string s)
        {
            if (s == null) return "";
            return s.Replace("[PIPE]", "|").Replace("[NL]", "\n");
        }

        public string ComputeChecksum()
        {
            string raw = RecordId + VehiclePlate + OwnerName + ServiceType
                       + ServiceCost.ToString("F2") + Technician + Notes
                       + ServiceDate.ToString("yyyy-MM-dd");
            int hash = 17;
            foreach (char c in raw)
                hash = hash * 31 + c;
            return Math.Abs(hash).ToString("X8");
        }
    }
}
