using System.Collections.Generic;

namespace ReagentBarcode.Models
{
    public class TestResultDto
    {
        public int TestNumber { get; set; }
        public string Chem { get; set; }
        public string Bottle { get; set; }
        public string Rgt { get; set; }
        public string Lot { get; set; }
        public string Serial { get; set; }
        public string Expiry { get; set; }
        public string Expected { get; set; }
        public string Actual { get; set; }
        public bool Passed { get; set; }
        public string Error { get; set; }
    }

    public class TestSummaryDto
    {
        public int Total { get; set; }
        public int Passed { get; set; }
        public int Failed { get; set; }
        public List<TestResultDto> Results { get; set; }
    }
}
