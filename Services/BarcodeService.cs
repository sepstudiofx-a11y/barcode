using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Extensions.Logging;
using ReagentBarcode.Models;
using ZXing;
using ZXing.Common;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using ImageSharpImage = SixLabors.ImageSharp.Image;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Text.Json;
using System.IO;

namespace ReagentBarcode.Services
{
    public class BarcodeService
    {
        private readonly ILogger<BarcodeService> _logger;
        private static List<BarcodeSample>? _cachedSamples;

        public BarcodeService(ILogger<BarcodeService> logger) { _logger = logger; }

        private List<BarcodeSample> GetSamples()
        {
            if (_cachedSamples != null) return _cachedSamples;
            try {
                string[] paths = {
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "data", "barcode_anchors.json"),
                    Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "barcode_anchors.json"),
                    "wwwroot/data/barcode_anchors.json"
                };
                foreach (var path in paths) {
                    if (File.Exists(path)) {
                        string json = File.ReadAllText(path);
                        var raw = JsonSerializer.Deserialize<List<RawSample>>(json);
                        if (raw != null) {
                            _cachedSamples = raw.Select(r => new BarcodeSample { 
                                ItemCode = r.ic, RgtType = r.rt, Serial = r.s, Full = r.f 
                            }).ToList();
                            return _cachedSamples;
                        }
                    }
                }
            } catch { }
            return BarcodeSample.AllSamples;
        }

        public BarcodeResult GenerateBarcode(ReagentInput i)
        {
            try {
                if (i == null) return Fail("Input required");
                DateTime exp = ParseExpiryDate(i.ExpDate);
                
                string ic = (i.ItemCode ?? "000").PadLeft(3,'0').Substring(0,3);
                string bc = (i.BottleCode ?? "1").Substring(0,1);
                string rc = (i.ReagentCode ?? "1").Substring(0,1);
                string dt = exp.ToString("yyMMdd");
                
                string s4 = new string((i.SerialNumber ?? "0").Where(char.IsDigit).ToArray()).PadLeft(4, '0');
                if (s4.Length > 4) s4 = s4[^4..];

                var samples = GetSamples();
                var sample = FindClosestSample(samples, ic, bc, rc, s4, i.LotNumber);
                
                string pLotPart = "";
                int calibration = 0;

                if (sample != null)
                {
                    int totalLen = sample.Full.Length;
                    int midLen = totalLen - 11 - 4 - 1;
                    string samplePLot = sample.Full.Substring(11, midLen);
                    
                    int sVal = int.Parse(s4);
                    int sampSVal = int.Parse(new string(sample.Serial.Where(char.IsDigit).ToArray()));
                    int deltaS = sVal - sampSVal;

                    if (deltaS == 0) {
                        pLotPart = samplePLot;
                    } else {
                        int lotLen = (midLen >= 3) ? 3 : midLen;
                        int pLen = midLen - lotLen;
                        string lotDigits = new string((i.LotNumber ?? "0").Where(char.IsDigit).ToArray());
                        string lotStr = lotDigits.PadLeft(lotLen, '0');
                        if (lotStr.Length > lotLen) lotStr = lotStr[^lotLen..];

                        string pStr = "";
                        if (pLen > 0) {
                            string basePStr = samplePLot.Substring(0, pLen);
                            if (int.TryParse(basePStr, out int baseP)) {
                                int p;
                                if (pLen > 1 && Math.Abs(deltaS) < 20) p = baseP;
                                else {
                                    int lastDigit = baseP % 10;
                                    int dynLast = (lastDigit + (deltaS % 10) * 3 + 100) % 10;
                                    p = (baseP / 10) * 10 + dynLast;
                                }
                                pStr = p.ToString().PadLeft(pLen, '0');
                            }
                        }
                        pLotPart = pStr + lotStr;
                    }

                    string samplePayload = sample.Full.Substring(0, totalLen - 1);
                    int sampleWSum = CalculateWeightedSum(samplePayload);
                    int sampleCS = sample.Full.Last() - '0';
                    calibration = (sampleCS + sampleWSum) % 10;
                } else {
                    pLotPart = ((int.Parse(s4[^1..]) * 3 + 5) % 10).ToString() + new string((i.LotNumber ?? "0").Where(char.IsDigit).ToArray()).PadLeft(3, '0');
                    calibration = 10;
                }

                string cFixed = ic + bc + rc + dt;
                string cFinal = cFixed + pLotPart + s4;
                int currentWSum = CalculateWeightedSum(cFinal);
                int cs = (calibration - (currentWSum % 10) + 20) % 10;
                
                string fullBarcode = cFinal + cs;

                return new BarcodeResult {
                    Success = true, 
                    BarcodeNumber = fullBarcode, 
                    BarcodeImageBase64 = GenerateBarcodeImage(fullBarcode),
                    Chem = i.Chem, GenItemCode = ic, GenBottleCode = bc, GenReagentCode = rc,
                    LotNumber = i.LotNumber, SerialNumber = s4, ExpDate = exp, GeneratedAt = DateTime.Now
                };
            } catch (Exception ex) { return Fail(ex.Message); }
        }

        private int CalculateWeightedSum(string s)
        {
            int sum = 0;
            int len = s.Length;
            for (int j = 0; j < len; j++) {
                int digit = s[len - 1 - j] - '0';
                int weight = (j % 2 == 0) ? 3 : 1;
                sum += digit * weight;
            }
            return sum;
        }

        private BarcodeSample? FindClosestSample(List<BarcodeSample> samples, string ic, string bc, string rc, string s4, string? lot)
        {
            var candidates = samples.Where(s => s.ItemCode == ic).ToList();
            if (!candidates.Any()) return null;

            // Priority 0: Exact Serial Match within Chemistry
            var exactS = candidates.FirstOrDefault(c => c.Serial == s4);
            if (exactS != null) return exactS;

            // Priority 1: Match BCRC exactly
            var bcrcFiltered = candidates.Where(c => c.Full.Substring(3,1) == bc && c.Full.Substring(4,1) == rc).ToList();
            if (bcrcFiltered.Any()) candidates = bcrcFiltered;

            // Priority 2: Match Lot
            string lotClean = new string((lot ?? "").Where(char.IsDigit).ToArray());
            if (!string.IsNullOrEmpty(lotClean)) {
                var lotFiltered = candidates.Where(c => c.Full.Contains(lotClean)).ToList();
                if (lotFiltered.Any()) candidates = lotFiltered;
            }

            int sVal = int.Parse(s4);
            return candidates.OrderBy(c => {
                if (int.TryParse(c.Serial, out int csv)) return Math.Abs(sVal - csv);
                return 9999;
            }).FirstOrDefault();
        }

        public string GenerateBarcodeImage(string b) {
            try {
                var w = new BarcodeWriterPixelData { Format = BarcodeFormat.CODE_128, Options = new EncodingOptions { Width = 1200, Height = 360, Margin = 5, PureBarcode = true } };
                var d = w.Write(b); using var img = ImageSharpImage.LoadPixelData<Rgba32>(d.Pixels, d.Width, d.Height);
                using var ms = new System.IO.MemoryStream(); img.Save(ms, new SixLabors.ImageSharp.Formats.Png.PngEncoder());
                return Convert.ToBase64String(ms.ToArray());
            } catch { return ""; }
        }

        public byte[] GeneratePdf(List<BarcodeResult> i) {
            QuestPDF.Settings.License = LicenseType.Community;
            return Document.Create(c => c.Page(p => { 
                p.Size(PageSizes.A4); p.Margin(1, Unit.Centimetre);
                p.Content().PaddingVertical(10).Table(table => {
                    table.ColumnsDefinition(columns => { columns.RelativeColumn(); columns.RelativeColumn(); columns.RelativeColumn(); });
                    foreach (var x in i) { table.Cell().Padding(5).Element(e => ComposeLabel(e, x)); }
                });
            })).GeneratePdf();
        }

        private void ComposeLabel(IContainer c, BarcodeResult i) {
            c.Border(0.5f).Padding(8).Column(col => {
                col.Spacing(2);
                col.Item().Row(r => { 
                    r.RelativeItem().Text(i.Chem).Bold().FontSize(8); 
                    r.RelativeItem().AlignRight().Text($"Lot:{i.LotNumber}").FontSize(7); 
                });
                col.Item().AlignCenter().MaxHeight(1.0f, Unit.Centimetre).Image(GenerateBarcodeImageVerbatim(i.BarcodeNumber!));
                col.Item().AlignCenter().Text(i.BarcodeNumber).FontSize(9).LetterSpacing(0.1f);
                col.Item().Row(r => {
                    r.RelativeItem().Text($"SN:{i.SerialNumber}").FontSize(6);
                    r.RelativeItem().AlignRight().Text($"Exp:{i.ExpDate:MMM yyyy}").FontSize(6).Italic();
                });
            });
        }

        private byte[] GenerateBarcodeImageVerbatim(string b) {
            try {
                var w = new BarcodeWriterPixelData { Format = BarcodeFormat.CODE_128, Options = new EncodingOptions { Width = 1000, Height = 250, Margin = 2, PureBarcode = true } };
                var d = w.Write(b); using var img = ImageSharpImage.LoadPixelData<Rgba32>(d.Pixels, d.Width, d.Height);
                using var ms = new System.IO.MemoryStream(); img.Save(ms, new SixLabors.ImageSharp.Formats.Png.PngEncoder());
                return ms.ToArray();
            } catch { return Array.Empty<byte>(); }
        }

        private DateTime ParseExpiryDate(string ds) {
            if (DateTime.TryParseExact(ds, new[] { "dd-MM-yyyy", "d-M-yyyy", "dd/MM/yyyy", "d/M/yyyy", "MM/dd/yyyy", "M/d/yyyy", "MM-dd-yyyy", "yyyy-MM-dd", "M/dd/yyyy", "MM/d/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime d)) return d;
            return DateTime.Now;
        }

        private BarcodeResult Fail(string m) => new BarcodeResult { Success = false, ErrorMessage = m };
        private class RawSample { public string ic { get; set; } = ""; public string rt { get; set; } = "R1"; public string s { get; set; } = ""; public string f { get; set; } = ""; }
    }
}
