namespace ReagentBarcode.Models
{
    /// <summary>
    /// Input model for reagent barcode generation
    /// No validations - fully dynamic input
    /// </summary>
    public class ReagentInput
    {
        public string? ItemCode { get; set; } // e.g. "010"
        public string? BottleCode { get; set; } // e.g. "2"
        public string? ReagentCode { get; set; } // e.g. "1"

        // Optional/Legacy (kept for UI mapping if needed, but logic will use Codes)
        public string? Chem { get; set; }
        public string? BottleType { get; set; }
        public string? RgtType { get; set; }
        
        public string? LotNumber { get; set; } // Full 4-digit lot (e.g. "2009")
        public string? SerialNumber { get; set; }
        public string? ExpDate { get; set; }
        public int? Position { get; set; }
    }
}
