param(
    [string]$Server = "(localdb)\MSSQLLocalDB",
    [string]$Database = "BA80",
    [switch]$IntegratedSecurity = $true,
    [string]$Username,
    [string]$Password,
    [switch]$TrustServerCertificate = $true,
    [switch]$Encrypt = $false
)

Write-Host "Testing connection to SQL Server..." -ForegroundColor Cyan
Write-Host "Server: $Server"
Write-Host "Database: $Database"

# Fallback to System.Data.SqlClient if Microsoft.Data.SqlClient is not available
$provider = "System.Data.SqlClient"
$connStr = ""

try {
    if ($IntegratedSecurity) {
        $connStr = "Server=$Server;Database=$Database;Integrated Security=True;MultipleActiveResultSets=True;"
    } else {
        if ([string]::IsNullOrWhiteSpace($Username)) {
            Write-Error "Username is required when IntegratedSecurity is false."
            exit 1
        }
        $connStr = "Server=$Server;Database=$Database;User ID=$Username;Password=$Password;MultipleActiveResultSets=True;"
    }
    
    # Append TrustServerCertificate and Encrypt if using System.Data.SqlClient (keywords might slightly differ or be ignored if not supported, but usually safe in connection string)
    if ($TrustServerCertificate) { $connStr += "TrustServerCertificate=True;" }
    if ($Encrypt) { $connStr += "Encrypt=True;" } else { $connStr += "Encrypt=False;" }

} catch {
    Write-Error "Failed to build connection string: $_"
    exit 1
}

Write-Host "Connection String (Masked): $($connStr -replace 'Password=.*?;', 'Password=******;')" -ForegroundColor Gray

try {
    $conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
    $conn.Open()
    Write-Host "SUCCESS: Connected to database successfully!" -ForegroundColor Green
    
    # Try a simple query
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT @@VERSION"
    $version = $cmd.ExecuteScalar()
    Write-Host "SQL Server Version: $version" -ForegroundColor Green
    
    $conn.Close()
}
catch {
    Write-Host "FAILURE: Could not connect to database." -ForegroundColor Red
    Write-Host "Error Details: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.InnerException) {
        Write-Host "Inner Error: $($_.Exception.InnerException.Message)" -ForegroundColor Red
    }
}
