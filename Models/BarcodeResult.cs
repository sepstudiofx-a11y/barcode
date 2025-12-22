namespace ReagentBarcode.Models
{
    /// <summary>
    /// Result model for barcode generation
    /// </summary>
    public class BarcodeResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public int? Id { get; set; }
        public string BarcodeNumber { get; set; } = string.Empty;
        public string BarcodeImageBase64 { get; set; } = string.Empty;
        public string LabelText { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; } = DateTime.Now;

        // Additional display properties
        // Additional display properties
        public string Chem { get; set; } = string.Empty;
        public string GenItemCode { get; set; } = string.Empty;// Display the used code
        public string GenBottleCode { get; set; } = string.Empty;
        public string GenReagentCode { get; set; } = string.Empty;
        
        public string BottleType { get; set; } = string.Empty;
        public string RgtType { get; set; } = string.Empty;
        public string LotNumber { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
        public DateTime ExpDate { get; set; }
    }
}
