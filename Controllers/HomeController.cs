using Microsoft.AspNetCore.Mvc;
using ReagentBarcode.Models;
using ReagentBarcode.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ZXing;
using ZXing.Common;
using System.Drawing;
using System.Drawing.Imaging;
using SDColor = System.Drawing.Color;
using SDImageFormat = System.Drawing.Imaging.ImageFormat;

namespace ReagentBarcode.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly BarcodeService _barcodeService;
        private readonly BarcodeHistoryService _historyService;
        private readonly LicenseService _licenseService;

        public HomeController(
            ILogger<HomeController> logger,
            BarcodeService barcodeService,
            BarcodeHistoryService historyService,
            LicenseService licenseService)
        {
            _logger = logger;
            _barcodeService = barcodeService;
            _historyService = historyService;
            _licenseService = licenseService;
        }

        public IActionResult Index()
        {
            ViewBag.Stats = _historyService.GetStats();
            ViewBag.IsLicenseValid = _licenseService.IsLicenseValid();
            ViewBag.RemainingDays = _licenseService.GetRemainingDays();
            return View();
        }

        public IActionResult History()
        {
            var history = _historyService.GetHistory();
            return View(history);
        }

        [HttpPost]
        public IActionResult ClearHistory()
        {
            _historyService.ClearHistory();
            TempData["Success"] = "History cleared successfully!";
            return RedirectToAction("History");
        }

        /// <summary>
        /// Print selected barcodes from history table
        /// </summary>
        [HttpPost]
        public IActionResult PrintSelected(string ids)
        {
            if (string.IsNullOrEmpty(ids))
            {
                TempData["Error"] = "Please select at least one barcode to print.";
                return RedirectToAction("History");
            }

            var idList = ids.Split(',', StringSplitOptions.RemoveEmptyEntries)
                           .Select(id => int.TryParse(id, out int val) ? val : -1)
                           .Where(id => id > 0)
                           .ToList();
            
            if (!idList.Any())
            {
                TempData["Error"] = "Invalid selection.";
                return RedirectToAction("History");
            }

            var history = _historyService.GetHistory().ToList();
            var itemsToPrint = idList.Join(history, 
                                          id => id, 
                                          h => h.Id, 
                                          (id, h) => h)
                                      .ToList();
            
            if (!itemsToPrint.Any())
            {
                TempData["Error"] = "No barcodes found to print.";
                return RedirectToAction("History");
            }
            
            byte[] pdfBytes = GenerateBatchBarcodePdf(itemsToPrint);
            var fileName = $"Barcode_Labels_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }

        /// <summary>
        /// Single print function - generates PDF with horizontal barcode labels (14cm x 3.5cm)
        /// </summary>
        [HttpPost]
        public IActionResult PrintPdf(string? ids = null)
        {
            var history = _historyService.GetHistory().ToList();

            if (!string.IsNullOrEmpty(ids))
            {
                var idList = ids.Split(',', StringSplitOptions.RemoveEmptyEntries)
                               .Select(id => int.TryParse(id, out int val) ? val : -1)
                               .Where(id => id > 0)
                               .ToList();
                
                if (idList.Any())
                {
                    history = idList.Join(history, 
                                        id => id, 
                                        h => h.Id, 
                                        (id, h) => h)
                                    .ToList();
                }
            }
            else
            {
                history = history.OrderByDescending(h => h.Id).ToList();
            }
            
            if (history.Count == 0)
            {
                return BadRequest(new { success = false, error = "No barcodes to print" });
            }
            
            byte[] pdfBytes = GenerateBatchBarcodePdf(history);
            var fileName = $"Generated_Barcodes_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                return File(pdfBytes, "application/pdf", fileName);
            }
            
        /// <summary>
        /// Generates PDF with horizontal barcode labels (14cm x 3.5cm each)
        /// </summary>
        private byte[] GenerateBatchBarcodePdf(List<BarcodeHistory> itemsToPrint)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1, Unit.Centimetre);

                    page.Content()
                        .PaddingVertical(10)
                        .Column(mainColumn =>
                        {
                            mainColumn.Spacing(5);

                            foreach (var item in itemsToPrint)
                            {
                                string lotNumber = item.LotNumber?.TrimStart('0') ?? "";
                                string displayCode = $"{item.RgtType ?? "R1"} 7-{lotNumber}";

                                mainColumn.Item().Element(labelContainer =>
                                {
                                    RenderSingleHorizontalLabel(labelContainer, displayCode, item.BarcodeNumber);
                                });
                            }
                        });
                });
            }).GeneratePdf();
        }

        /// <summary>
        /// Renders a single horizontal label (14cm x 3.5cm) with barcode and display code
        /// </summary>
        private void RenderSingleHorizontalLabel(IContainer container, string code, string barcodeNumber)
        {
            // Generate the barcode image (includes the human-readable text underneath it)
            var barcodeImage = GenerateBarcodeImage(barcodeNumber, includeText: true);

            // Define the critical dimensions in Centimetres
            const float labelWidth = 14f;
            const float labelHeight = 3.5f;

            // *** FIX HERE: Reduced barcode area width further to 9.14f to resolve floating point precision error. ***
            // 9.14 + 2.5 + 2.0 = 13.64 cm. This provides a small buffer inside the 14cm container with 5pt padding.
            const float barcodeAreaWidth = 9.14f; // Was 9.15f

            const float spacerWidth = 2.5f;        // Fixed width for the gap
            const float codeTextWidth = 2.0f;      // Fixed width for the display code text

            container
                .Width(labelWidth, Unit.Centimetre)
                .Height(labelHeight, Unit.Centimetre)
                .Background(Colors.White)
                .Padding(5) // 5 points padding
                .AlignLeft()
                .Row(labelRow =>
                {
                    // 1. Barcode Area (9.14 cm)
                    labelRow.ConstantItem(barcodeAreaWidth, Unit.Centimetre)
                        .Column(barcodeColumn =>
                        {
                            barcodeColumn.Item()
                                // Ensure the image fits vertically in the available space
                                .MaxHeight(labelHeight - 1, Unit.Centimetre)
                                .AlignCenter()
                                .Image(barcodeImage)
                                .FitArea(); // FitArea maintains aspect ratio for the barcode/number
                        });

                    // 2. Spacer Area (2.5 cm)
                    // Uses .Extend() to reliably create a fixed-size empty element.
                    labelRow.ConstantItem(spacerWidth, Unit.Centimetre)
                        .Element(e => e.Extend());

                    // 3. Display Code Text Area (2.0 cm)
                    labelRow.ConstantItem(codeTextWidth, Unit.Centimetre)
                        .AlignRight()
                        .AlignMiddle()
                        .Text(code)
                        .FontSize(14)
                        .Bold();
            });
        }

        /// <summary>
        /// Generates barcode image as byte array
        /// </summary>
        private byte[] GenerateBarcodeImage(string barcodeNumber, bool includeText)
        {
            var writer = new BarcodeWriterPixelData
            {
                Format = BarcodeFormat.CODE_128,
                Options = new EncodingOptions
                {
                    Height = 300,
                    Width = 1600,
                    Margin = 10,
                    PureBarcode = !includeText
                }
            };

            var pixelData = writer.Write(barcodeNumber);
            using (var bitmap = new Bitmap(pixelData.Width, pixelData.Height, PixelFormat.Format32bppRgb))
            {
                using (var ms = new MemoryStream())
                {
                    var bitmapData = bitmap.LockBits(
                        new Rectangle(0, 0, pixelData.Width, pixelData.Height),
                        ImageLockMode.WriteOnly,
                        PixelFormat.Format32bppRgb);

                    try
                    {
                        System.Runtime.InteropServices.Marshal.Copy(
                            pixelData.Pixels, 0, bitmapData.Scan0, pixelData.Pixels.Length);
                    }
                    finally
                    {
                        bitmap.UnlockBits(bitmapData);
                    }
                    bitmap.Save(ms, SDImageFormat.Png);
                    return ms.ToArray();
                }
            }
        }

        /// <summary>
        /// Test all 48 test cases
        /// </summary>
        [HttpPost]
        public IActionResult TestAllBarcode()
        {
            var testCases = GetTestCases();
            var results = new List<object>();
            int passCount = 0;
            int failCount = 0;

            foreach (var testCase in testCases)
            {
                try
                {
                    var input = new ReagentInput
                            {
                                Chem = testCase.Chem,
                        BottleType = testCase.Bottle,
                        RgtType = testCase.Rgt,
                        LotNumber = testCase.Lot,
                        SerialNumber = testCase.Serial,
                        ExpDate = testCase.Expiry
                    };

                    var result = _barcodeService.GenerateBarcode(input);
                    
                    bool passed = result.Success && result.BarcodeNumber == testCase.Expected;
                    
                    if (passed) passCount++;
                    else failCount++;

                results.Add(new
                {
                        TestNumber = testCase.TestNumber,
                        Chem = testCase.Chem,
                        Bottle = testCase.Bottle,
                        Rgt = testCase.Rgt,
                        Lot = testCase.Lot,
                        Serial = testCase.Serial,
                        Expiry = testCase.Expiry,
                        Expected = testCase.Expected,
                        Actual = result.BarcodeNumber ?? "ERROR",
                        Passed = passed,
                        Error = result.ErrorMessage
                    });
                }
                catch (Exception ex)
                {
                    failCount++;
                results.Add(new
                {
                        TestNumber = testCase.TestNumber,
                        Chem = testCase.Chem,
                        Bottle = testCase.Bottle,
                        Rgt = testCase.Rgt,
                        Lot = testCase.Lot,
                        Serial = testCase.Serial,
                        Expiry = testCase.Expiry,
                        Expected = testCase.Expected,
                        Actual = "EXCEPTION",
                        Passed = false,
                        Error = ex.Message
                    });
                }
            }

            return Json(new
            {
                Total = testCases.Count,
                Passed = passCount,
                Failed = failCount,
                Results = results
            });
        }

        private List<TestCase> GetTestCases()
        {
            return new List<TestCase>
            {
                new(1, "UREA II GEN", "40ml", "R1", "009", "8932", "09/30/2024", "01021240930200989327"),
                new(2, "UREA II GEN", "40ml", "R1", "009", "8951", "09/30/2024", "01021240930900989513"),
                new(3, "UREA II GEN", "20ml", "R2", "009", "9442", "09/30/2024", "01012240930000994429"),
                new(4, "TOTAL IgE", "40ml", "R1", "073", "5151", "07/31/2025", "07421250731507351515")
            };
        }

        private class TestCase
        {
            public int TestNumber { get; }
            public string Chem { get; }
            public string Bottle { get; }
            public string Rgt { get; }
            public string Lot { get; }
            public string Serial { get; }
            public string Expiry { get; }
            public string Expected { get; }

            public TestCase(int testNumber, string chem, string bottle, string rgt, string lot, string serial, string expiry, string expected)
            {
                TestNumber = testNumber;
                Chem = chem;
                Bottle = bottle;
                Rgt = rgt;
                Lot = lot;
                Serial = serial;
                Expiry = expiry;
                Expected = expected;
            }
        }
    }
}
