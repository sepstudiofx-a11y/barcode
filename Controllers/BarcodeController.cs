using Microsoft.AspNetCore.Mvc;
using ReagentBarcode.Models;
using ReagentBarcode.Services;

namespace ReagentBarcode.Controllers
{
    [ApiController]
    [Route("api/barcode")]
    public class BarcodeController : ControllerBase
    {
        private readonly BarcodeService _barcodeService;
        private readonly LicenseService _licenseService;

        public BarcodeController(BarcodeService barcodeService, LicenseService licenseService)
        {
            _barcodeService = barcodeService;
            _licenseService = licenseService;
        }

        [HttpGet("license-status")]
        public IActionResult GetLicenseStatus()
        {
            return Ok(new
            {
                IsValid = _licenseService.IsLicenseValid(),
                RemainingDays = _licenseService.GetRemainingDays(),
                RemainingToday = _licenseService.GetRemainingGeneratesToday(),
                CanGenerate = _licenseService.CanGenerateToday()
            });
        }

        [HttpPost("generate")]
        public IActionResult Generate([FromBody] ReagentInput input)
        {
            if (input == null)
                return BadRequest("Invalid input");

            if (!_licenseService.IsLicenseValid())
            {
                return BadRequest(new BarcodeResult 
                { 
                    Success = false, 
                    ErrorMessage = "License Expired. This software was valid for a 28-day trial period. Please contact support to renew your license." 
                });
            }

            if (!_licenseService.CanGenerateToday())
            {
                return BadRequest(new BarcodeResult
                {
                    Success = false,
                    ErrorMessage = "Daily Limit Reached. You have reached the maximum allowed generations for today. Please wait until tomorrow."
                });
            }

            // 🔒 Dynamic chemical normalization (UI → Engine)
            var chemMatch = ChemicalData.FindByAnyName(input.Chem);
            if (chemMatch != null) {
                input.Chem = chemMatch.Name;
                if (string.IsNullOrEmpty(input.ItemCode) || input.ItemCode == "000") {
                    input.ItemCode = chemMatch.DefaultCode;
                }
            }

            var result = _barcodeService.GenerateBarcode(input);

            if (result.Success)
            {
                _licenseService.IncrementUsage();
            }

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPost("print")]
        public IActionResult Print([FromBody] List<BarcodeResult> items)
        {
            if (items == null || !items.Any())
                return BadRequest("No items to print");

            try
            {
                var pdfBytes = _barcodeService.GeneratePdf(items);
                return File(pdfBytes, "application/pdf", $"barcodes_{DateTime.Now:yyyyMMddHHmmss}.pdf");
            }
            catch (Exception ex)
            {
                return BadRequest($"Print failed: {ex.Message}");
            }
        }

        [HttpGet("chemicals")]
        public IActionResult GetChemicals()
        {
            return Ok(ChemicalData.Chemicals.Select(c => new ChemicalDto { Display = c.Name, Value = c.Name, Code = c.DefaultCode }));
        }

        [HttpGet("definitions")]
        public IActionResult GetDefinitions()
        {
            return Ok(new DefinitionsResponse
            { 
               Chemicals = ChemicalData.Chemicals,
               Bottles = ChemicalData.Bottles,
               Reagents = ChemicalData.Reagents
            });
        }
    }

    public class ChemicalOption
    {
        public string Value { get; set; } = string.Empty;
        public string Display { get; set; } = string.Empty;
    }

    public class ChemicalDto
    {
        public string Display { get; set; }
        public string Value { get; set; }
        public string Code { get; set; }
    }

    public class DefinitionsResponse
    {
        public List<ChemicalItem> Chemicals { get; set; }
        public List<BottleOption> Bottles { get; set; }
        public List<ReagentOption> Reagents { get; set; }
    }
}
