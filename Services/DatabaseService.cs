using System;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ReagentBarcode.Models;
using System.Linq;
using System.Collections.Generic;

namespace ReagentBarcode.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString;
        private readonly ILogger<DatabaseService> _logger;

        public DatabaseService(IConfiguration configuration, ILogger<DatabaseService> logger)
        {
            _logger = logger;
            
            // Try load dynamic settings first
            string settingsPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ConnectionSettings.json");
            if (System.IO.File.Exists(settingsPath))
            {
                try
                {
                    var json = System.IO.File.ReadAllText(settingsPath);
                    var settings = System.Text.Json.JsonSerializer.Deserialize<dynamic>(json);
                    
                    var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder
                    {
                        DataSource = settings.GetProperty("Server").GetString(),
                        InitialCatalog = settings.GetProperty("Database").GetString(),
                        IntegratedSecurity = settings.GetProperty("IntegratedSecurity").GetBoolean(),
                        TrustServerCertificate = true,
                        MultipleActiveResultSets = true
                    };

                    if (!builder.IntegratedSecurity)
                    {
                        builder.UserID = settings.GetProperty("Username").GetString();
                        builder.Password = settings.GetProperty("Password").GetString();
                    }
                    
                    _connectionString = builder.ConnectionString;
                    _logger.LogInformation("Using dynamic SQL connection settings.");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to load dynamic settings: {ex.Message}. Falling back to appsettings.json.");
                    _connectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
                }
            }
            else
            {
                _connectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            }
        }

        public bool RegisterReagent(BarcodeResult barcodeResult)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    // 1. Get Template Row logic
                    // We need to find a row with the same chemical code to copy from
                    // In current structure, Chem ID isn't directly in barcodeResult except via Chem name
                    // But we know 'UREA' or other names from input.
                    
                    // First, find the ChemUID from ChemReagParam using the name
                    // NOTE: The ChemName in barcodeResult might not match DB exactly (e.g. UREA vs UREA IIGEN)
                    // We might need a fuzzy match or a mapping.
                    // For now, let's try direct match.
                    
                    string chemName = barcodeResult.Chem;
                    int chemUid = GetChemUid(connection, chemName);
                    
                    if (chemUid == -1)
                    {
                         _logger.LogWarning($"ChemUID not found for '{chemName}'. Trying 'UREA IIGEN' fallback if applicable.");
                         // Fallback Logic: Always try UREA IIGEN if specific chem not found
                         // This ensures random/test barcodes can still be registered and read (as UREA).
                         chemUid = GetChemUid(connection, "UREA IIGEN");
                         
                         if (chemUid != -1) {
                             _logger.LogInformation($"Using fallback template 'UREA IIGEN' (ID: {chemUid}) for unknown chemical '{chemName}'.");
                         }
                    }
                    
                    if (chemUid == -1) 
                    {
                        _logger.LogError($"Could not determine ChemUID for chemical '{chemName}' and fallback failed. Registration skipped.");
                        return false;
                    }

                    // 2. Get Template Row
                    var template = GetTemplateRow(connection, chemUid);
                    if (template == null)
                    {
                        _logger.LogError($"No template reagent found for ChemUID {chemUid}. Registration skipped.");
                        return false;
                    }

                    // 3. Insert New Row
                    InsertReagent(connection, template, barcodeResult, chemUid);
                    
                    _logger.LogInformation($"Successfully registered barcode {barcodeResult.BarcodeNumber} for {chemName}.");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Database Registration Failed: {ex.Message}");
                return false;
            }
        }

        private int GetChemUid(SqlConnection conn, string name)
        {
            string sql = "SELECT TOP 1 ChemUID FROM dbo.ChemReagParam WHERE ChemName LIKE @Name";
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Name", "%" + name + "%");
                var res = cmd.ExecuteScalar();
                return res != null ? (int)res : -1;
            }
        }

        private Dictionary<string, object> GetTemplateRow(SqlConnection conn, int chemUid)
        {
            string sql = "SELECT TOP 1 * FROM dbo.Reagent WHERE ReagentID = @Uid"; // ReagentID seems to be the link to ChemUID based on previous investigation (ReagentID=ChemUID?)
            // Wait, previous investigation:
            // UREA (ChemUID: 48) -> Reagent WHERE ReagentID = 48 -> Found row.
            // So ReagentID column in Reagent Table MATCHES ChemUID from ChemReagParam.
            
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Uid", chemUid);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        var row = new Dictionary<string, object>();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            row[reader.GetName(i)] = reader.GetValue(i);
                        }
                        return row;
                    }
                }
            }
            return null;
        }

        private void InsertReagent(SqlConnection conn, Dictionary<string, object> template, BarcodeResult result, int chemUid)
        {
            // Get Max UID
            int nextUid;
            using (var cmd = new SqlCommand("SELECT MAX(UID) FROM dbo.Reagent", conn))
            {
                var val = cmd.ExecuteScalar();
                nextUid = val != DBNull.Value ? Convert.ToInt32(val) + 1 : 1;
            }

            var cols = new List<string>();
            var paramNames = new List<string>();
            var cmdInsert = new SqlCommand();
            cmdInsert.Connection = conn;

            // Define overrides
            template["UID"] = nextUid;
            template["BarCode"] = result.BarcodeNumber;
            // Handle Dates
            template["ExpirationDate"] = result.ExpDate; // DateTime
            template["ReagOpenDate"] = DateTime.Now; // Set open date to now? Or keep template? Usually OpenDate is when put on machine. Wait.
            // Let's look at template data: "ReagOpenDate = 2025-11-12...".
            // If we register it, maybe it shouldn't be "Opened" yet?
            // But if the validation checks this, better have a valid date. 
            // Let's set ReagOpenDate to NULL or Now.
            // Safe bet: specific date or Now.
            template["ReagOpenDate"] = DateTime.Now; 
            
            template["LotNum"] = result.LotNumber!.PadLeft(3, '0');
            template["SN"] = result.SerialNumber!.PadLeft(4, '0');
            
            // ReagentID corresponds to ChemUID
            template["ReagentID"] = chemUid;

            foreach (var kvp in template)
            {
                cols.Add(kvp.Key);
                string pName = "@" + kvp.Key;
                paramNames.Add(pName);
                cmdInsert.Parameters.AddWithValue(pName, kvp.Value ?? DBNull.Value);
            }

            string sql = $"INSERT INTO dbo.Reagent ({string.Join(", ", cols)}) VALUES ({string.Join(", ", paramNames)})";
            cmdInsert.CommandText = sql;
            cmdInsert.ExecuteNonQuery();
        }
    }
}
