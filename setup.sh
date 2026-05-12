#!/bin/bash
# ============================================================================
# AutoFact Installation & Configuration Script
# This script configures the application for the current PC/VM setup
# ============================================================================

set -e

echo "========================================"
echo "  AutoFact Installation Configuration"
echo "========================================"
echo ""

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

# ============================================================================
# Step 1: Detect VM IP
# ============================================================================
echo -e "${YELLOW}[1/4] Detecting VM IP Address...${NC}"
echo ""
echo "What is the IP address of your MariaDB VM?"
echo "To find it, run on your VM: ip addr show"
echo ""
read -p "Enter VM IP (e.g., 192.168.56.200): " VM_IP

if [ -z "$VM_IP" ]; then
    echo -e "${RED}Error: VM IP cannot be empty${NC}"
    exit 1
fi

echo -e "${GREEN}✓ VM IP set to: $VM_IP${NC}"
echo ""

# ============================================================================
# Step 2: Test connectivity
# ============================================================================
echo -e "${YELLOW}[2/4] Testing connectivity to VM...${NC}"

if ping -c 1 "$VM_IP" &> /dev/null; then
    echo -e "${GREEN}✓ VM is reachable${NC}"
else
    echo -e "${RED}✗ Cannot ping VM at $VM_IP${NC}"
    echo "Please check:"
    echo "  1. VM is running"
    echo "  2. IP address is correct"
    echo "  3. Network is properly configured"
    exit 1
fi

# Test MariaDB port (if nc is available)
if command -v nc &> /dev/null; then
    if nc -zv "$VM_IP" 3306 &> /dev/null; then
        echo -e "${GREEN}✓ MariaDB port (3306) is accessible${NC}"
    else
        echo -e "${RED}✗ Cannot connect to MariaDB port on $VM_IP:3306${NC}"
        exit 1
    fi
fi
echo ""

# ============================================================================
# Step 3: Configure appsettings.json
# ============================================================================
echo -e "${YELLOW}[3/4] Configuring appsettings.json...${NC}"

APPSETTINGS_FILE="AutoFact/appsettings.json"

if [ ! -f "$APPSETTINGS_FILE" ]; then
    echo -e "${RED}Error: $APPSETTINGS_FILE not found${NC}"
    exit 1
fi

# Backup original
cp "$APPSETTINGS_FILE" "$APPSETTINGS_FILE.bak"
echo -e "${GREEN}✓ Backed up original to $APPSETTINGS_FILE.bak${NC}"

# Update connection string
cat > "$APPSETTINGS_FILE" << EOF
{
  "ConnectionStrings": {
    "Default": "server=$VM_IP;user id=app;password=Demaindeslaube;database=AutoFact"
  }
}
EOF

echo -e "${GREEN}✓ Updated connection string to:${NC}"
echo "  Server: $VM_IP"
echo "  Database: AutoFact"
echo "  User: app"
echo ""

# ============================================================================
# Step 4: MariaDB Configuration
# ============================================================================
echo -e "${YELLOW}[4/4] MariaDB Configuration${NC}"
echo ""
echo "Next, you need to configure MariaDB on your VM."
echo ""
echo "SSH or RDP into your VM and run these commands:"
echo ""
echo "--- Copy and paste into your VM terminal ---"
echo "sudo mariadb"
echo ""
echo "# Check if database exists"
echo "SHOW DATABASES;"
echo ""
echo "# If AutoFact doesn't exist, create it:"
echo "CREATE DATABASE IF NOT EXISTS AutoFact;"
echo ""
echo "# Create/update the app user with YOUR PC's IP"
echo "# (Replace 192.168.1.X with your actual PC IP, or use % for any IP)"
echo ""
read -p "Enter your PC's IP address (the machine running this script) [or press Enter for %]: " PC_IP
if [ -z "$PC_IP" ]; then
    PC_IP="%"
fi

cat << EOF

-- Replace the IP below with: $PC_IP
DROP USER IF EXISTS 'app'@'$PC_IP';
CREATE USER 'app'@'$PC_IP' IDENTIFIED BY 'Demaindeslaube';
GRANT ALL PRIVILEGES ON AutoFact.* TO 'app'@'$PC_IP';
FLUSH PRIVILEGES;

-- Verify it worked:
SELECT User, Host FROM mysql.user WHERE User = 'app';
exit

--- End of commands ---

EOF

echo ""
echo -e "${GREEN}Configuration complete!${NC}"
echo ""
echo "Next steps:"
echo "  1. Configure MariaDB on your VM (copy commands above)"
echo "  2. Run: dotnet run (or F5 in Visual Studio)"
echo "  3. Click 'S'inscrire' to create your first account"
echo ""
echo "For troubleshooting, see: QUICK_FIX.md"
