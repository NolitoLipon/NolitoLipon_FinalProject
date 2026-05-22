using System;
using System.IO;

namespace VehicleServiceRecords.Services
{
    public class AuditLogger
    {
        private readonly string _auditFile;
        private readonly object _lock = new object();

        public AuditLogger(string auditFile)
        {
            _auditFile = auditFile;
        }

        public void Log(string action, string details)
        {
            lock (_lock)
            {
                try
                {
                    string entry = string.Format("[{0}] ACTION={1} | {2}",
                        DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        action.PadRight(8),
                        details);
                    File.AppendAllText(_auditFile, entry + Environment.NewLine);
                }
                catch (Exception ex)
                {
                    // Don't crash program due to audit failure; warn instead
                    Console.WriteLine("[AUDIT WARNING] Could not write audit log: " + ex.Message);
                }
            }
        }

        public void LogError(string context, string error)
        {
            Log("ERROR", string.Format("Context={0} | Error={1}", context, error));
        }

        public void LogRead(string details)    { Log("READ",    details); }
        public void LogAdd(string details)     { Log("ADD",     details); }
        public void LogUpdate(string details)  { Log("UPDATE",  details); }
        public void LogDelete(string details)  { Log("DELETE",  details); }
        public void LogReport(string details)  { Log("REPORT",  details); }
        public void LogSystem(string details)  { Log("SYSTEM",  details); }
    }
}
