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

        public IActionResult Index()
        {
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
                return Json(new { success = true, message = "Connection successful!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Connection failed: " + ex.Message });
            }
        }

        [HttpPost]
        public IActionResult Save(ConnectionSettings settings)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(settings, options);
                System.IO.File.WriteAllText(_settingsFilePath, json);
                return Json(new { success = true, message = "Settings saved successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Failed to save settings: " + ex.Message });
            }
        }

        private ConnectionSettings LoadSettings()
        {
            if (System.IO.File.Exists(_settingsFilePath))
            {
                var json = System.IO.File.ReadAllText(_settingsFilePath);
                return JsonSerializer.Deserialize<ConnectionSettings>(json);
            }
            return new ConnectionSettings 
            { 
                Server = "(localdb)\\MSSQLLocalDB", 
                Database = "BA80", 
                IntegratedSecurity = true 
            };
        }

        private string BuildConnectionString(ConnectionSettings settings)
        {
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = settings.Server,
                InitialCatalog = settings.Database,
                IntegratedSecurity = settings.IntegratedSecurity,
                TrustServerCertificate = true,
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
    }
}
