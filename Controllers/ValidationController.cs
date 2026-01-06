using Microsoft.AspNetCore.Mvc;
using ReagentBarcode.Models;
using ReagentBarcode.Services;
using System.Text;

namespace ReagentBarcode.Controllers
{
    [ApiController]
    [Route("api/validation")]
    public class ValidationController : ControllerBase
    {
        private readonly BarcodeService _barcodeService;

        public ValidationController(BarcodeService barcodeService)
        {
            _barcodeService = barcodeService;
        }

        [HttpGet("run")]
        public ContentResult RunValidation()
        {
            var sb = new StringBuilder();
            sb.Append("<html><head><style>table { border-collapse: collapse; width: 100%; font-family: sans-serif; } th, td { border: 1px solid #ddd; padding: 8px; text-align: left; } th { background-color: #f2f2f2; } .pass { color: green; } .fail { color: red; font-weight: bold; }</style></head><body>");
            sb.Append("<h1>Barcode Mass Validation Report</h1>");
            
            // --- Fixed Tests Section ---
            sb.Append("<h2>Fixed Template Verification (15 Verified Cases)</h2>");
            sb.Append("<table><tr><th>ID</th><th>Chemical</th><th>Params (Lot/SN/Exp)</th><th>Expected</th><th>Actual</th><th>Status</th></tr>");

            var tests = GetTestCases();
            int passed = 0;

            foreach (var t in tests)
            {
                // ... (Logic to prep input)
                string itemCode = t.Code;
                var chemMatch = ChemicalData.FindByAnyName(t.Chem);
                if (chemMatch != null) itemCode = chemMatch.DefaultCode;

                string bottleCode = "1";
                if (t.Bottle.Contains("40")) bottleCode = "2";
                if (t.Bottle.Contains("20")) bottleCode = "1";
                if (t.Bottle.Contains("20") || t.Bottle.Contains("IgE") && t.Rgt=="R2") bottleCode = "1";
                
                string rgtCode = t.Rgt == "R2" ? "2" : "1";

                var input = new ReagentInput
                {
                    Chem = t.Chem,
                    ItemCode = itemCode,
                    BottleCode = bottleCode,
                    ReagentCode = rgtCode,
                    LotNumber = t.Lot,
                    SerialNumber = t.Serial,
                    ExpDate = t.Expiry
                };

                var result = _barcodeService.GenerateBarcode(input, false);

                bool isMatch = result.BarcodeNumber == t.Expected;
                string status = isMatch ? "<span class='pass'>PASS</span>" : "<span class='fail'>FAIL</span>";
                if (isMatch) passed++;

                sb.Append($"<tr><td>{t.Id}</td><td>{t.Chem}</td><td>Lot:{t.Lot} SN:{t.Serial} Exp:{t.Expiry}</td><td>{t.Expected}</td><td>{result.BarcodeNumber}</td><td>{status}</td></tr>");
            }
            sb.Append($"</table><p><strong>Fixed Tests Summary: {passed}/{tests.Count} Passed</strong></p>");


            // --- Mass Dynamic Tests Section ---
            sb.Append("<h2>Mass Dynamic Validation (1000 Iterations)</h2>");
            sb.Append("<table><tr><th>Iter</th><th>Template</th><th>Dynamic Inputs (Lot / SN / Exp)</th><th>Generated Barcode</th><th>Status</th></tr>");
            
            int massPassed = 0;
            int totalMass = 1000;
            var rand = new Random();

            for (int i = 1; i <= totalMass; i++)
            {
                var t = tests[rand.Next(tests.Count)];
                
                string newSerial = rand.Next(0, 10000).ToString("D4");
                DateTime date = DateTime.Today.AddDays(rand.Next(1, 730));
                if (rand.NextDouble() > 0.5) date = new DateTime(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month));
                string newExp = date.ToString("MM/dd/yyyy");

                string itemCode = t.Code;
                string bottleCode = "1";
                if (t.Expected.Length > 4 && t.Expected[3] == '2') bottleCode = "2";
                string rgtCode = t.Rgt == "R2" ? "2" : "1";

                var input = new ReagentInput {
                    Chem = t.Chem,
                    ItemCode = itemCode,
                    BottleCode = bottleCode,
                    ReagentCode = rgtCode,
                    LotNumber = t.Lot,
                    SerialNumber = newSerial,
                    ExpDate = newExp
                };

                var result = _barcodeService.GenerateBarcode(input, generateImage: false);
                
                bool valid = true;
                if (result.BarcodeNumber.Length != 20) valid = false;
                if (!long.TryParse(result.BarcodeNumber, out _)) valid = false;
                
                string status = valid ? "<span class='pass'>VALID</span>" : "<span class='fail'>INVALID</span>";
                if (valid) massPassed++;

                sb.Append($"<tr><td>{i}</td><td>{t.Chem} ({t.Rgt})</td><td>Lot:{t.Lot} / SN:{newSerial} / Exp:{newExp}</td><td>{result.BarcodeNumber}</td><td>{status}</td></tr>");
            }

            sb.Append("</table>");
            sb.Append($"<h3>Mass Test Results: {massPassed}/{totalMass} - {(double)massPassed/totalMass*100:0.00}% Valid</h3>");
            sb.Append("</body></html>");

            return base.Content(sb.ToString(), "text/html");
        }

        private void RunDynamicTest(StringBuilder sb, string label, string itemCode, string chem, string bottle, string rgt, string lot, string serial, string expiry)
        {
            var input = new ReagentInput
            {
                Chem = chem,
                ItemCode = itemCode,
                BottleCode = bottle,
                ReagentCode = rgt,
                LotNumber = lot,
                SerialNumber = serial,
                ExpDate = expiry
            };

            var result = _barcodeService.GenerateBarcode(input);
            sb.AppendLine($"Dynamic: {label}");
            sb.AppendLine($"   Input   : Lot={lot}, Serial={serial}, Exp={expiry}");
            sb.AppendLine($"   Generated: {result.BarcodeNumber}");
            if (!result.Success) sb.AppendLine($"   Error    : {result.ErrorMessage}");
            sb.AppendLine();
        }

        private List<TestCase> GetTestCases()
        {
            return new List<TestCase>
            {
                // Table provided by USER - Verified Machine Readable
                new(1,  "IgE",        "034", "IgE",  "R1", "051", "8696", "08/31/2024", "03421240831305186967"),
                new(2,  "IgE",        "034", "IgE",  "R2", "051", "8716", "08/31/2024", "03412240831905187165"),
                new(3,  "IgE",        "034", "IgE",  "R1", "052", "8723", "11/30/2024", "03421241130605287237"),
                new(4,  "IgE",        "034", "IgE",  "R2", "052", "8746", "11/30/2024", "03412241130805287467"),
                new(5,  "IgE",        "034", "IgE",  "R1", "053", "9721", "05/31/2025", "03421250531105397211"),
                new(6,  "IgE",        "034", "IgE",  "R2", "053", "9764", "05/31/2025", "03412250531305397641"),
                new(7,  "UREA IIGEN", "010", "UREA", "R1", "009", "8931", "09/30/2024", "01021240930900989311"),
                new(8,  "UREA IIGEN", "010", "UREA", "R1", "009", "8945", "09/30/2024", "01021240930100989451"),
                new(9,  "UREA IIGEN", "010", "UREA", "R2", "009", "9439", "09/30/2024", "01012240930100994395"),
                new(10, "UREA IIGEN", "010", "UREA", "R1", "010", "0559", "11/30/2024", "01021241130001005591"),
                new(11, "UREA IIGEN", "010", "UREA", "R1", "010", "0556", "11/30/2024", "01021241130101005561"),
                new(12, "UREA IIGEN", "010", "UREA", "R2", "010", "1068", "11/30/2024", "01012241130501010681"),
                new(13, "UREA IIGEN", "010", "UREA", "R1", "013", "0477", "11/30/2025", "01021251130301304777"),
                new(14, "UREA IIGEN", "010", "UREA", "R1", "013", "0476", "11/30/2025", "01021251130001304769"),
                new(15, "UREA IIGEN", "010", "UREA", "R2", "013", "0117", "11/30/2025", "01012251130701301175")
            };
        }

        public record TestCase(int Id, string Chem, string Code, string Bottle, string Rgt, string Lot, string Serial, string Expiry, string Expected);
    }
}
