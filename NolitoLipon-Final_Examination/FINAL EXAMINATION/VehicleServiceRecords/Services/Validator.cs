using System;
using System.Text.RegularExpressions;

namespace VehicleServiceRecords.Services
{
    public class ValidationResult
    {
        public bool IsValid  { get; set; }
        public string Error  { get; set; }

        public static ValidationResult Ok()             { return new ValidationResult { IsValid = true }; }
        public static ValidationResult Fail(string msg) { return new ValidationResult { IsValid = false, Error = msg }; }
    }

    public static class Validator
    {
        // Plate: 2-8 alphanumeric/hyphen/space characters
        private static readonly Regex PlateRegex = new Regex(@"^[A-Za-z0-9\-\s]{2,10}$");

        public static ValidationResult ValidatePlate(string plate)
        {
            if (string.IsNullOrWhiteSpace(plate))
                return ValidationResult.Fail("Vehicle plate cannot be empty.");
            if (!PlateRegex.IsMatch(plate.Trim()))
                return ValidationResult.Fail("Vehicle plate must be 2-10 alphanumeric characters (hyphens/spaces allowed).");
            return ValidationResult.Ok();
        }

        public static ValidationResult ValidateOwnerName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return ValidationResult.Fail("Owner name cannot be empty.");
            if (name.Trim().Length < 2 || name.Trim().Length > 80)
                return ValidationResult.Fail("Owner name must be 2-80 characters.");
            return ValidationResult.Ok();
        }

        public static ValidationResult ValidateServiceType(string serviceType)
        {
            string[] allowed = {
                "oil change", "tire rotation", "brake service", "engine repair",
                "transmission", "ac service", "battery replacement", "inspection",
                "wheel alignment", "clutch repair", "suspension", "electrical", "other"
            };
            if (string.IsNullOrWhiteSpace(serviceType))
                return ValidationResult.Fail("Service type cannot be empty.");
            string lower = serviceType.Trim().ToLower();
            foreach (string a in allowed)
                if (lower == a) return ValidationResult.Ok();
            return ValidationResult.Fail("Invalid service type. Allowed: " + string.Join(", ", allowed));
        }

        public static ValidationResult ValidateCost(string costStr, out decimal cost)
        {
            cost = 0;
            if (string.IsNullOrWhiteSpace(costStr))
                return ValidationResult.Fail("Cost cannot be empty.");
            if (!decimal.TryParse(costStr.Trim(), out cost))
                return ValidationResult.Fail("Cost must be a valid number.");
            if (cost < 0)
                return ValidationResult.Fail("Cost cannot be negative.");
            if (cost > 9999999)
                return ValidationResult.Fail("Cost exceeds maximum allowed value.");
            return ValidationResult.Ok();
        }

        public static ValidationResult ValidateTechnician(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return ValidationResult.Fail("Technician name cannot be empty.");
            if (name.Trim().Length < 2 || name.Trim().Length > 60)
                return ValidationResult.Fail("Technician name must be 2-60 characters.");
            return ValidationResult.Ok();
        }

        public static ValidationResult ValidateServiceDate(string dateStr, out DateTime date)
        {
            date = DateTime.MinValue;
            if (string.IsNullOrWhiteSpace(dateStr))
                return ValidationResult.Fail("Service date cannot be empty.");
            if (!DateTime.TryParse(dateStr.Trim(), out date))
                return ValidationResult.Fail("Invalid date format. Use YYYY-MM-DD.");
            if (date > DateTime.Now.AddDays(1))
                return ValidationResult.Fail("Service date cannot be in the future.");
            if (date < new DateTime(1990, 1, 1))
                return ValidationResult.Fail("Service date cannot be before 1990.");
            return ValidationResult.Ok();
        }
    }
}
