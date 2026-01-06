using System.Collections.Generic;
using System.Linq;

namespace ReagentBarcode.Models
{
    public static class ChemicalData
    {
        public static readonly List<ChemicalItem> Chemicals = new()
        {
            new("UREA II GEN", "010", "UREA", "UREA IIGEN"),
            new("IgE", "034", "TOTAL IgE")
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
