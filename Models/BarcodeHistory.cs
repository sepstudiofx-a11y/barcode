namespace ReagentBarcode.Models
{
    /// <summary>
    /// Model for storing barcode generation history
    /// </summary>
    public class BarcodeHistory
    {
        public int Id { get; set; }
        public string BarcodeNumber { get; set; } = string.Empty;
        public string Chem { get; set; } = string.Empty;
        public string BottleType { get; set; } = string.Empty;
        public string RgtType { get; set; } = string.Empty;
        public string LotNumber { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
        public DateTime ExpDate { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.Now;
        public string? LabelText { get; set; }

        // For display purposes
        public string DisplayText => $"{Chem} {BottleType} {RgtType} - {BarcodeNumber}";
        public string FormattedExpDate => ExpDate.ToString("MMM dd, yyyy");
        public string FormattedGeneratedAt => GeneratedAt.ToString("MMM dd, yyyy HH:mm:ss");
    }
}
