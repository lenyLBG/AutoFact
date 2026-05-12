# ============================================================================
# AutoFact - Installation Configuration Script (Windows PowerShell)
# ============================================================================
# This script automates the configuration of appsettings.json for multi-PC
# installations of AutoFact. It detects the VM IP, tests connectivity,
# and generates the correct configuration file.
#
# Usage: .\setup.ps1
# ============================================================================

param(
    [string]$VmIp = "",
    [int]$VmPort = 3306
)

# Colors for output
$RED = "`e[31m"
$GREEN = "`e[32m"
$YELLOW = "`e[33m"
$NC = "`e[0m"

Write-Host "╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║        AutoFact - Multi-PC Installation Configuration         ║" -ForegroundColor Cyan
Write-Host "║                    Windows PowerShell Setup                    ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# ============================================================================
# STEP 1: Detect VM IP
# ============================================================================
Write-Host "STEP 1: VM IP Detection" -ForegroundColor Yellow
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Yellow
Write-Host ""

if ([string]::IsNullOrWhiteSpace($VmIp)) {
    Write-Host "Enter the IP address of your MariaDB/MySQL server VM:"
    Write-Host "(e.g., 192.168.56.200, 192.168.1.100, etc.)"
    Write-Host ""
    $VmIp = Read-Host "VM IP Address"
    
    if ([string]::IsNullOrWhiteSpace($VmIp)) {
        Write-Host "${RED}✗ ERROR: VM IP cannot be empty${NC}" -ForegroundColor Red
        exit 1
    }
}

Write-Host "${GREEN}✓ Using VM IP: $VmIp${NC}" -ForegroundColor Green
Write-Host ""

# ============================================================================
# STEP 2: Test Connectivity
# ============================================================================
Write-Host "STEP 2: Network Connectivity Test" -ForegroundColor Yellow
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Yellow
Write-Host ""

# Test ping
Write-Host "Testing ping to $VmIp..." -NoNewline
$pingTest = Test-Connection -ComputerName $VmIp -Count 1 -Quiet -ErrorAction SilentlyContinue
if ($pingTest) {
    Write-Host " ${GREEN}✓ OK${NC}" -ForegroundColor Green
} else {
    Write-Host " ${RED}✗ FAILED${NC}" -ForegroundColor Red
    Write-Host ""
    Write-Host "${YELLOW}WARNING: VM is not reachable. Check:${NC}" -ForegroundColor Yellow
    Write-Host "  1. VM is running and network is configured"
    Write-Host "  2. IP address is correct ($VmIp)"
    Write-Host "  3. VirtualBox Host-Only network exists (if using VirtualBox)"
    Write-Host "  4. Firewall is not blocking ICMP packets"
    Write-Host ""
    Write-Host "Continue anyway? (y/n)"
    $continue = Read-Host
    if ($continue -ne "y" -and $continue -ne "Y") {
        Write-Host "${RED}Setup cancelled${NC}" -ForegroundColor Red
        exit 1
    }
}

# Test port connectivity
Write-Host "Testing TCP connection to $VmIp`:$VmPort..." -NoNewline
$tcpTest = $false
try {
    $socket = New-Object System.Net.Sockets.TcpClient
    $socket.Connect($VmIp, $VmPort)
    $tcpTest = $socket.Connected
    $socket.Close()
} catch {
    $tcpTest = $false
}

if ($tcpTest) {
    Write-Host " ${GREEN}✓ OK${NC}" -ForegroundColor Green
} else {
    Write-Host " ${YELLOW}⚠ PORT NOT ACCESSIBLE${NC}" -ForegroundColor Yellow
    Write-Host "  MariaDB may not be running or port 3306 is not accessible"
    Write-Host "  (This can be verified after configuration)"
}

Write-Host ""

# ============================================================================
# STEP 3: Generate appsettings.json
# ============================================================================
Write-Host "STEP 3: Generate Configuration File" -ForegroundColor Yellow
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Yellow
Write-Host ""

$appSettingsPath = "AutoFact\appsettings.json"

# Create the JSON content
$appSettingsContent = @"
{
  "ConnectionStrings": {
    "Default": "Server=$VmIp;Port=3306;Database=AutoFact;Uid=app;Pwd=Demaindeslaube;SslMode=none;ConnectionTimeout=30;"
  }
}
"@

# Backup existing file if it exists
if (Test-Path $appSettingsPath) {
    $backupPath = "$appSettingsPath.bak"
    Write-Host "Backing up existing appsettings.json..." -NoNewline
    Copy-Item -Path $appSettingsPath -Destination $backupPath -Force
    Write-Host " ${GREEN}✓ Saved as $backupPath${NC}" -ForegroundColor Green
}

# Write new file
try {
    Set-Content -Path $appSettingsPath -Value $appSettingsContent -Force
    Write-Host "Creating new appsettings.json..." -NoNewline
    Write-Host " ${GREEN}✓ Created${NC}" -ForegroundColor Green
} catch {
    Write-Host " ${RED}✗ FAILED${NC}" -ForegroundColor Red
    Write-Host "Error: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Configuration file content:" -ForegroundColor Cyan
Write-Host "────────────────────────────────────────────────────────────────"
Write-Host $appSettingsContent
Write-Host "────────────────────────────────────────────────────────────────"
Write-Host ""

# ============================================================================
# STEP 4: MariaDB User Configuration
# ============================================================================
Write-Host "STEP 4: MariaDB User Setup" -ForegroundColor Yellow
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Yellow
Write-Host ""

# Detect local PC IP (this PC's IP from the perspective of the VM)
Write-Host "To complete setup, MariaDB must have a user for this PC."
Write-Host ""
Write-Host "Common configurations:" -ForegroundColor Cyan
Write-Host "  • If using VirtualBox Host-Only: typically 192.168.56.1"
Write-Host "  • If on same LAN: use your PC's actual IP (ipconfig /all)"
Write-Host "  • If port-forwarded: may be different"
Write-Host ""
Write-Host "What is this PC's IP from the VM's perspective?"
Write-Host "(Leave blank for 192.168.56.1 - common for VirtualBox)"
Write-Host ""
$localIp = Read-Host "This PC's IP (or press Enter for 192.168.56.1)"
if ([string]::IsNullOrWhiteSpace($localIp)) {
    $localIp = "192.168.56.1"
}

Write-Host ""
Write-Host "${GREEN}✓ Configuration Complete!${NC}" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host ""
Write-Host "1. On the MariaDB server VM, create the user for this PC:"
Write-Host "   (Run these commands as root/admin on the VM)"
Write-Host ""
Write-Host "   ┌────────────────────────────────────────────────────────────┐"
Write-Host "   │ mysql -u root -p                                           │" -ForegroundColor Magenta
Write-Host "   │ MariaDB [(none)]> CREATE USER IF NOT EXISTS 'app'@'$localIp' │" -ForegroundColor Magenta
Write-Host "   │                   IDENTIFIED BY 'Demaindeslaube';          │" -ForegroundColor Magenta
Write-Host "   │ MariaDB [(none)]> GRANT ALL PRIVILEGES ON AutoFact.*       │" -ForegroundColor Magenta
Write-Host "   │                   TO 'app'@'$localIp';                       │" -ForegroundColor Magenta
Write-Host "   │ MariaDB [(none)]> FLUSH PRIVILEGES;                        │" -ForegroundColor Magenta
Write-Host "   │ MariaDB [(none)]> EXIT;                                    │" -ForegroundColor Magenta
Write-Host "   └────────────────────────────────────────────────────────────┘"
Write-Host ""

Write-Host "2. Test the connection from PowerShell:"
Write-Host ""
Write-Host "   ${YELLOW}Test-NetConnection -ComputerName $VmIp -Port 3306${NC}" -ForegroundColor Yellow
Write-Host ""

Write-Host "3. Run the test inscription script:"
Write-Host ""
Write-Host "   ${YELLOW}Invoke-MySqlQuery -Query (Get-Content 'AutoFact\sql\test_inscription.sql')${NC}" -ForegroundColor Yellow
Write-Host "   ${YELLOW}(or use MySQL Workbench to execute sql/test_inscription.sql)${NC}" -ForegroundColor Yellow
Write-Host ""

Write-Host "4. Launch the application:"
Write-Host ""
Write-Host "   ${YELLOW}dotnet run${NC}" -ForegroundColor Yellow
Write-Host ""

Write-Host "5. Try to register a new user:"
Write-Host "   Go to Login → Create Account and register"
Write-Host ""

Write-Host "╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Green
Write-Host "║                     Setup Complete! ✓                          ║" -ForegroundColor Green
Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Green
Write-Host ""

Write-Host "If you encounter issues, check:" -ForegroundColor Yellow
Write-Host "  • TROUBLESHOOTING_INSCRIPTION.md - Comprehensive diagnosis guide"
Write-Host "  • NETWORK_SETUP.md - Multi-PC configuration details"
Write-Host "  • QUICK_FIX.md - Fast reference for common issues"
Write-Host ""
