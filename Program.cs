using ReagentBarcode.Services;
using System.Text.Json.Serialization;
using ReagentBarcode.Models;
using ReagentBarcode.Controllers;
using System.Diagnostics;

// 🛑 FORCE KILL PORT 9010 USAGE BEFORE START
// 🛑 FORCE KILL PORT 9010 USAGE BEFORE START
if (args.Contains("--test-image"))
{
    Console.WriteLine("=== IMAGE TEST CASES ANALYSIS ===");
    var service = new BarcodeService(new Microsoft.Extensions.Logging.Abstractions.NullLogger<BarcodeService>());
    
    var samples = service.GetSamples_PUBLIC(); // I'll make it public for this
    Console.WriteLine("RELEVANT ANCHORS FOR 010 R2 Lot 009:");
    foreach(var s in samples.Where(x => x.ItemCode == "010" && x.Full.Substring(4,1) == "2" && x.Full.Substring(12,3) == "009")) {
        string pay = s.Full.Substring(0, s.Full.Length-1);
        int sum = 0; for(int j=0; j<pay.Length; j++) sum += (pay[pay.Length-1-j]-'0') * (j%2==0?3:1);
        int cal = (s.Full.Last()-'0' + sum) % 10;
        Console.WriteLine($"  SN:{s.Serial} P:{s.Full[11]} Cal:{cal} Full:{s.Full}");
    }
    Console.WriteLine();

    var cases = new[] {

        new { Name = "Top Right (Anchor)", Lot = "009", SN = "9426", Exp = "30/09/2024" },
        new { Name = "Top Left (New Lot)", Lot = "010", SN = "9426", Exp = "30/09/2024" },
        new { Name = "Middle (New Date)",  Lot = "009", SN = "9426", Exp = "30/09/2026" },
        new { Name = "Bottom (New Serial)", Lot = "009", SN = "9536", Exp = "30/09/2024" }
    };

    foreach (var c in cases) {
        var res = service.GenerateBarcode(new ReagentInput { 
            Chem = "UREA II GEN", ItemCode = "010", BottleCode = "1", ReagentCode = "2",
            LotNumber = c.Lot, SerialNumber = c.SN, ExpDate = c.Exp 
        });
        Console.WriteLine($"[{c.Name}]");
        Console.WriteLine($"  Input: Lot={c.Lot}, SN={c.SN}, Exp={c.Exp}");
        Console.WriteLine($"  Output Barcode: {res.BarcodeNumber}");
        
        // Internal Analysis
        string p = res.BarcodeNumber!.Substring(0, res.BarcodeNumber.Length - 1);
        int cs = res.BarcodeNumber.Last() - '0';
        int sum = 0;
        for(int j=0; j<p.Length; j++) {
            int d = p[p.Length-1-j] - '0';
            sum += d * (j%2==0 ? 3 : 1);
        }
        int cal = (cs + sum) % 10;
        Console.WriteLine($"  Verify: WSum={sum}, CS={cs}, Calibration={cal}");
        Console.WriteLine();
    }
    return;
}


if (args.Contains("--test-table"))
{
    Console.WriteLine("=== TABLE TEST CASES ANALYSIS ===");
    var service = new BarcodeService(new Microsoft.Extensions.Logging.Abstractions.NullLogger<BarcodeService>());
    var samples = service.GetSamples_PUBLIC();
    
    string[] ics = { "006", "022", "013", "002" };
    foreach(var ic in ics) {
        Console.WriteLine($"RELEVANT ANCHORS FOR {ic}:");
        var filtered = samples.Where(x => x.ItemCode == ic).Take(3).ToList();
        foreach(var s in filtered) {
           Console.WriteLine($"  SN:{s.Serial} P:{s.Full[11]} Full:{s.Full}");
        }
    }
    var cases = new List<(string Name, ReagentInput Input, string Expected)> {
        // ALAT (015)
        ("ALAT R1 L28 SN0823", new ReagentInput { Chem="ALAT", ItemCode="015", BottleCode="2", ReagentCode="1", LotNumber="028", SerialNumber="0823", ExpDate="30/09/2025" }, "01521250930702808233"),
        ("ALAT R2 L30 SN0192", new ReagentInput { Chem="ALAT", ItemCode="015", BottleCode="1", ReagentCode="2", LotNumber="030", SerialNumber="0192", ExpDate="31/12/2025" }, "01512251231403001927"),

        // CREA ENZ (071)
        ("CREA R1 L64 SN5852", new ReagentInput { Chem="CREA ENZ", ItemCode="071", BottleCode="2", ReagentCode="1", LotNumber="064", SerialNumber="5852", ExpDate="30/09/2024" }, "07121240930706458521"),
        ("CREA R1 L70 SN0192", new ReagentInput { Chem="CREA ENZ", ItemCode="071", BottleCode="2", ReagentCode="1", LotNumber="070", SerialNumber="0192", ExpDate="31/10/2025" }, "07121251031307001921"), 
        ("CREA R2 L64 SN6747", new ReagentInput { Chem="CREA ENZ", ItemCode="071", BottleCode="1", ReagentCode="2", LotNumber="064", SerialNumber="6747", ExpDate="30/09/2024" }, "07112240930206467479"),

        // UREA (010)
        ("UREA R1 L09 SN8932", new ReagentInput { Chem="UREA II GEN", ItemCode="010", BottleCode="2", ReagentCode="1", LotNumber="009", SerialNumber="8932", ExpDate="30/09/2024" }, "01021240930200989327"),
        ("UREA R1 L09 SN8956", new ReagentInput { Chem="UREA II GEN", ItemCode="010", BottleCode="2", ReagentCode="1", LotNumber="009", SerialNumber="8956", ExpDate="30/09/2024" }, "01021240930400989563"),
        ("UREA R2 L13 SN0117", new ReagentInput { Chem="UREA II GEN", ItemCode="010", BottleCode="1", ReagentCode="2", LotNumber="013", SerialNumber="0117", ExpDate="30/11/2025" }, "01012251130701301175"),

        // PHOS (013)
        ("PHOS R1 L08 SN6077", new ReagentInput { Chem="PHOSPHORUS", ItemCode="013", BottleCode="2", ReagentCode="1", LotNumber="008", SerialNumber="6077", ExpDate="30/09/2024" }, "01321240930600860771"),

        // AMYLASE (017)
        ("AMYL R1 L21 SN8625", new ReagentInput { Chem="AMYLASE", ItemCode="017", BottleCode="2", ReagentCode="1", LotNumber="021", SerialNumber="8625", ExpDate="28/02/2025" }, "01721250228502186253"),

        // ASAT (016)
        ("ASAT R1 L26 SN0461", new ReagentInput { Chem="ASAT", ItemCode="016", BottleCode="2", ReagentCode="1", LotNumber="026", SerialNumber="0461", ExpDate="30/09/2025" }, "01621250930602604619"),

        // CHOL (002)
        ("CHOL R1 L08 SN9163", new ReagentInput { Chem="CHOL", ItemCode="002", BottleCode="2", ReagentCode="1", LotNumber="008", SerialNumber="9163", ExpDate="31/01/2025" }, "00221250131600891635"),

        // GLUC (001)
        ("GLUC R1 L33 SN8765", new ReagentInput { Chem="GLUCOSE", ItemCode="001", BottleCode="2", ReagentCode="1", LotNumber="033", SerialNumber="8765", ExpDate="31/12/2024" }, "00121241231203387655"),

        // HDL (025)
        ("HDL R1 L43 SN6616", new ReagentInput { Chem="HDL D", ItemCode="025", BottleCode="2", ReagentCode="1", LotNumber="043", SerialNumber="6616", ExpDate="31/01/2025" }, "02521250131404366167"),

        // LDL (026)
        ("LDL R2 L31 SN8611", new ReagentInput { Chem="LDL D", ItemCode="026", BottleCode="1", ReagentCode="2", LotNumber="031", SerialNumber="8611", ExpDate="30/11/2024" }, "02612241130303186111"),

        // TG (003)
        ("TG R1 L91 SN9059", new ReagentInput { Chem="TRIGLYCERIDES", ItemCode="003", BottleCode="2", ReagentCode="1", LotNumber="091", SerialNumber="9059", ExpDate="30/11/2024" }, "00321241130709190599")
    };



    int passCount = 0;
    foreach (var c in cases) {
        var res = service.GenerateBarcode(c.Input);
        bool pass = res.BarcodeNumber == c.Expected;
        if (pass) passCount++;
        
        Console.WriteLine($"[{(pass ? "PASS" : "FAIL")}] {c.Name}");
        Console.WriteLine($"  Input SN:{c.Input.SerialNumber} Lot:{c.Input.LotNumber}");
        Console.WriteLine($"  Expected: {c.Expected}");
        Console.WriteLine($"  Actual:   {res.BarcodeNumber}");
        Console.WriteLine();
    }
    Console.WriteLine($"Summary: {passCount}/{cases.Count} Passed");
    return;
}


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
