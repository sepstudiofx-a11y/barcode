using System;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace ReagentBarcode.Services
{
    public class LicenseService
    {
        private readonly string _licenseFilePath;
        private readonly ILogger<LicenseService> _logger;
        
        // 🔒 Hardcoded limits (Embedded in binary to prevent easy tampering via config files)
        private const int TrialDaysLimit = 28;
        private const int DailyPrintLimit = 50;
        private const string SecretKey = "B4rc0d3-G3n-Pr0-2024-(Secure-Edition)";

        public LicenseService(ILogger<LicenseService> logger)
        {
            _logger = logger;
            
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appDataPath, "ReagentBarcode");
            Directory.CreateDirectory(appFolder);
            _licenseFilePath = Path.Combine(appFolder, "license.dat");
        }

        public bool IsLicenseValid()
        {
            try
            {
                var data = GetLicenseData();
                DateTime expirationDate = data.FirstRunDate.AddDays(TrialDaysLimit);
                return DateTime.Now.Date < expirationDate.Date;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking license validity");
                return false;
            }
        }

        public int GetRemainingDays()
        {
            try
            {
                var data = GetLicenseData();
                DateTime expirationDate = data.FirstRunDate.AddDays(TrialDaysLimit);
                int remaining = (int)(expirationDate - DateTime.Now).TotalDays;
                return remaining > 0 ? remaining : 0;
            }
            catch { return 0; }
        }

        public bool CanGenerateToday()
        {
            try
            {
                var data = GetLicenseData();
                string today = DateTime.Now.ToString("yyyy-MM-dd");
                if (data.DailyUsage.TryGetValue(today, out int count))
                {
                    return count < DailyPrintLimit;
                }
                return true;
            }
            catch { return false; }
        }

        public int GetRemainingGeneratesToday()
        {
            try
            {
                var data = GetLicenseData();
                string today = DateTime.Now.ToString("yyyy-MM-dd");
                if (data.DailyUsage.TryGetValue(today, out int count))
                {
                    int r = DailyPrintLimit - count;
                    return r > 0 ? r : 0;
                }
                return DailyPrintLimit;
            }
            catch { return 0; }
        }

        public void IncrementUsage()
        {
            try
            {
                var data = GetLicenseData();
                string today = DateTime.Now.ToString("yyyy-MM-dd");
                
                if (data.DailyUsage.ContainsKey(today))
                    data.DailyUsage[today]++;
                else
                    data.DailyUsage[today] = 1;

                // Prune
                var old = data.DailyUsage.Keys
                    .Where(k => DateTime.TryParse(k, out var dt) && dt < DateTime.Now.AddDays(-7))
                    .ToList();
                foreach (var d in old) data.DailyUsage.Remove(d);

                SaveLicenseData(data);
            }
            catch (Exception ex) { _logger.LogError(ex, "Error incrementing usage"); }
        }

        private LicenseData GetLicenseData()
        {
            if (File.Exists(_licenseFilePath))
            {
                try
                {
                    byte[] encrypted = File.ReadAllBytes(_licenseFilePath);
                    string json = Decrypt(encrypted);
                    // Use the global AppJsonContext via reflection or just use the type resolver if configured globally? 
                    // No, for direct calls we need to pass the TypeInfo. 
                    // Since AppJsonContext is internal in Program.cs, we can't see it easily if it's not in the same assembly/namespace setup ideally.
                    // BUT, Program.cs compiles into the main assembly. 
                    // Let's assume we can access AppJsonContext if we verify namespace. 
                    // Program.cs classes are usually top-level. 
                    
                    // Actually, simpler fix: Use a specific options instance if we can't access AppJsonContext easily, 
                    // OR make AppJsonContext public in ReagentBarcode namespace.
                    
                    // Let's rely on Type.GetType or just fix the Program.cs to put AppJsonContext in ReagentBarcode namespace.
                    var data = JsonSerializer.Deserialize(json, AppJsonContext.Default.LicenseData);
                    if (data != null && data.FirstRunDate != default) return data;
                }
                catch (Exception ex) { _logger.LogWarning(ex, "License corrupted, resetting..."); }
            }

            var newData = new LicenseData { FirstRunDate = DateTime.Now };
            SaveLicenseData(newData);
            return newData;
        }

        private void SaveLicenseData(LicenseData data)
        {
            try
            {
                string json = JsonSerializer.Serialize(data, AppJsonContext.Default.LicenseData);
                byte[] encrypted = Encrypt(json);
                File.WriteAllBytes(_licenseFilePath, encrypted);
            }
            catch (Exception ex) { _logger.LogError(ex, "Error saving license"); }
        }

        private byte[] Encrypt(string text)
        {
            byte[] data = System.Text.Encoding.UTF8.GetBytes(text);
            for (int i = 0; i < data.Length; i++)
                data[i] = (byte)(data[i] ^ SecretKey[i % SecretKey.Length]);
            return data;
        }

        private string Decrypt(byte[] data)
        {
            for (int i = 0; i < data.Length; i++)
                data[i] = (byte)(data[i] ^ SecretKey[i % SecretKey.Length]);
            return System.Text.Encoding.UTF8.GetString(data);
        }
    }

    public class LicenseData
    {
        public DateTime FirstRunDate { get; set; }
        public Dictionary<string, int> DailyUsage { get; set; } = new();
    }
}
