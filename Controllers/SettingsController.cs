using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace ReagentBarcode.Controllers
{
    public class SettingsController : Controller
    {
        private readonly string _settingsFilePath = Path.Combine(Directory.GetCurrentDirectory(), "ConnectionSettings.json");

        public IActionResult Index(string? message = null)
        {
            if (!string.IsNullOrEmpty(message))
            {
                ViewBag.Message = message;
            }
            var settings = LoadSettings();
            return View(settings);
        }

        [HttpPost]
        public async Task<IActionResult> TestConnection(ConnectionSettings settings)
        {
            var connectionString = BuildConnectionString(settings);
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                }
                return Json(new ConnectionTestResult { Success = true, Message = "Connection successful!" });
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                if (ex.InnerException != null) msg += " | Inner: " + ex.InnerException.Message;
                return Json(new ConnectionTestResult { Success = false, Message = "Connection failed: " + msg });
            }
        }

        [HttpPost]
        public IActionResult Save(ConnectionSettings settings)
        {
            try
            {
                var options = new JsonSerializerOptions { 
                    WriteIndented = true,
                    TypeInfoResolver = AppJsonContext.Default
                };
                var json = JsonSerializer.Serialize(settings, AppJsonContext.Default.ConnectionSettings);
                System.IO.File.WriteAllText(_settingsFilePath, json);
                return Json(new ConnectionTestResult { Success = true, Message = "Settings saved successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new ConnectionTestResult { Success = false, Message = "Failed to save settings: " + ex.Message });
            }
        }

        private ConnectionSettings LoadSettings()
        {
            if (System.IO.File.Exists(_settingsFilePath))
            {
                var json = System.IO.File.ReadAllText(_settingsFilePath);
                return JsonSerializer.Deserialize(json, AppJsonContext.Default.ConnectionSettings) ?? new ConnectionSettings();
            }
            return new ConnectionSettings 
            { 
                Server = "DESKTOP-TE2MER2\\BS360", 
                Database = "BA80", 
                IntegratedSecurity = true,
                TrustServerCertificate = true,
                Encrypt = false
            };
        }

        private string BuildConnectionString(ConnectionSettings settings)
        {
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = settings.Server,
                InitialCatalog = settings.Database,
                IntegratedSecurity = settings.IntegratedSecurity,
                TrustServerCertificate = settings.TrustServerCertificate,
                Encrypt = settings.Encrypt,
                MultipleActiveResultSets = true,
                ConnectTimeout = 10
            };

            if (!settings.IntegratedSecurity)
            {
                builder.UserID = settings.Username;
                builder.Password = settings.Password;
            }

            return builder.ConnectionString;
        }
    }

    public class ConnectionSettings
    {
        public string Server { get; set; }
        public string Database { get; set; }
        public bool IntegratedSecurity { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public bool TrustServerCertificate { get; set; } = true;
        public bool Encrypt { get; set; } = false;
    }

    public class ConnectionTestResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }
}
