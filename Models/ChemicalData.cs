using System.Collections.Generic;
using System.Linq;

namespace ReagentBarcode.Models
{
    public static class ChemicalData
    {
        public static readonly List<ChemicalItem> Chemicals = new()
        {
            new("GLUCOSE", "001", "GLUC"),
            new("CHOLESTEROL", "002", "CHOL"),
            new("TRIGLYCERIDES", "003", "TG"),
            new("ALBUMIN", "004", "ALB"),
            new("TOTAL PROTEIN", "005", "TP"),
            new("TOTAL BILIRUBIN", "006", "BIL T"),
            new("BILIRUBIN DIRECT", "007", "BIL D"),
            new("UA II GEN", "009", "UA", "URIC ACID"),
            new("UREA II GEN", "010", "UREA"),
            new("MAGNESIUM", "012", "MG"),
            new("PHOSPHORUS", "013", "PHOS"),
            new("ALAT", "015", "ALT"),
            new("ASAT", "016", "AST"),
            new("AMYLASE", "017", "AMYL"),
            new("ALP", "018"),
            new("CK", "019"),
            new("LDH", "020"),
            new("GGT", "022"),
            new("GTT", "024"),
            new("HDL DIRECT", "025", "HDL", "HDL D"),
            new("LDL DIRECT", "026", "LDL", "LDL D"),
            new("CRP ULTRA", "027", "CRP", "CRP U"),
            new("RF", "031"),
            new("TOTAL IgE", "074", "IgE"),
            new("CALCIUM ARSENAZO", "059", "CA", "CA ARS"),
            new("HbA1c DIRECT", "061", "HBA1C", "HBA1C D", "HbA1c D"),
            new("CREA ENZ", "071", "CREA", "CREATININE")
        };

        public static readonly List<BottleOption> Bottles = new()
        {
            new("20ml", "1"),
            new("40ml", "2"),
            new("60ml", "3"),
            new("Standard", "1") 
        };

        public static readonly List<ReagentOption> Reagents = new()
        {
            new("R1", "1"),
            new("R2", "2")
        };

        public static ChemicalItem? FindByAnyName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            string n = name.ToUpperInvariant();
            return Chemicals.FirstOrDefault(c => 
                c.Name.Equals(n, System.StringComparison.OrdinalIgnoreCase) || 
                c.Aliases.Any(a => n.Contains(a)) ||
                n.Contains(c.Name.ToUpperInvariant()));
        }
    }

    public class ChemicalItem
    {
        public string Name { get; set; }
        public string DefaultCode { get; set; }
        public List<string> Aliases { get; set; }

        public ChemicalItem(string name, string code, params string[] aliases)
        {
            Name = name;
            DefaultCode = code;
            Aliases = aliases.Select(a => a.ToUpperInvariant()).ToList();
        }
    }

    public record BottleOption(string Name, string Code);
    public record ReagentOption(string Name, string Code);
}
