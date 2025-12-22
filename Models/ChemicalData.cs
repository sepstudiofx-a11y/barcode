using System.Collections.Generic;
using System.Linq;

namespace ReagentBarcode.Models
{
    public static class ChemicalData
    {
        public static readonly List<ChemicalItem> Chemicals = new()
        {
            new("ALAT", "015", "ALT"),
            new("AMYLASE", "017", "AMYL"),
            new("ASAT", "016", "AST"),
            new("CALCIUM ARSENAZO", "059", "CA", "CA ARS"),
            new("CHOLESTEROL", "002", "CHOL"),
            new("CREA ENZ", "071", "CREA", "CREATININE"),
            new("GGT", "022"),
            new("GLUCOSE", "001", "GLUC"),
            new("GTT", "024"),
            new("HbA1c DIRECT", "031", "HBA1C"),
            new("HDL DIRECT", "025", "HDL"),
            new("LDL DIRECT", "026", "LDL"),
            new("MAGNESIUM", "012", "MG"),
            new("PHOSPHORUS", "013", "PHOS"),
            new("RF", "031"),
            new("TRIGLYCERIDES", "003", "TG"),
            new("TOTAL IgE", "034", "IgE"),
            new("UA II GEN", "009", "UA", "URIC ACID"),
            new("UREA II GEN", "010", "UREA"),
            new("CK", "019"),
            new("CRP ULTRA", "027", "CRP"),
            new("BILIRUBIN DIRECT", "007", "BIL D"),
            new("ALP", "018"),
            new("ALBUMIN", "004", "ALB"),
            new("TOTAL BILIRUBIN", "006")
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
                c.Aliases.Any(a => n.Contains(a)));
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
