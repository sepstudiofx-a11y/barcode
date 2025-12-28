
# Script to merge ValidationController.cs template with generated test cases
template_top = """using Microsoft.AspNetCore.Mvc;
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

"""

template_bottom = """
        public record TestCase(int Id, string Chem, string Code, string Bottle, string Rgt, string Lot, string Serial, string Expiry, string Expected);
    }
}
"""

with open('new_test_cases.txt', 'r') as f:
    test_cases_code = f.read()

with open('Controllers/ValidationController.cs', 'w') as f:
    f.write(template_top)
    f.write(test_cases_code)
    f.write(template_bottom)

print("SUCCESS")
