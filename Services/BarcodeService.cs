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
        private static Dictionary<string, (int k, int m, int pSlope, int count)> _calibrationSlopes = new();

        public BarcodeService(ILogger<BarcodeService> logger) { _logger = logger; }

        public List<BarcodeSample> GetSamples_PUBLIC() => GetSamples();
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
                        if (Environment.GetCommandLineArgs().Contains("--test-image")) Console.WriteLine($"  Loading Anchors from: {path}");
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
                            // Merge with hardcoded ones to ensure we have the ones I just added
                            foreach(var s in BarcodeSample.AllSamples) {
                                if (!_cachedSamples.Any(x => x.Full == s.Full)) _cachedSamples.Add(s);
                            }
                            AnalyzeCalibrationSlopes(_cachedSamples);
                            return _cachedSamples;
                        }

                    }

                }
            } catch (Exception ex) { 
                if (Environment.GetCommandLineArgs().Contains("--test-image")) Console.WriteLine($"  GetSamples Error: {ex.Message}");
            }
            if (_cachedSamples == null) 
            {
                if (Environment.GetCommandLineArgs().Intersect(new[] { "--test-image", "--test-table" }).Any()) Console.WriteLine("  WARNING: Falling back to Hardcoded Samples!");
                _cachedSamples = BarcodeSample.AllSamples;
            }
            
            // Strictly adhere to the 20-digit schema if requested
            var filteredSamples = _cachedSamples.Where(s => s.Full.Length >= 20).ToList();
            if (filteredSamples.Any()) {
                AnalyzeCalibrationSlopes(filteredSamples);
                return filteredSamples;
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
            
            if (Environment.GetCommandLineArgs().Intersect(new[] { "--test-image", "--test-table" }).Any()) {
                Console.WriteLine($"  Anchor Found: {sample?.Full ?? "NONE"} (SN:{sample?.Serial}) Length:{sample?.Full?.Length ?? 0}");
            }

            string pLotPart = "";
            string fullBarcode = "";


                // Use prefix from sample if found
                string currentPrefix = ic + bc + rc;

                if (sample == null)
                {
                    // Fallback if no anchor found
                    pLotPart = ((int.Parse(s4[^1..]) * 3 + 5) % 10).ToString() + new string((i.LotNumber ?? "0").Where(char.IsDigit).ToArray()).PadLeft(3, '0');
                    string cFinal = currentPrefix + dt + pLotPart + s4;
                    int weight = CalculateWeightedSum(cFinal);
                    int cs = (10 - weight % 10) % 10;
                    fullBarcode = cFinal + cs;
                }
                else
                {
                    // Use prefix from sample
                    if (sample.Full.Length >= 5) {
                        currentPrefix = sample.Full.Substring(0, 5);
                    }

                    int totalLen = sample.Full.Length;
                    int midLen = totalLen - 11 - 4 - 1;
                    string samplePLot = sample.Full.Substring(11, midLen);
                    
                    int sVal = int.Parse(s4);
                    int sampSVal = int.Parse(new string(sample.Serial.Where(char.IsDigit).ToArray()));
                    int deltaS = sVal - sampSVal;

                    int lotLen = 3;
                    int pLen = midLen - lotLen;
                    if (pLen < 0) { pLen = 0; lotLen = midLen; }
                    
                    string lotTarget = new string((i.LotNumber ?? "0").Where(char.IsDigit).ToArray());
                    string lotStr = (lotTarget.Length >= lotLen) ? lotTarget[^lotLen..] : lotTarget.PadLeft(lotLen, '0');
                    pLotPart = ""; 

                    // New Delta-Based Engine
                    string groupKey = sample.ItemCode + "_" + sample.RgtType;
                    int k = 2; // P-to-Cal slope
                    int m = 6; // SN-to-Cal slope
                    int ps = 3; // SN-to-P slope
                    
                    if (_calibrationSlopes.TryGetValue(groupKey, out var slopes)) {
                        if (slopes.count >= 2) {
                            k = slopes.k;
                            m = slopes.m;
                        }
                    }
                    
                    // Fixed high-confidence overrides for 20-digit analyzer
                    if (sample.ItemCode == "010") { k = 2; m = 0; } // Urea
                    if (sample.ItemCode == "013") { k = 2; m = 8; } // PHOS
                    if (sample.ItemCode == "022") { k = 6; m = 4; } // GGT
                    if (sample.ItemCode == "071") { k = 1; m = 0; } // CREA R1
                    
                    string pStr = "";
                    int pSampleNum = 0;
                    int sSampleNum = int.Parse(sample.Serial);
                    int sInputNum = int.Parse(s4);
                    
                    if (pLen > 0 && int.TryParse(samplePLot.Substring(0, pLen), out int parsedPS)) pSampleNum = parsedPS;

                    // Calculate New P
                    int deltaSLast = (sInputNum % 10) - (sSampleNum % 10);
                    int pModulo = (int)Math.Pow(10, pLen);
                    int pNewNum = (pSampleNum + ps * deltaSLast) % pModulo;
                    while (pNewNum < 0) pNewNum += pModulo;
                    pStr = pNewNum.ToString().PadLeft(pLen, '0');

                    // Calculate New Calibration (relative to anchor)
                    int deltaP = pNewNum - pSampleNum; 
                    int deltaSTens = (sInputNum / 10) - (sSampleNum / 10);
                    
                    // Anchor Calibration
                    int anchorCS = sample.Full.Last() - '0';
                    int wAnchor = CalculateWeightedSum(sample.Full[..^1]);
                    int anchorCal = (anchorCS + wAnchor) % 10;
                    
                    int targetCal = (anchorCal + k * deltaP + m * deltaSTens) % 10;
                    while (targetCal < 0) targetCal += 10;
                    
                    pLotPart = pStr + lotStr;
                    string cFinal = currentPrefix + dt + pLotPart + s4;
                    int currentWSum = CalculateWeightedSum(cFinal);
                    int cs = (targetCal - currentWSum) % 10;
                    if (cs < 0) cs += 10;
                    
                    fullBarcode = cFinal + cs;
                }

                return new BarcodeResult {
                    Success = true, 
                    BarcodeNumber = fullBarcode, 
                    BarcodeImageBase64 = GenerateBarcodeImage(fullBarcode),
                    Chem = i.Chem, GenItemCode = currentPrefix.Substring(0,3), GenBottleCode = currentPrefix.Substring(3,1), GenReagentCode = currentPrefix.Substring(4,1),
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

            if (Environment.GetCommandLineArgs().Contains("--test-image")) {
                Console.WriteLine($"  Searching for IC:{ic} BC:{bc} RC:{rc} Lot:{lotTarget} SN:{s4}");
                Console.WriteLine($"  Chem Samples: {chemistrySamples.Count}");
                var first = chemistrySamples.FirstOrDefault();
                if (first != null) Console.WriteLine($"  Sample 1: {first.Full} IC={first.ItemCode} BC={first.Full.Substring(3,1)} RC={first.Full.Substring(4,1)} Lot={first.Full.Substring(12,3)} SN={first.Serial}");
            }

            // Priority 1: Match IC, BC, RC, Lot, and find closest serial
            var lotSamples = chemistrySamples.Where(s => 
                s.Full.Length >= 15 && 
                s.Full.Substring(3, 1) == bc && 
                s.Full.Substring(4, 1) == rc &&
                s.Full.Substring(12, 3) == lotTarget).ToList();

            if (Environment.GetCommandLineArgs().Contains("--test-image")) {
                Console.WriteLine($"  Lot Samples found: {lotSamples.Count}");
            }

            if (lotSamples.Any()) {

                int targetSerial = int.Parse(s4);
                return lotSamples.OrderBy(s => {
                    if (int.TryParse(s.Serial, out int sv)) return Math.Abs(targetSerial - sv);
                    return 9999;
                }).First();
            }

            // Priority 2: Match IC, BC, RC and find closest serial (Lot differs)
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

            // Final Priority: Closest serial in chemistry
            int sVal = int.Parse(s4);
            return chemistrySamples.OrderBy(c => {
                if (int.TryParse(c.Serial, out int csv)) return Math.Abs(sVal - csv);
                return 9999;
            }).FirstOrDefault();
        }


        public string GenerateBarcodeImage(string b) {
            try {
                var options = new Code128EncodingOptions { 
                    Width = 1200, 
                    Height = 360, 
                    Margin = 20, 
                    PureBarcode = true,
                    ForceCodeset = Code128EncodingOptions.Codesets.C
                };
                var w = new BarcodeWriterPixelData { Format = BarcodeFormat.CODE_128, Options = options };
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
                var options = new Code128EncodingOptions { 
                    Width = 1000, 
                    Height = 250, 
                    Margin = 20, 
                    PureBarcode = true,
                    ForceCodeset = Code128EncodingOptions.Codesets.C
                };
                var w = new BarcodeWriterPixelData { Format = BarcodeFormat.CODE_128, Options = options };
                var d = w.Write(b); using var img = ImageSharpImage.LoadPixelData<Rgba32>(d.Pixels, d.Width, d.Height);
                using var ms = new System.IO.MemoryStream(); img.Save(ms, new SixLabors.ImageSharp.Formats.Png.PngEncoder());
                return ms.ToArray();
            } catch { return Array.Empty<byte>(); }
        }

        private void AnalyzeCalibrationSlopes(List<BarcodeSample> samples)
        {
            if (_calibrationSlopes.Any()) return;
            try
            {
                var grouped = samples.GroupBy(x => x.ItemCode + "_" + x.RgtType);
                foreach (var g in grouped)
                {
                    var valid = new List<(int p, int sLast, int sTens, int cal)>();
                    foreach (var s in g)
                    {
                        if (s.Full.Length < 18) continue;
                        int midLen = s.Full.Length - 16;
                        string pLot = s.Full.Substring(11, midLen);
                        int lotLen = (midLen >= 3) ? 3 : midLen;
                        int pLen = midLen - lotLen;
                        
                        if (int.TryParse(pLot.Substring(0, pLen), out int p))
                        {
                             string payload = s.Full.Substring(0, s.Full.Length - 1);
                             int w = CalculateWeightedSum(payload);
                             int cal = (s.Full.Last() - '0' + w) % 10;
                             int serial = int.Parse(s.Serial);
                             valid.Add((p, serial % 10, serial / 10, cal));
                        }
                    }

                    if (valid.Count < 2) continue;

                    // Vote for pSlope
                    var pVotes = new Dictionary<int, int>();
                    for (int i = 0; i < valid.Count; i++) {
                        for (int j = i + 1; j < valid.Count; j++) {
                            int ds = valid[j].sLast - valid[i].sLast;
                            int dp = valid[j].p - valid[i].p;
                            for (int sCand = 0; sCand < 10; sCand++) {
                                if ((sCand * ds) % 10 == dp % 10) pVotes[sCand] = pVotes.GetValueOrDefault(sCand) + 1;
                            }
                        }
                    }
                    int bestPS = pVotes.OrderByDescending(x => x.Value).FirstOrDefault().Key;

                    // Vote for (k, m)
                    var votes = new Dictionary<(int k, int m), int>();
                    for (int i = 0; i < valid.Count; i++)
                    {
                        for (int j = i + 1; j < valid.Count; j++)
                        {
                            int dp = valid[j].p - valid[i].p;
                            int ds = valid[j].sTens - valid[i].sTens;
                            int dc = valid[j].cal - valid[i].cal;
                            while (dc < 0) dc += 10;

                            for (int kC = 0; kC < 10; kC++) {
                                for (int mC = 0; mC < 10; mC++) {
                                    if ((kC * dp + mC * ds) % 10 == dc) {
                                        var pair = (kC, mC);
                                        votes[pair] = votes.GetValueOrDefault(pair) + 1;
                                    }
                                }
                            }
                        }
                    }

                    if (votes.Any()) {
                        var best = votes.OrderByDescending(x => x.Value).First().Key;
                        _calibrationSlopes[g.Key] = (best.k, best.m, bestPS, valid.Count);
                    }
                }
            }
            catch {}
        }



        private DateTime ParseExpiryDate(string ds)
        {
            var formats = new[] { 
                "dd/MM/yyyy", "d/M/yyyy", 
                "dd-MM-yyyy", "d-M-yyyy",
                "dd.MM.yyyy", "d.M.yyyy",
                "MM/dd/yyyy", "M/d/yyyy", 
                "yyyy-MM-dd", "MMM yyyy", "MMMM yyyy"
            };
            if (DateTime.TryParseExact(ds, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime d)) 
            {
                // Always use the last day of the month as seen in anchors (30/31)
                return new DateTime(d.Year, d.Month, DateTime.DaysInMonth(d.Year, d.Month));
            }
            return DateTime.Now;
        }


        private BarcodeResult Fail(string m) => new BarcodeResult { Success = false, ErrorMessage = m };
        private class RawSample { public string ic { get; set; } = ""; public string rt { get; set; } = "R1"; public string s { get; set; } = ""; public string f { get; set; } = ""; }
    }
}
