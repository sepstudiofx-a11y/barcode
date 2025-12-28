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
        public IActionResult RunValidation()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Validation Results:");
            sb.AppendLine("--------------------------------------------------");

            var tests = GetTestCases();
            int passed = 0;

            foreach (var t in tests)
            {
                // Simulate the controller's normalization logic
                string itemCode = t.Code; // Default
                var chemMatch = ChemicalData.FindByAnyName(t.Chem);
                if (chemMatch != null)
                {
                    itemCode = chemMatch.DefaultCode;
                }

                // Map Bottle/Reagent text to codes
                string bottleCode = "1";
                if (t.Bottle.Contains("40")) bottleCode = "2";
                if (t.Bottle.Contains("60")) bottleCode = "3";
                
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

                var result = _barcodeService.GenerateBarcode(input);

                bool isMatch = result.BarcodeNumber == t.Expected;
                string status = isMatch ? "PASS" : "FAIL";
                if (isMatch) passed++;

                sb.AppendLine($"Test #{t.Id} [{t.Chem} SN:{t.Serial} Lot:{t.Lot}] -> {status}");
                if (!isMatch)
                {
                    sb.AppendLine($"   Expected: {t.Expected}");
                    sb.AppendLine($"   Actual  : {result.BarcodeNumber}");
                }
            }

            sb.AppendLine("--------------------------------------------------");
            sb.AppendLine($"Summary: {passed}/{tests.Count} Passed.");

            return Ok(sb.ToString());
        }

        private List<TestCase> GetTestCases()
        {
            return new List<TestCase>
            {
                // BIL TOTAL
                new(1, "BIL TOTAL", "BILT", "40 ml", "R1", "034", "9420", "10/31/2025", "00621251031303494201"),
                new(2, "BIL TOTAL", "BILT", "40 ml", "R1", "034", "9475", "10/31/2025", "00621251031803494751"),
                new(3, "BIL TOTAL", "BILT", "40 ml", "R1", "034", "9473", "10/31/2025", "00621251031203494731"),
                new(4, "BIL TOTAL", "BILT", "40 ml", "R1", "034", "9385", "10/31/2025", "00621251031503493851"),
                new(5, "BIL TOTAL", "BILT", "40 ml", "R1", "034", "9371", "10/31/2025", "00621251031303493711"),
                new(6, "BIL TOTAL", "BILT", "40 ml", "R1", "034", "9366", "10/31/2025", "00621251031803493661"),
                new(7, "BIL TOTAL", "BILT", "40 ml", "R1", "034", "9447", "10/31/2025", "00621251031403494471"),
                new(8, "BIL TOTAL", "BILT", "40 ml", "R1", "034", "9433", "10/31/2025", "00621251031203494331"),
                new(9, "BIL TOTAL", "BILT", "40 ml", "R1", "034", "9452", "10/31/2025", "00621251031903494521"),
                new(10, "BIL TOTAL", "BILT", "20 ml", "R2", "033", "9821", "10/31/2025", "00612251031803398211"),
                new(11, "BIL TOTAL", "BILT", "20 ml", "R2", "033", "9791", "10/31/2025", "00612251031503397911"),
                new(12, "BIL TOTAL", "BILT", "20 ml", "R2", "033", "9741", "10/31/2025", "00612251031503397411"),
                new(13, "BIL TOTAL", "BILT", "20 ml", "R2", "033", "9776", "10/31/2025", "00612251031003397761"),
                new(14, "BIL TOTAL", "BILT", "20 ml", "R2", "033", "9922", "10/31/2025", "00612251031403399221"),
                new(15, "BIL TOTAL", "BILT", "20 ml", "R2", "033", "9890", "10/31/2025", "00612251031503398901"),

                // GGT
                new(16, "GGT", "GGT", "40 ml", "R1", "007", "0347", "12/31/2024", "02221241231100703475"),
                new(17, "GGT", "GGT", "40 ml", "R1", "007", "0396", "12/31/2024", "02221241231800703963"),
                new(18, "GGT", "GGT", "40 ml", "R1", "007", "0370", "12/31/2024", "02221241231100703707"),
                new(19, "GGT", "GGT", "40 ml", "R1", "007", "0437", "12/31/2024", "02221241231400704375"),
                new(20, "GGT", "GGT", "40 ml", "R1", "007", "0365", "12/31/2024", "02221241231500703655"),
                
                // GGT R2 (20ml)
                new(21, "GGT", "GGT", "20 ml", "R2", "006", "9116", "09/30/2024", "02212240930000691161"),
                new(22, "GGT", "GGT", "20 ml", "R2", "006", "8760", "09/30/2024", "02212240930000687601"),
                new(23, "GGT", "GGT", "20 ml", "R2", "006", "8814", "09/30/2024", "02212240930500688141"),
                new(24, "GGT", "GGT", "20 ml", "R2", "006", "8874", "09/30/2024", "02212240930500688741"),
                new(25, "GGT", "GGT", "20 ml", "R2", "006", "8790", "09/30/2024", "02212240930000687901"),

                // PHOSPHORUS
                new(26, "PHOSPHORUS", "PHOS", "40 ml", "R1", "011", "0009", "09/30/2025", "01321250930401100099"),
                new(27, "PHOSPHORUS", "PHOS", "40 ml", "R1", "011", "0087", "09/30/2025", "01321250930801100877"),
                new(28, "PHOSPHORUS", "PHOS", "40 ml", "R1", "011", "0036", "09/30/2025", "01321250930501100369"),
                new(29, "PHOSPHORUS", "PHOS", "40 ml", "R1", "011", "0128", "09/30/2025", "01321250930401101285"),
                new(30, "PHOSPHORUS", "PHOS", "40 ml", "R1", "011", "0028", "09/30/2025", "01321250930101100287"),
            };
        }

        public record TestCase(int Id, string Chem, string Code, string Bottle, string Rgt, string Lot, string Serial, string Expiry, string Expected);
    }
}
