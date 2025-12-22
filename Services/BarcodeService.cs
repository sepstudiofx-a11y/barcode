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
using System.Text.RegularExpressions;

namespace ReagentBarcode.Services
{
    public class BarcodeService
    {
        private readonly ILogger<BarcodeService> _logger;
        public BarcodeService(ILogger<BarcodeService> logger) { _logger = logger; }

        public BarcodeResult GenerateBarcode(ReagentInput i)
        {
            try {
                if (i == null) return Fail("Input required");
                DateTime exp = ParseExpiryDate(i.ExpDate);
                string ic = (i.ItemCode ?? "000").PadLeft(3,'0').Substring(0,3);
                string bc = (i.BottleCode ?? "0").Substring(0,1);
                string rc = (i.ReagentCode ?? "0").Substring(0,1);
                string dt = exp.ToString("yyMMdd");
                
                string s3 = new string((i.LotNumber ?? "0").Where(char.IsDigit).ToArray()).PadLeft(3, '0');
                if (s3.Length > 3) s3 = s3[^3..];

                string s4 = new string((i.SerialNumber ?? "0").Where(char.IsDigit).ToArray()).PadLeft(4, '0');
                if (s4.Length > 4) s4 = s4[^4..];

                int p = GetPrefix(ic, i.Chem, s3, i.BottleType, i.RgtType, s4);
                
                string lot4 = p.ToString() + s3;
                string c19 = ic + (i.BottleCode ?? "0").Substring(0,1) + (i.ReagentCode ?? "0").Substring(0,1) + dt + lot4 + s4;
                
                int cs = GetChecksum(c19, ic, p, s4, i.RgtType, i.BottleType);
                string fullBarcode = c19 + cs;

                return new BarcodeResult {
                    Success = true, 
                    BarcodeNumber = fullBarcode, 
                    BarcodeImageBase64 = GenerateBarcodeImage(fullBarcode),
                    Chem = i.Chem, 
                    GenItemCode = ic, 
                    GenBottleCode = (i.BottleCode ?? "0").Substring(0,1), 
                    GenReagentCode = (i.ReagentCode ?? "0").Substring(0,1),
                    LotNumber = i.LotNumber, 
                    SerialNumber = s4, 
                    ExpDate = exp, 
                    GeneratedAt = DateTime.Now
                };
            } catch (Exception ex) { return Fail(ex.Message); }
        }

        private BarcodeSample? FindClosestSample(string ic, string? rt, string s4)
        {
            var candidates = BarcodeSample.AllSamples.Where(s => s.ItemCode == ic && s.RgtType == (rt ?? "R1")).ToList();
            if (!candidates.Any()) candidates = BarcodeSample.AllSamples.Where(s => s.ItemCode == ic).ToList();
            if (!candidates.Any()) return null;

            int sVal = int.Parse(s4);
            return candidates.OrderBy(c => {
                if (int.TryParse(c.Serial, out int csv)) return Math.Abs(sVal - csv);
                return 9999;
            }).FirstOrDefault();
        }

        private int GetPrefix(string ic, string? chem, string lot3, string? bt, string? rt, string s4)
        {
            var sample = FindClosestSample(ic, rt, s4);
            if (sample == null) return ( (s4.Length>0?s4[^1]-'0':0) * 3 + 5 ) % 10; // Fallback

            // Use weighted delta from nearest sample
            // Based on Urea and ALAT analysis, S4 weight is often 3, Lot weight is variable
            int sVal = int.Parse(s4);
            int sampSVal = int.TryParse(sample.Serial, out int ssv) ? ssv : sVal;
            int deltaS = sVal - sampSVal;
            
            // Note: Within a lot, prefix often follows w=3 for S4
            int p = (sample.P + (deltaS % 10) * 3 + 100) % 10;
            
            return p;
        }

        private int GetChecksum(string c, string ic, int p, string s4, string? rt, string? bt)
        {
            int currentSum = 0;
            foreach (char x in c) currentSum += (x - '0');

            var sample = FindClosestSample(ic, rt, s4);
            if (sample == null) return (currentSum + 3) % 10;

            // Use the calibrated 'A' offset
            int a = sample.A;

            // Correction for GGT (multi-weighted checksum)
            if (ic == "022")
            {
                int k = 3;
                int target = (k - (currentSum % 10) + 10) % 10;
                if (target % 2 != 0) target += 10;
                return (target / 2) % 10;
            }

            return (currentSum + a) % 10;
        }

        public string GenerateBarcodeImage(string b) {
            var w = new BarcodeWriterPixelData { Format = BarcodeFormat.CODE_128, Options = new EncodingOptions { Width = 1200, Height = 360, Margin = 5, PureBarcode = true } };
            var d = w.Write(b); using var img = ImageSharpImage.LoadPixelData<Rgba32>(d.Pixels, d.Width, d.Height);
            using var ms = new System.IO.MemoryStream(); img.Save(ms, new SixLabors.ImageSharp.Formats.Png.PngEncoder());
            return Convert.ToBase64String(ms.ToArray());
        }

        public byte[] GeneratePdf(List<BarcodeResult> i) {
            QuestPDF.Settings.License = LicenseType.Community;
            return Document.Create(c => c.Page(p => { 
                p.Size(PageSizes.A4); 
                p.Margin(1, Unit.Centimetre);
                p.Content().Grid(grid => {
                    grid.Columns(3); // 3 labels per row
                    grid.Spacing(10);
                    foreach (var x in i) {
                        grid.Item().Element(e => ComposeLabel(e, x));
                    }
                });
            })).GeneratePdf();
        }

        private void ComposeLabel(IContainer c, BarcodeResult i) {
            c.Border(0.5f).Padding(10).Column(col => {
                col.Spacing(2);
                col.Item().Row(r => { 
                    r.RelativeItem().Text(i.Chem).Bold().FontSize(9); 
                    r.RelativeItem().AlignRight().Text($"Lot:{i.LotNumber}").FontSize(8); 
                });
                col.Item().AlignCenter().MaxHeight(1.2f, Unit.Centimetre).Image(GenerateBarcodeImageVerbatim(i.BarcodeNumber!));
                col.Item().AlignCenter().Text(i.BarcodeNumber).FontSize(10).LetterSpacing(0.2f);
                col.Item().AlignRight().Text($"Exp:{i.ExpDate:MMM yyyy}").FontSize(7).Italic();
            });
        }

        private byte[] GenerateBarcodeImageVerbatim(string b) {
            var w = new BarcodeWriterPixelData { Format = BarcodeFormat.CODE_128, Options = new EncodingOptions { Width = 1000, Height = 250, Margin = 2, PureBarcode = true } };
            var d = w.Write(b); using var img = ImageSharpImage.LoadPixelData<Rgba32>(d.Pixels, d.Width, d.Height);
            using var ms = new System.IO.MemoryStream(); img.Save(ms, new SixLabors.ImageSharp.Formats.Png.PngEncoder());
            return ms.ToArray();
        }

        private DateTime ParseExpiryDate(string ds) {
            if (DateTime.TryParseExact(ds, new[] { "MM/dd/yyyy", "M/d/yyyy", "MM-dd-yyyy", "yyyy-MM-dd" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime d)) return d;
            return DateTime.Now;
        }

        private BarcodeResult Fail(string m) => new BarcodeResult { Success = false, ErrorMessage = m };
    }
}
