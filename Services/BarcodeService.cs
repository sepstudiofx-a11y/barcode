using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Extensions.Logging;
using ReagentBarcode.Models;
using ZXing;
using ZXing.Common;
using ZXing.OneD;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
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
        private static Dictionary<string, Slopes> _calibrationSlopesExtended = new();

        public struct Slopes {
            public int[] pSlopes; // 4 digits of SN
            public int[] calSlopes; // SN tens, and maybe others
            public int k; // P-delta slope for Cal
            public int lotSlope; // Lot-delta slope for Cal
            public int count;
        }

        public BarcodeService(ILogger<BarcodeService> logger) { _logger = logger; }

        private List<BarcodeSample> GetSamples()
        {
            if (_cachedSamples != null) return _cachedSamples;
            try {
                string[] paths = {
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "data", "barcode_anchors.json"),
                    Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "barcode_anchors.json"),
                    "wwwroot/data/barcode_anchors.json",
                    "../wwwroot/data/barcode_anchors.json"
                };
                foreach (var path in paths) {
                    if (File.Exists(path)) {
                        string json = File.ReadAllText(path);
                        using var doc = JsonDocument.Parse(json);
                        _cachedSamples = new List<BarcodeSample>();
                        foreach (var el in doc.RootElement.EnumerateArray()) {
                            _cachedSamples.Add(new BarcodeSample {
                                ItemCode = el.GetProperty("ic").GetString() ?? "",
                                RgtType = el.GetProperty("rt").GetString() ?? "R1",
                                Serial = el.GetProperty("s").GetString() ?? "",
                                Full = el.GetProperty("f").GetString() ?? ""
                            });
                        }
                        if (_cachedSamples != null) {
                            foreach(var s in BarcodeSample.AllSamples) {
                                if (!_cachedSamples.Any(x => x.Full == s.Full)) _cachedSamples.Add(s);
                            }
                            AnalyzeCalibrationSlopes(_cachedSamples);
                            return _cachedSamples;
                        }
                    }
                }
            } catch { }

            if (_cachedSamples == null) 
            {
                _cachedSamples = BarcodeSample.AllSamples;
            }
            
            AnalyzeCalibrationSlopes(_cachedSamples);
            return _cachedSamples;
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
                string fullBarcode = "";
                string currentPrefix = ic + bc + rc;

                if (sample == null)
                {
                    pLotPart = ((int.Parse(s4[^1..]) * 3 + 5) % 10).ToString() + new string((i.LotNumber ?? "0").Where(char.IsDigit).ToArray()).PadLeft(3, '0');
                    string cFinal = currentPrefix + dt + pLotPart + s4;
                    int weight = CalculateWeightedSum(cFinal);
                    int cs = (10 - weight % 10) % 10;
                    fullBarcode = (cFinal + cs).PadLeft(20, '0')[^20..];
                }
                else
                {
                    if (sample.Full.Length >= 5) {
                        currentPrefix = sample.Full.Substring(0, 5);
                    }

                    int lotLen = 3;
                    string lotTarget = new string((i.LotNumber ?? "0").Where(char.IsDigit).ToArray());
                    string lotStr = (lotTarget.Length >= lotLen) ? lotTarget[^lotLen..] : lotTarget.PadLeft(lotLen, '0');

                    string groupKey = sample.ItemCode + "_" + sample.RgtType;
                    int[] pS = { 3, 0, 3, 0 };
                    int k = 1; int m = 0; 
                    int lS = 0;
                    
                    if (_calibrationSlopesExtended.TryGetValue(groupKey, out var slopes)) {
                        pS = slopes.pSlopes;
                        k = slopes.k; 
                        m = slopes.calSlopes[0];
                        lS = slopes.lotSlope;
                    }
                    
                    int pSampleNum = (sample.Full.Length >= 12) ? sample.Full[11] - '0' : 0;
                    int sSampleNum = int.Parse(new string(sample.Serial.Where(char.IsDigit).ToArray()));
                    int sInputNum = int.Parse(s4);
                    int lotSampleNum = int.Parse(sample.Full.Substring(12, 3));
                    int lotInputNum = int.Parse(lotStr);

                    int pNewNum = pSampleNum;
                    int tempSN = sInputNum;
                    int tempSNSample = sSampleNum;
                    for (int j = 0; j < 4; j++) {
                        int d = (tempSN % 10) - (tempSNSample % 10);
                        pNewNum = (pNewNum + pS[j] * d) % 10;
                        tempSN /= 10; tempSNSample /= 10;
                    }
                    while (pNewNum < 0) pNewNum += 10;
                    string pStr = pNewNum.ToString();

                    int deltaP = pNewNum - pSampleNum; 
                    int deltaSTensActual = (sInputNum / 10) - (sSampleNum / 10);
                    int deltaLot = lotInputNum - lotSampleNum;
                    
                    int anchorCS = sample.Full.Last() - '0';
                    int wAnchor = CalculateWeightedSum(sample.Full[..^1]);
                    int anchorCal = (anchorCS + wAnchor) % 10;
                    
                    int targetCal = (anchorCal + k * deltaP + m * deltaSTensActual + lS * deltaLot) % 10;
                    while (targetCal < 0) targetCal += 10;
                    
                    pLotPart = pStr + lotStr;
                    string cFinal = currentPrefix + dt + pLotPart + s4;
                    int currentWSum = CalculateWeightedSum(cFinal);
                    int cs = (targetCal - currentWSum) % 10;
                    if (cs < 0) cs += 10;
                    
                    fullBarcode = (cFinal + cs).PadLeft(20, '0')[^20..];
                }

                return new BarcodeResult {
                    Success = true, 
                    BarcodeNumber = fullBarcode, 
                    BarcodeImageBase64 = GenerateBarcodeImage(fullBarcode),
                    Chem = i.Chem, GenItemCode = currentPrefix.Substring(0,3), GenBottleCode = currentPrefix.Length>=4 ? currentPrefix.Substring(3,1) : "1", GenReagentCode = currentPrefix.Length>=5 ? currentPrefix.Substring(4,1) : "1",
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
            var chemistrySamples = samples.Where(s => s.ItemCode == ic).ToList();
            if (!chemistrySamples.Any()) return null;

            string lotTarget = new string((lot ?? "").Where(char.IsDigit).ToArray()).PadLeft(3, '0');
            if (lotTarget.Length > 3) lotTarget = lotTarget[^3..];

            var lotSamples = chemistrySamples.Where(s => 
                s.Full.Length >= 15 && 
                s.Full.Substring(3, 1) == bc && 
                s.Full.Substring(4, 1) == rc &&
                s.Full.Substring(12, 3) == lotTarget).ToList();

            if (lotSamples.Any()) {
                int targetSerial = int.Parse(s4);
                return lotSamples.OrderBy(s => {
                    if (int.TryParse(s.Serial, out int sv)) return Math.Abs(targetSerial - sv);
                    return 9999;
                }).First();
            }

            var typeSamples = chemistrySamples.Where(s => 
                s.Full.Length >= 5 && 
                s.Full.Substring(3, 1) == bc && 
                s.Full.Substring(4, 1) == rc).ToList();

            if (typeSamples.Any()) {
                int targetSerial = int.Parse(s4);
                return typeSamples.OrderBy(s => {
                    if (int.TryParse(s.Serial, out int sv)) return Math.Abs(targetSerial - sv);
                    return 9999;
                }).First();
            }

            int sVal = int.Parse(s4);
            return chemistrySamples.OrderBy(c => {
                if (int.TryParse(c.Serial, out int csv)) return Math.Abs(sVal - csv);
                return 9999;
            }).FirstOrDefault();
        }

        public string GenerateBarcodeImage(string b) {
            var bytes = GenerateBarcodeImageBytes(b);
            return bytes != null ? Convert.ToBase64String(bytes) : "";
        }

        public byte[] GeneratePdf(List<BarcodeResult> i) {
            try {
                QuestPDF.Settings.License = LicenseType.Community;
                QuestPDF.Settings.EnableDebugging = false;
                
                return Document.Create(c => c.Page(p => { 
                    p.Size(PageSizes.A4); 
                    p.Margin(1, Unit.Centimetre);
                    p.Content().PaddingVertical(10).Table(table => {
                        table.ColumnsDefinition(columns => { columns.RelativeColumn(); columns.RelativeColumn(); columns.RelativeColumn(); });
                        foreach (var x in i) { table.Cell().Padding(5).Element(e => ComposeLabel(e, x)); }
                    });
                })).GeneratePdf();
            }
            catch (Exception ex) {
                _logger.LogError($"PDF Generation Failed: {ex.Message}");
                throw; // Rethrow to let Controller handle it
            }
        }

        private void ComposeLabel(IContainer c, BarcodeResult i) {
            c.Border(0.5f).Padding(8).Column(col => {
                col.Spacing(2);
                col.Item().Row(r => { r.RelativeItem().Text(i.Chem).Bold().FontSize(8); r.RelativeItem().AlignRight().Text($"Lot:{i.LotNumber}").FontSize(7); });
                
                var barcodeBytes = GenerateBarcodeImageBytes(i.BarcodeNumber!);
                if (barcodeBytes != null && barcodeBytes.Length > 0)
                {
                    // Fixed height for consistency
                    col.Item().AlignCenter().Height(1.2f, Unit.Centimetre).Image(barcodeBytes);
                }
                else
                {
                    col.Item().AlignCenter().Height(1.2f, Unit.Centimetre).Text("ERR").FontColor(Colors.Red.Medium).FontSize(8);
                }
                
                col.Item().AlignCenter().Text(i.BarcodeNumber).FontSize(9).LetterSpacing(0.1f);
                col.Item().Row(r => { r.RelativeItem().Text($"SN:{i.SerialNumber}").FontSize(6); r.RelativeItem().AlignRight().Text($"Exp:{i.ExpDate:MMM yyyy}").FontSize(6).Italic(); });
            });
        }

        private byte[]? GenerateBarcodeImageBytes(string b) {
            if (string.IsNullOrEmpty(b)) return null;
            try {
                // Generate High-Res Image directly to ensure sharpness without manual scaling artifacts
                // Width 2000 ensures that even with anti-aliasing, the bars are distinct enough for scanners.
                var options = new Code128EncodingOptions { 
                    Width = 2000, 
                    Height = 500, 
                    Margin = 10, 
                    PureBarcode = true 
                };
                
                var w = new BarcodeWriterPixelData { Format = BarcodeFormat.CODE_128, Options = options };
                var d = w.Write(b);
                
                if (d == null) return null;

                using var img = ImageSharpImage.LoadPixelData<Rgba32>(d.Pixels, d.Width, d.Height);
                using var ms = new System.IO.MemoryStream(); 
                img.Save(ms, new SixLabors.ImageSharp.Formats.Png.PngEncoder());
                return ms.ToArray();
            } 
            catch (Exception ex) {
                _logger.LogError($"Barcode Image Gen Failed: {ex.Message}");
                return null;
            }
        }

        private byte[] GenerateBarcodeImageVerbatim(string b) {
            return GenerateBarcodeImageBytes(b) ?? Array.Empty<byte>();
        }

        private void AnalyzeCalibrationSlopes(List<BarcodeSample> samples)
        {
            _calibrationSlopesExtended.Clear();
            foreach (var g in samples.GroupBy(s => s.ItemCode + "_" + s.RgtType))
            {
                var valid = g.Select(s => {
                    if (s.Full.Length < 20) return null;
                    int pVal = s.Full[11] - '0', sVal = int.Parse(new string(s.Serial.Where(char.IsDigit).ToArray())), lotVal = int.Parse(s.Full.Substring(12, 3)), csVal = s.Full.Last() - '0';
                    int wSum = CalculateWeightedSum(s.Full[..^1]), calVal = (csVal + wSum) % 10;
                    return new { p = pVal, s = sVal, cal = calVal, lot = lotVal, snDigits = new[] { sVal % 10, (sVal / 10) % 10, (sVal / 100) % 10, (sVal / 1000) % 10 }, sFT = sVal / 10 };
                }).Where(x => x != null).ToList();

                if (valid.Count < 2) continue;
                var pVotes = new Dictionary<(int, int, int, int), int>();
                for (int i = 0; i < valid.Count; i++) {
                    for (int j = i + 1; j < valid.Count; j++) {
                        int dp = (valid[j]!.p - valid[i]!.p + 10) % 10;
                        for (int p0 = 0; p0 < 10; p0++) for (int p2 = 0; p2 < 10; p2++) {
                            int pred = (p0 * (valid[j]!.snDigits[0] - valid[i]!.snDigits[0]) + p2 * (valid[j]!.snDigits[2] - valid[i]!.snDigits[2])) % 10;
                            if ((pred + 10) % 10 == dp) pVotes[(p0, 0, p2, 0)] = pVotes.GetValueOrDefault((p0, 0, p2, 0)) + 1;
                        }
                    }
                }
                var bestP = pVotes.OrderByDescending(x => x.Value).ThenByDescending(x => x.Key == (3, 0, 3, 0)).FirstOrDefault().Key;
                if (!pVotes.Any()) bestP = (3, 0, 3, 0);

                var cVotes = new Dictionary<(int k, int m, int l), int>();
                for (int i = 0; i < valid.Count; i++) {
                    for (int j = i + 1; j < valid.Count; j++) {
                        int dp = (valid[j]!.p - valid[i]!.p + 10) % 10, dsT = valid[j]!.sFT - valid[i]!.sFT, dL = valid[j]!.lot - valid[i]!.lot, dc = (valid[j]!.cal - valid[i]!.cal + 10) % 10;
                        for (int ck = 0; ck < 10; ck++) for (int cm = 0; cm < 10; cm++) foreach (int cl in new[] { 0, 1, 9 }) {
                            if ((ck * dp + cm * dsT + cl * dL) % 10 == dc) cVotes[(ck, cm, cl)] = cVotes.GetValueOrDefault((ck, cm, cl)) + (dL == 0 ? 5 : 1);
                        }
                    }
                }
                if (cVotes.Any()) {
                    var b = cVotes.OrderByDescending(x => x.Value).First().Key;
                    _calibrationSlopesExtended[g.Key] = new Slopes { pSlopes = new[] { bestP.Item1, bestP.Item2, bestP.Item3, bestP.Item4 }, calSlopes = new[] { b.m }, k = b.k, lotSlope = b.l, count = valid.Count };
                }
            }
            var avgP = new[] { 3, 0, 3, 0 }; int avgK = 1, avgM = 0, avgL = 0;
            foreach (var s in BarcodeSample.AllSamples.Select(x => x.ItemCode + "_" + x.RgtType).Distinct()) {
                if (!_calibrationSlopesExtended.ContainsKey(s)) _calibrationSlopesExtended[s] = new Slopes { pSlopes = avgP, calSlopes = new[] { avgM }, k = avgK, lotSlope = avgL, count = 0 };
            }
        }

        private DateTime ParseExpiryDate(string ds)
        {
            var formats = new[] { "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "d-M-yyyy", "dd.MM.yyyy", "d.M.yyyy", "MM/dd/yyyy", "M/d/yyyy", "yyyy-MM-dd", "MMM yyyy", "MMMM yyyy" };
            if (DateTime.TryParseExact(ds, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime d)) return new DateTime(d.Year, d.Month, DateTime.DaysInMonth(d.Year, d.Month));
            return DateTime.Now;
        }

        private BarcodeResult Fail(string m) => new BarcodeResult { Success = false, ErrorMessage = m };
    }
}
