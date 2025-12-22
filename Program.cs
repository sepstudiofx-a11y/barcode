using ReagentBarcode.Services;
using System.Text.Json.Serialization;
using ReagentBarcode.Models;
using ReagentBarcode.Controllers;
using System.Diagnostics;

// 🛑 FORCE KILL PORT 9010 USAGE BEFORE START
KillPortUser(9010);

var builder = WebApplication.CreateBuilder(args);


// Add services to the container.
// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        // Configure JSON options for both serialization and deserialization
        options.JsonSerializerOptions.PropertyNamingPolicy = null; // Use PascalCase to match C# model
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true; // Allow case-insensitive matching
        options.JsonSerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonContext.Default);
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register barcode services
builder.Services.AddScoped<BarcodeService>();
builder.Services.AddSingleton<BarcodeHistoryService>();
builder.Services.AddSingleton<LicenseService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllers();

// Use port 9010
// Clear any existing URL configurations first
string url = "http://localhost:9010";
app.Urls.Clear();
app.Urls.Add(url);

Console.WriteLine($"Application starting on {url}");

// Identify the execution mode
bool isService = !(Environment.UserInteractive);

try
{
    // Auto-open browser
    if (!isService) {
        Task.Run(async () => {
            await Task.Delay(1000); // Give server time to start
            try {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            } catch { /* Fail silently if browser can't open */ }
        });
    }

    app.Run();
}
catch (System.Net.Sockets.SocketException ex) when (ex.SocketErrorCode == System.Net.Sockets.SocketError.AddressAlreadyInUse)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"ERROR: Port 9010 is already in use!");
    Console.WriteLine("Please close the application using this port and try again.");
    Console.ResetColor();
    Console.WriteLine("Press any key to exit...");
    Console.ReadKey();
    Environment.Exit(1);
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"ERROR: Failed to start application: {ex.Message}");
    Console.ResetColor();
    Console.WriteLine("Press any key to exit...");
    Console.ReadKey();
    Environment.Exit(1);
}

static void KillPortUser(int port)
{
    try 
    {
        var psi = new ProcessStartInfo("netstat", "-ano") { RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true };
        var p = Process.Start(psi);
        string output = p.StandardOutput.ReadToEnd();
        p.WaitForExit();

        foreach (var line in output.Split(Environment.NewLine))
        {
            if (line.Contains($":{port}") && line.Contains("LISTENING"))
            {
                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0 && int.TryParse(parts[^1], out int pid))
                {
                    try 
                    { 
                        var proc = Process.GetProcessById(pid);
                        Console.WriteLine($"Stopping process on port {port}: {proc.ProcessName} (PID: {pid})...");
                        proc.Kill();
                        proc.WaitForExit(1000);
                    } 
                    catch { }
                }
            }
        }
    }
    catch { }
}

[JsonSerializable(typeof(DefinitionsResponse))]
[JsonSerializable(typeof(ReagentInput))]
[JsonSerializable(typeof(BarcodeResult))]
[JsonSerializable(typeof(List<BarcodeResult>))]
[JsonSerializable(typeof(ChemicalDto))]
[JsonSerializable(typeof(List<ChemicalDto>))]
[JsonSerializable(typeof(ChemicalItem))]
[JsonSerializable(typeof(List<ChemicalItem>))]
[JsonSerializable(typeof(BottleOption))]
[JsonSerializable(typeof(List<BottleOption>))]
[JsonSerializable(typeof(ReagentOption))]
[JsonSerializable(typeof(List<ReagentOption>))]
[JsonSerializable(typeof(ReagentBarcode.Services.LicenseData))]
internal partial class AppJsonContext : JsonSerializerContext
{
}
