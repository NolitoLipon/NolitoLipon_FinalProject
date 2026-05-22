using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using VehicleServiceRecords.Models;

namespace VehicleServiceRecords.Services
{
    public class ReportGenerator
    {
        private readonly string _reportDir;
        private readonly AuditLogger _audit;

        public ReportGenerator(string reportDir, AuditLogger audit)
        {
            _reportDir = reportDir;
            _audit     = audit;
        }

        // ---- Report 1: Summary by Service Type ----
        public string GenerateServiceTypeSummary(List<ServiceRecord> records)
        {
            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var totals = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

            foreach (ServiceRecord r in records)
            {
                if (!r.IsActive) continue;
                if (!counts.ContainsKey(r.ServiceType)) { counts[r.ServiceType] = 0; totals[r.ServiceType] = 0; }
                counts[r.ServiceType]++;
                totals[r.ServiceType] += r.ServiceCost;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=================================================================");
            sb.AppendLine("          VEHICLE SERVICE RECORDS — SERVICE TYPE SUMMARY         ");
            sb.AppendLine("  Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine("=================================================================");
            sb.AppendLine(string.Format("{0,-25} {1,8} {2,14}", "Service Type", "Count", "Total Cost"));
            sb.AppendLine(new string('-', 50));

            decimal grandTotal = 0;
            int grandCount     = 0;
            foreach (string key in counts.Keys)
            {
                sb.AppendLine(string.Format("{0,-25} {1,8} {2,14:F2}", key, counts[key], totals[key]));
                grandTotal += totals[key];
                grandCount += counts[key];
            }
            sb.AppendLine(new string('-', 50));
            sb.AppendLine(string.Format("{0,-25} {1,8} {2,14:F2}", "TOTAL", grandCount, grandTotal));
            sb.AppendLine("=================================================================");

            return SaveReport("service_type_summary", sb.ToString());
        }

        // ---- Report 2: Monthly Revenue Report ----
        public string GenerateMonthlyRevenue(List<ServiceRecord> records)
        {
            var monthly = new SortedDictionary<string, decimal>();
            var monthlyCounts = new SortedDictionary<string, int>();

            foreach (ServiceRecord r in records)
            {
                if (!r.IsActive) continue;
                string key = r.ServiceDate.ToString("yyyy-MM");
                if (!monthly.ContainsKey(key)) { monthly[key] = 0; monthlyCounts[key] = 0; }
                monthly[key]       += r.ServiceCost;
                monthlyCounts[key] += 1;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=================================================================");
            sb.AppendLine("          VEHICLE SERVICE RECORDS — MONTHLY REVENUE REPORT       ");
            sb.AppendLine("  Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine("=================================================================");
            sb.AppendLine(string.Format("{0,-12} {1,8} {2,14} {3,14}", "Month", "Jobs", "Revenue", "Avg Cost"));
            sb.AppendLine(new string('-', 52));

            decimal grandTotal = 0;
            int grandCount     = 0;
            foreach (string key in monthly.Keys)
            {
                decimal avg = monthlyCounts[key] > 0 ? monthly[key] / monthlyCounts[key] : 0;
                sb.AppendLine(string.Format("{0,-12} {1,8} {2,14:F2} {3,14:F2}",
                    key, monthlyCounts[key], monthly[key], avg));
                grandTotal += monthly[key];
                grandCount += monthlyCounts[key];
            }
            sb.AppendLine(new string('-', 52));
            decimal grandAvg = grandCount > 0 ? grandTotal / grandCount : 0;
            sb.AppendLine(string.Format("{0,-12} {1,8} {2,14:F2} {3,14:F2}", "TOTAL", grandCount, grandTotal, grandAvg));
            sb.AppendLine("=================================================================");

            return SaveReport("monthly_revenue", sb.ToString());
        }

        // ---- Report 3: Top Vehicles by Spend ----
        public string GenerateTopVehicles(List<ServiceRecord> records, int topN)
        {
            var spend  = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            var visits = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var owners = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (ServiceRecord r in records)
            {
                if (!r.IsActive) continue;
                string plate = r.VehiclePlate.Trim().ToUpper();
                if (!spend.ContainsKey(plate))  { spend[plate] = 0; visits[plate] = 0; owners[plate] = r.OwnerName; }
                spend[plate]  += r.ServiceCost;
                visits[plate] += 1;
            }

            // Collect and sort plates by spend descending
            List<string> plateList = new List<string>(spend.Keys);
            plateList.Sort(delegate(string a, string b) { return spend[b].CompareTo(spend[a]); });

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=================================================================");
            sb.AppendLine("       VEHICLE SERVICE RECORDS — TOP VEHICLES BY TOTAL SPEND     ");
            sb.AppendLine("  Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine("=================================================================");
            sb.AppendLine(string.Format("{0,5} {1,-12} {2,-25} {3,8} {4,14}", "Rank", "Plate", "Owner", "Visits", "Total Spend"));
            sb.AppendLine(new string('-', 68));

            int rank = 1;
            foreach (string plate in plateList)
            {
                if (rank > topN) break;
                sb.AppendLine(string.Format("{0,5} {1,-12} {2,-25} {3,8} {4,14:F2}",
                    rank++, plate, owners[plate], visits[plate], spend[plate]));
            }
            sb.AppendLine("=================================================================");

            return SaveReport("top_vehicles", sb.ToString());
        }

        // ---- Report 4: Full Active Records Listing ----
        public string GenerateFullListing(List<ServiceRecord> records)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=================================================================");
            sb.AppendLine("          VEHICLE SERVICE RECORDS — FULL ACTIVE LISTING          ");
            sb.AppendLine("  Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine("=================================================================");

            int count = 0;
            foreach (ServiceRecord r in records)
            {
                if (!r.IsActive) continue;
                count++;
                sb.AppendLine(string.Format("  RecordId   : {0}", r.RecordId));
                sb.AppendLine(string.Format("  Plate      : {0}", r.VehiclePlate));
                sb.AppendLine(string.Format("  Owner      : {0}", r.OwnerName));
                sb.AppendLine(string.Format("  Service    : {0}", r.ServiceType));
                sb.AppendLine(string.Format("  Cost       : {0:F2}", r.ServiceCost));
                sb.AppendLine(string.Format("  Technician : {0}", r.Technician));
                sb.AppendLine(string.Format("  Svc Date   : {0:yyyy-MM-dd}", r.ServiceDate));
                sb.AppendLine(string.Format("  Notes      : {0}", r.Notes));
                sb.AppendLine(string.Format("  Created    : {0:yyyy-MM-dd HH:mm:ss}", r.CreatedAt));
                sb.AppendLine(string.Format("  Updated    : {0:yyyy-MM-dd HH:mm:ss}", r.UpdatedAt));
                sb.AppendLine(string.Format("  Checksum   : {0}", r.Checksum));
                sb.AppendLine(new string('-', 50));
            }
            sb.AppendLine(string.Format("  Total Active Records: {0}", count));
            sb.AppendLine("=================================================================");

            return SaveReport("full_listing", sb.ToString());
        }

        private string SaveReport(string name, string content)
        {
            string filename = string.Format("{0}_{1}.txt",
                name, DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            string path = Path.Combine(_reportDir, filename);
            File.WriteAllText(path, content);
            _audit.LogReport("Generated: " + filename);
            return path;
        }
    }
}
