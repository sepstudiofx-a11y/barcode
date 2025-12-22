using ReagentBarcode.Models;

namespace ReagentBarcode.Services
{
    /// <summary>
    /// Service for managing barcode generation history
    /// </summary>
    public class BarcodeHistoryService
    {
        private readonly List<BarcodeHistory> _history = new();
        private int _nextId = 1;

        /// <summary>
        /// Add a barcode result to history
        /// </summary>
        /// <summary>
        /// Add a barcode result to history
        /// </summary>
        /// <summary>
        /// Add a barcode result to history
        /// </summary>
        public int AddToHistory(BarcodeResult result)
        {
            var historyItem = new BarcodeHistory
            {
                Id = _nextId++,
                BarcodeNumber = result.BarcodeNumber,
                Chem = result.Chem,
                BottleType = result.BottleType,
                RgtType = result.RgtType,
                LotNumber = result.LotNumber,
                SerialNumber = result.SerialNumber,
                ExpDate = result.ExpDate,
                GeneratedAt = result.GeneratedAt,
                LabelText = result.LabelText
            };

            _history.Add(historyItem);

            // Keep only last 100 items to prevent memory issues
            if (_history.Count > 100)
            {
                _history.RemoveAt(0);
            }

            return historyItem.Id;
        }

        /// <summary>
        /// Get all barcode history (always returns fresh data from the in-memory list)
        /// This ensures we're not using any cached data
        /// </summary>
        public IEnumerable<BarcodeHistory> GetHistory()
        {
            // Always return a fresh enumeration of the current history
            // Order by newest first (most recent at the top)
            return _history.OrderByDescending(h => h.GeneratedAt).ToList();
        }

        /// <summary>
        /// Get history by chemical
        /// </summary>
        public IEnumerable<BarcodeHistory> GetHistoryByChemical(string chem)
        {
            return _history
                .Where(h => h.Chem.Contains(chem, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(h => h.GeneratedAt);
        }

        /// <summary>
        /// Clear all history
        /// </summary>
        public void ClearHistory()
        {
            _history.Clear();
            _nextId = 1;
        }

        /// <summary>
        /// Get statistics
        /// </summary>
        public BarcodeStats GetStats()
        {
            return new BarcodeStats
            {
                TotalGenerated = _history.Count,
                ChemicalsCount = _history.Select(h => h.Chem).Distinct().Count(),
                RecentCount = _history.Count(h => h.GeneratedAt > DateTime.Now.AddHours(-24))
            };
        }
    }

    /// <summary>
    /// Statistics model
    /// </summary>
    public class BarcodeStats
    {
        public int TotalGenerated { get; set; }
        public int ChemicalsCount { get; set; }
        public int RecentCount { get; set; }
    }
}
