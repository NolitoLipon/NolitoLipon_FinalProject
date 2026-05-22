using System;
using System.Collections.Generic;
using VehicleServiceRecords.Models;

namespace VehicleServiceRecords.Helpers
{
    public static class ConsoleHelper
    {
        public static void WriteHeader(string title)
        {
            Console.WriteLine();
            Console.WriteLine(new string('=', 60));
            Console.WriteLine("  " + title.ToUpper());
            Console.WriteLine(new string('=', 60));
        }

        public static void WriteSeparator() { Console.WriteLine(new string('-', 60)); }

        public static void WriteSuccess(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[OK] " + msg);
            Console.ResetColor();
        }

        public static void WriteError(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[ERROR] " + msg);
            Console.ResetColor();
        }

        public static void WriteWarning(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("[WARN] " + msg);
            Console.ResetColor();
        }

        public static void WriteInfo(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("[INFO] " + msg);
            Console.ResetColor();
        }

        public static string PromptRequired(string label)
        {
            string value = "";
            while (string.IsNullOrWhiteSpace(value))
            {
                Console.Write("  " + label + ": ");
                value = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(value))
                    WriteError("This field is required.");
            }
            return value.Trim();
        }

        public static string PromptOptional(string label, string defaultValue = "")
        {
            Console.Write("  " + label);
            if (!string.IsNullOrEmpty(defaultValue))
                Console.Write(" [" + defaultValue + "]");
            Console.Write(": ");
            string v = Console.ReadLine();
            return string.IsNullOrWhiteSpace(v) ? defaultValue : v.Trim();
        }

        public static void PrintRecordShort(ServiceRecord r)
        {
            Console.WriteLine(string.Format("  {0,-10} {1,-12} {2,-22} {3,-18} {4,10:F2}  {5,-12}",
                r.RecordId,
                r.VehiclePlate,
                r.OwnerName.Length > 22 ? r.OwnerName.Substring(0, 19) + "..." : r.OwnerName,
                r.ServiceType,
                r.ServiceCost,
                r.ServiceDate.ToString("yyyy-MM-dd")));
        }

        public static void PrintRecordDetail(ServiceRecord r)
        {
            Console.WriteLine();
            WriteSeparator();
            Console.WriteLine(string.Format("  Record ID   : {0}", r.RecordId));
            Console.WriteLine(string.Format("  Plate       : {0}", r.VehiclePlate));
            Console.WriteLine(string.Format("  Owner       : {0}", r.OwnerName));
            Console.WriteLine(string.Format("  Service     : {0}", r.ServiceType));
            Console.WriteLine(string.Format("  Cost        : {0:F2}", r.ServiceCost));
            Console.WriteLine(string.Format("  Technician  : {0}", r.Technician));
            Console.WriteLine(string.Format("  Service Date: {0:yyyy-MM-dd}", r.ServiceDate));
            Console.WriteLine(string.Format("  Notes       : {0}", string.IsNullOrWhiteSpace(r.Notes) ? "(none)" : r.Notes));
            Console.WriteLine(string.Format("  Status      : {0}", r.IsActive ? "ACTIVE" : "INACTIVE"));
            WriteSeparator();
            Console.WriteLine(string.Format("  Created     : {0:yyyy-MM-dd HH:mm:ss}", r.CreatedAt));
            Console.WriteLine(string.Format("  Updated     : {0:yyyy-MM-dd HH:mm:ss}", r.UpdatedAt));
            Console.WriteLine(string.Format("  Checksum    : {0}", r.Checksum));
            WriteSeparator();
        }

        public static void PrintRecordTableHeader()
        {
            Console.WriteLine(string.Format("  {0,-10} {1,-12} {2,-22} {3,-18} {4,10}  {5}",
                "RecordId", "Plate", "Owner", "ServiceType", "Cost", "SvcDate"));
            Console.WriteLine("  " + new string('-', 87));
        }

        public static void PressAnyKey()
        {
            Console.WriteLine();
            Console.Write("  Press any key to continue...");
            Console.ReadKey(true);
        }

        public static bool Confirm(string message)
        {
            Console.Write("  " + message + " (y/n): ");
            string ans = Console.ReadLine();
            return ans != null && ans.Trim().ToLower() == "y";
        }
    }
}
