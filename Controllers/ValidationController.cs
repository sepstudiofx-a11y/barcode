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
                string itemCode = t.Code;
                var chemMatch = ChemicalData.FindByAnyName(t.Chem);
                if (chemMatch != null)
                {
                    itemCode = chemMatch.DefaultCode;
                }

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

            sb.AppendLine();
            sb.AppendLine("Dynamic Variation Tests (User Request):");
            sb.AppendLine("--------------------------------------------------");
            
            // Dynamic Case 1: IgE with modified Lot (999)
            RunDynamicTest(sb, "IgE (Dynamic Lot 999)", "034", "IgE", "2", "1", "999", "1234", "12/31/2025");

            // Dynamic Case 2: UREA with modified Expiry (2030)
            RunDynamicTest(sb, "UREA (Dynamic Exp 2030)", "010", "UREA II GEN", "2", "1", "009", "8931", "01/01/2030");

            // Dynamic Case 3: IgE with modified Serial (0001)
            RunDynamicTest(sb, "IgE (Dynamic Serial 0001)", "034", "IgE", "2", "1", "051", "0001", "08/31/2024");

            // User Verification Case 1 (Image)
            RunDynamicTest(sb, "Verify Image 1 (IgE, Lot 030)", "034", "IgE", "2", "1", "030", "0016", "12/31/2025");

            // User Verification Case 2 (Image)
            RunDynamicTest(sb, "Verify Image 2 (IgE, Lot 034)", "034", "IgE", "2", "1", "034", "0016", "12/31/2025");

            // User Verification Case 3 (Image UREA 0559)
            RunDynamicTest(sb, "Verify Image 3 (UREA, Lot 010, SN 0559)", "010", "UREA IIGEN", "2", "1", "010", "0559", "11/30/2024");

            // User Verification Case 4 (Image UREA 0556)
            RunDynamicTest(sb, "Verify Image 4 (UREA, Lot 010, SN 0556)", "010", "UREA IIGEN", "2", "1", "010", "0556", "11/30/2024");

            // User Verification Case 5 (Image UREA 1068 R2)
            RunDynamicTest(sb, "Verify Image 5 (UREA, Lot 010, SN 1068, R2)", "010", "UREA IIGEN", "1", "2", "010", "1068", "11/30/2024");

            sb.AppendLine("--------------------------------------------------");
            sb.AppendLine($"Summary: {passed}/{tests.Count} Fixed Tests Passed.");

            return Ok(sb.ToString());
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
