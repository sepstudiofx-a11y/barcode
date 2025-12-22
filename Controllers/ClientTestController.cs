using Microsoft.AspNetCore.Mvc;
using ReagentBarcode.Models;
using ReagentBarcode.Services;
using System.Collections.Generic;
using System;
using System.Linq;

namespace ReagentBarcode.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClientTestController : ControllerBase
    {
        private readonly BarcodeService _barcodeService;

        public ClientTestController(BarcodeService barcodeService)
        {
            _barcodeService = barcodeService;
        }

        [HttpGet("test-client-cases")]
        public IActionResult TestClientCases()
        {
            var testCases = GetClientTestCases();
            var results = new List<object>();
            int passCount = 0;
            int failCount = 0;

            foreach (var testCase in testCases)
            {
                try
                {
                    // Map chemical name to ItemCode
                    string itemCode = GetItemCodeForChemical(testCase.Chem);
                    
                    // Map bottle and reagent to codes
                    string bottleCode = testCase.Bottle.Contains("40") ? "2" : (testCase.Bottle.Contains("60") ? "3" : "1");
                    string reagentCode = testCase.Rgt.Contains("2") ? "2" : "1";

                    var input = new ReagentInput
                    {
                        Chem = testCase.Chem,
                        ItemCode = itemCode,
                        BottleCode = bottleCode,
                        ReagentCode = reagentCode,
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
                        Id = testCase.Id,
                        Chem = testCase.Chem,
                        ItemCode = itemCode,
                        Bottle = testCase.Bottle,
                        Rgt = testCase.Rgt,
                        Lot = testCase.Lot,
                        Serial = testCase.Serial,
                        Expiry = testCase.Expiry,
                        Expected = testCase.Expected,
                        Actual = result.BarcodeNumber ?? "ERROR",
                        Passed = passed,
                        Status = passed ? "PASS" : "FAIL",
                        Error = result.ErrorMessage
                    });
                }
                catch (Exception ex)
                {
                    failCount++;
                    results.Add(new
                    {
                        Id = testCase.Id,
                        Chem = testCase.Chem,
                        Actual = "EXCEPTION",
                        Passed = false,
                        Status = "FAIL",
                        Error = ex.Message
                    });
                }
            }

            return Ok(new
            {
                Total = testCases.Count,
                Passed = passCount,
                Failed = failCount,
                PassRate = $"{(passCount * 100.0 / testCases.Count):F2}%",
                Results = results
            });
        }

        [HttpGet("export-results-csv")]
        public IActionResult ExportResultsCsv()
        {
            var testCases = GetClientTestCases();
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Id,Chem,ItemCode,Bottle,Rgt,Lot,Serial,Expiry,Expected,Actual,Status,Error");

            foreach (var testCase in testCases)
            {
                try
                {
                    string itemCode = GetItemCodeForChemical(testCase.Chem);
                    string bottleCode = testCase.Bottle.Contains("40") ? "2" : (testCase.Bottle.Contains("60") ? "3" : "1");
                    string reagentCode = testCase.Rgt.Contains("2") ? "2" : "1";

                    var input = new ReagentInput
                    {
                        Chem = testCase.Chem,
                        ItemCode = itemCode,
                        BottleCode = bottleCode,
                        ReagentCode = reagentCode,
                        BottleType = testCase.Bottle,
                        RgtType = testCase.Rgt,
                        LotNumber = testCase.Lot,
                        SerialNumber = testCase.Serial,
                        ExpDate = testCase.Expiry
                    };

                    var result = _barcodeService.GenerateBarcode(input);
                    bool passed = result.Success && result.BarcodeNumber == testCase.Expected;

                    csv.AppendLine($"{testCase.Id},\"{testCase.Chem}\",{itemCode},\"{testCase.Bottle}\",\"{testCase.Rgt}\",\"{testCase.Lot}\",\"{testCase.Serial}\",\"{testCase.Expiry}\",\"{testCase.Expected}\",\"{result.BarcodeNumber ?? "ERROR"}\",{(passed ? "PASS" : "FAIL")},\"{result.ErrorMessage ?? ""}\"");
                }
                catch (Exception ex)
                {
                    csv.AppendLine($"{testCase.Id},\"{testCase.Chem}\",,,,,,,\"EXCEPTION\",\"FAIL\",\"{ex.Message}\"");
                }
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", "BarcodeTestResults.csv");
        }

        private string GetItemCodeForChemical(string chemicalName)
        {
            var item = ChemicalData.FindByAnyName(chemicalName);
            if (item != null) return item.DefaultCode;

            // Fallback map if FindByAnyName misses anything in the samples
            var manualMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "UREA II GEN", "010" }, { "UREA", "010" },
                { "UA II GEN", "009" }, { "UA", "009" },
                { "TG", "003" }, { "TRIGLYCERIDES", "003" },
                { "RF", "031" },
                { "MG", "012" }, { "MAGNESIUM", "012" },
                { "HDL DIRECT", "025" }, { "HDL D", "025" }, { "HDL", "025" },
                { "HbA1c DIRECT", "061" }, { "HbA1c D", "061" }, { "HBA1C", "061" },
                { "GLUCOSE", "001" }, { "GLUC", "001" },
                { "CK", "019" },
                { "CREA ENZ", "071" }, { "CREA", "071" }, { "CREATININE", "071" },
                { "CRP ULTRA", "027" }, { "CRP U", "027" },
                { "CALCIUM ARSENAZO", "059" }, { "CA ARS", "059" }, { "CA", "059" },
                { "BILIRUBIN DIRECT", "007" }, { "BIL D", "007" },
                { "ASAT", "016" },
                { "AMYLASE", "017" }, { "AMYL", "017" },
                { "ALP", "018" },
                { "ALBUMIN", "004" }, { "ALB", "004" },
                { "ALAT", "015" }, { "ALT", "015" },
                { "CHOLESTEROL", "002" }, { "CHOL", "002" },
                { "GGT", "022" },
                { "GTT", "024" },
                { "LDL DIRECT", "026" }, { "LDL D", "026" }, { "LDL", "026" },
                { "PHOSPHORUS", "013" }, { "PHOS", "013" },
                { "TOTAL IgE", "074" }, { "IgE", "074" }
            };

            return manualMap.TryGetValue(chemicalName, out var code) ? code : "001";
        }

        private List<ClientTestCase> GetClientTestCases()
        {
            return new List<ClientTestCase>
            {
                // Spreadsheet Samples (Table 1)
                new(1, "UREA II GEN", "40ml", "R1", "009", "8932", "09/30/2024", "01021240930200989327"),
                new(2, "UREA II GEN", "40ml", "R1", "009", "8951", "09/30/2024", "01021240930900989513"),
                new(3, "UREA II GEN", "20ml", "R2", "009", "9442", "09/30/2024", "01012240930000994429"),
                new(4, "UA II GEN", "40ml", "R1", "014", "0106", "01/31/2026", "00921260131701401067"),
                new(5, "UA II GEN", "40ml", "R1", "014", "0077", "01/31/2026", "00921260131701400771"),
                new(6, "UA II GEN", "20ml", "R2", "015", "0006", "12/31/2025", "00912251231001500061"),
                new(7, "TG", "40ml", "R1", "096", "0768", "02/28/2026", "00321260228309607687"),
                new(8, "TG", "40ml", "R1", "096", "0773", "02/28/2026", "00321260228809607731"),
                new(9, "TG", "40ml", "R1", "096", "0772", "02/28/2026", "00321260228509607727"),
                new(10, "TG", "40ml", "R1", "096", "0771", "02/28/2026", "00321260228209607713"),
                new(11, "TG", "20ml", "R2", "096", "0393", "02/28/2026", "00312260228909603933"),
                new(12, "TG", "20ml", "R2", "096", "0365", "02/28/2026", "00312260228509603659"),
                new(13, "RF", "40ml", "R1", "046", "8719", "12/31/2024", "03121241231304687193"),
                new(14, "RF", "20ml", "R2", "046", "8815", "12/31/2024", "03112241231704688157"),
                new(15, "MG", "40ml", "R1", "051", "0164", "03/31/2026", "01221260331705101641"),
                new(16, "HDL DIRECT", "40ml", "R1", "043", "6616", "01/31/2025", "02521250131404366167"),
                new(17, "HDL DIRECT", "40ml", "R1", "043", "6635", "01/31/2025", "02521250131104366355"),
                new(18, "HDL DIRECT", "20ml", "R2", "041", "6798", "01/31/2025", "02512250131004167989"),
                new(19, "HDL DIRECT", "20ml", "R2", "041", "6775", "01/31/2025", "02512250131104167759"),
                new(20, "HbA1c DIRECT", "40ml", "R1", "038", "8767", "03/31/2025", "06121250331903887671"),
                new(21, "HbA1c DIRECT", "20ml", "R2", "038", "8931", "03/31/2025", "06112250331003889313"),
                new(22, "GLUCOSE", "40ml", "R1", "039", "0639", "02/28/2026", "00121260228603906395"),
                new(23, "GLUCOSE", "40ml", "R1", "039", "0642", "02/28/2026", "00121260228503906427"),
                new(24, "GLUCOSE", "40ml", "R1", "039", "0638", "02/28/2026", "00121260228303906387"),
                new(25, "GLUCOSE", "20ml", "R1", "039", "0640", "02/28/2026", "00111260228903906401"),
                new(26, "CK", "40ml", "R1", "033", "5955", "09/30/2025", "01921250930303359551"),
                new(27, "CK", "20ml", "R2", "030", "6175", "09/30/2025", "01912250930303061753"),
                new(28, "CREA ENZ", "40ml", "R1", "064", "6127", "09/30/2024", "07121240930106461273"),
                new(29, "CREA ENZ", "40ml", "R1", "064", "6126", "09/30/2024", "07121240930806461261"),
                new(30, "CREA ENZ", "20ml", "R2", "064", "7016", "09/30/2024", "07112240930806470169"),
                new(31, "CRP ULTRA", "40ml", "R1", "085", "0363", "05/31/2025", "02721250531008503637"),
                new(32, "CRP ULTRA", "40ml", "R2", "085", "0830", "05/31/2025", "02722250531908508301"),
                new(33, "CALCIUM ARSENAZO", "40ml", "R1", "008", "0146", "06/30/2026", "05921260630100801469"),
                new(34, "CALCIUM ARSENAZO", "40ml", "R1", "008", "0147", "06/30/2026", "05921260630400801471"),
                new(35, "BILIRUBIN DIRECT", "40ml", "R1", "004", "0276", "05/31/2025", "00721250531300402761"),
                new(36, "BILIRUBIN DIRECT", "40ml", "R1", "004", "0279", "05/31/2025", "00721250531200402791"),
                new(37, "BILIRUBIN DIRECT", "20ml", "R2", "004", "0907", "05/31/2025", "00712250531000409071"),
                new(38, "ASAT", "40ml", "R1", "026", "0493", "09/30/2025", "01621250930202604939"),
                new(39, "ASAT", "40ml", "R1", "026", "0520", "09/30/2025", "01621250930602605201"),
                new(40, "ASAT", "20ml", "R2", "030", "0166", "12/31/2025", "01612251231903001669"),
                new(41, "AMYLASE", "40ml", "R1", "021", "8643", "02/28/2025", "01721250228902186433"),
                new(42, "AMYLASE", "40ml", "R1", "021", "8640", "02/28/2025", "01721250228002186405"),
                new(43, "ALP", "40ml", "R1", "037", "8889", "04/30/2025", "01821250430603788895"),
                new(44, "ALP", "20ml", "R2", "038", "9246", "04/30/2025", "01812250430503892467"),
                new(45, "ALBUMIN", "40ml", "R1", "039", "9897", "01/31/2026", "00421260131103998973"),
                new(46, "ALAT", "40ml", "R1", "028", "0824", "09/30/2025", "01521250930002808247"),
                new(47, "ALAT", "40ml", "R1", "028", "0823", "09/30/2025", "01521250930702808233"),
                new(48, "ALAT", "20ml", "R2", "030", "0219", "12/31/2025", "01512251231803002197"),

                // New Samples List (Table 2 onwards)
                new(101, "ALAT", "40ml", "R1", "024", "0563", "10/31/2024", "01521241031902405633"),
                new(102, "ALAT", "40ml", "R1", "024", "0562", "10/31/2024", "01521241031602405621"),
                new(103, "ALAT", "20ml", "R2", "024", "1879", "07/31/2024", "01512240731002418791"),
                new(104, "ALAT", "40ml", "R1", "028", "0936", "09/30/2025", "01521250930902809363"),
                new(105, "ALAT", "40ml", "R1", "028", "0935", "09/30/2025", "01521250930602809359"),
                new(106, "ALAT", "20ml", "R2", "030", "0192", "12/31/2025", "01512251231403001927"),
                new(107, "AMYLASE", "40ml", "R1", "021", "6300", "02/28/2025", "01721250228102163001"),
                new(108, "AMYLASE", "40ml", "R1", "021", "6301", "02/28/2025", "01721250228402163017"),
                new(109, "AMYLASE", "40ml", "R1", "021", "8625", "02/28/2025", "01721250228502186253"),
                new(110, "ASAT", "40ml", "R1", "026", "0509", "09/30/2025", "01621250930302605095"),
                new(111, "ASAT", "40ml", "R1", "026", "0461", "09/30/2025", "01621250930602604619"),
                new(112, "ASAT", "20ml", "R2", "030", "0181", "12/31/2025", "01612251231403001817"),
                new(113, "CA ARS", "40ml", "R1", "008", "0158", "06/30/2026", "0592126063000801585"),
                new(114, "CA ARS", "40ml", "R1", "008", "0159", "06/30/2026", "0592126063000801597"),
                new(115, "CHOL", "40ml", "R1", "008", "9163", "01/31/2025", "00221250131600891635"),
                new(116, "CHOL", "40ml", "R1", "008", "9164", "01/31/2025", "00221250131900891643"),
                new(117, "CHOL", "40ml", "R1", "008", "9162", "01/31/2025", "00221250131300891627"),
                new(118, "CREA ENZ", "40ml", "R1", "064", "5850", "09/30/2024", "07121240930106458507"),
                new(119, "CREA ENZ", "40ml", "R1", "064", "5852", "09/30/2024", "07121240930706458521"),
                new(120, "CREA ENZ", "20ml", "R2", "064", "6811", "09/30/2024", "07112240930706468113"),
                new(121, "CREA ENZ", "40ml", "R1", "070", "0017", "10/31/2025", "07121251031507000177"),
                new(122, "CREA ENZ", "20ml", "R2", "070", "0166", "10/31/2025", "07112251031807001667"),
                new(123, "GGT", "40ml", "R1", "006", "8347", "09/30/2024", "02221240930600683471"),
                new(124, "GGT", "20ml", "R2", "006", "8847", "09/30/2024", "02212240930400688471"),
                new(125, "GGT", "40ml", "R1", "007", "0473", "12/31/2024", "02221241231200704735"),
                                new(126, "GLUC", "40ml", "R1", "039", "0642", "02/28/2026", "00121260228503906427"),
                new(127, "GLUC", "40ml", "R1", "039", "0638", "02/28/2026", "00121260228303906387"),
                new(131, "GLUC", "40ml", "R1", "033", "8798", "03/31/2025", "00121250331003387985"),
                new(132, "GTT", "40ml", "R1", "010", "1010", "03/31/2026", "01821260331001010107"),
                new(133, "HbA1c D", "40ml", "R1", "038", "8704", "03/31/2025", "06121250331003887045"),
                new(134, "HbA1c D", "20ml", "R2", "038", "8855", "03/31/2025", "06112250331903888553"),
                new(135, "HbA1c D", "40ml", "R1", "039", "8702", "03/31/2025", "06121250331203987021"),
                new(136, "HbA1c D", "40ml", "R1", "039", "8733", "03/31/2025", "06121250331503987339"),
                new(137, "HDL D", "40ml", "R1", "043", "6747", "01/31/2025", "02521250131004367477"),
                new(138, "HDL D", "40ml", "R1", "043", "6748", "01/31/2025", "02521250131304367485"),
                new(139, "HDL D", "20ml", "R2", "041", "6947", "01/31/2025", "02512250131404169477"),
                new(140, "HDL D", "20ml", "R2", "041", "6876", "01/31/2025", "02512250131704168765"),
                new(141, "LDL D", "40ml", "R1", "038", "8471", "11/30/2024", "02621241130503884717"),
                new(142, "LDL D", "20ml", "R2", "031", "8611", "11/30/2024", "02612241130303186111"),
                new(143, "LDL D", "40ml", "R1", "039", "8702", "03/31/2025", "02621250331203987021"),
                new(144, "LDL D", "40ml", "R1", "039", "8733", "03/31/2025", "02621250331503987339"),
                new(145, "LDL D", "20ml", "R2", "032", "8799", "03/31/2025", "02612250331503287999"),
                new(146, "LDL D", "20ml", "R2", "032", "9050", "03/31/2025", "02612250331703290501"),
                new(161, "MG", "40ml", "R1", "047", "9926", "11/30/2024", "01221241130004799263"),
                new(162, "MG", "40ml", "R1", "051", "0164", "03/31/2026", "01221260331705101641"),
                new(201, "PHOSPHORUS", "40ml", "R1", "008", "6077", "09/30/2024", "01321240930600860771"),
                new(202, "PHOSPHORUS", "40ml", "R1", "008", "8272", "09/30/2024", "01321240930700882721"),
                new(203, "PHOSPHORUS", "40ml", "R1", "008", "9907", "09/30/2024", "01321240930300899071"),
                new(204, "PHOSPHORUS", "40ml", "R1", "011", "0009", "09/30/2025", "01321250930401100099"),
                new(205, "RF", "40ml", "R1", "046", "8719", "12/31/2024", "03121241231304687193"),
                new(206, "RF", "20ml", "R2", "046", "8815", "12/31/2024", "03112241231704688157"),
                new(207, "RF", "40ml", "R1", "050", "0031", "12/31/2025", "03121251231305000311"),
                new(208, "RF", "20ml", "R2", "050", "0091", "12/31/2025", "03112251231605000911"),
                new(209, "TOTAL IgE", "40ml", "R1", "051", "5151", "07/31/2025", "07421250731505151511"),
                new(210, "TOTAL IgE", "40ml", "R1", "052", "5252", "07/31/2025", "07421250731805252529"),
                new(211, "TOTAL IgE", "40ml", "R1", "053", "5353", "07/31/2025", "07421250731905353537"),
                new(212, "UREA II GEN", "40ml", "R1", "010", "0559", "11/30/2024", "01021241130001005591"),
                new(213, "UREA II GEN", "20ml", "R2", "010", "1068", "11/30/2024", "01012241130501010681"),
                new(214, "UREA II GEN", "40ml", "R1", "013", "0477", "11/30/2025", "01021251130301304777"),
                new(215, "UREA II GEN", "20ml", "R2", "013", "0117", "11/30/2025", "01012251130701301175")
            };
        }

        private class ClientTestCase
        {
            public int Id { get; }
            public string Chem { get; }
            public string Bottle { get; }
            public string Rgt { get; }
            public string Lot { get; }
            public string Serial { get; }
            public string Expiry { get; }
            public string Expected { get; }

            public ClientTestCase(int id, string chem, string bottle, string rgt, string lot, string serial, string expiry, string expected)
            {
                Id = id;
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
