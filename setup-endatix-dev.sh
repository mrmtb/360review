#!/bin/bash

# MacOS Development Quick Start Script for Endatix
# This script helps you get Endatix running quickly on MacOS

set -e

echo "🚀 Endatix MacOS Development Setup"
echo "==================================="
echo ""

# Function to find psql in common Homebrew locations
find_psql() {
    # Check if psql is already in PATH
    if command -v psql &> /dev/null; then
        echo "$(command -v psql)"
        return 0
    fi
    
    # Common Homebrew paths
    local HOMEBREW_PATHS=(
        "/opt/homebrew/bin/psql"                    # Apple Silicon
        "/usr/local/bin/psql"                       # Intel Mac
        "/opt/homebrew/opt/postgresql/bin/psql"     # Homebrew PostgreSQL
        "/usr/local/opt/postgresql/bin/psql"        # Homebrew PostgreSQL (Intel)
        "/opt/homebrew/opt/postgresql@15/bin/psql"  # Versioned
        "/opt/homebrew/opt/postgresql@14/bin/psql"
        "/usr/local/opt/postgresql@15/bin/psql"
        "/usr/local/opt/postgresql@14/bin/psql"
    )
    
    for path in "${HOMEBREW_PATHS[@]}"; do
        if [ -f "$path" ]; then
            echo "$path"
            return 0
        fi
    done
    
    return 1
}

# Find and setup psql
PSQL_PATH=$(find_psql)
if [ -z "$PSQL_PATH" ]; then
    echo "❌ PostgreSQL client (psql) not found"
    echo ""
    echo "Homebrew PostgreSQL might not be in your PATH."
    echo ""
    echo "Try one of these:"
    echo "  1. Add Homebrew to PATH:"
    echo "     export PATH=\"/opt/homebrew/bin:\$PATH\"  # Apple Silicon"
    echo "     export PATH=\"/usr/local/bin:\$PATH\"     # Intel Mac"
    echo ""
    echo "  2. Find your PostgreSQL installation:"
    echo "     brew --prefix postgresql"
    echo ""
    echo "  3. Or install PostgreSQL:"
    echo "     brew install postgresql"
    echo ""
    exit 1
fi

# Add psql directory to PATH for this script
export PATH="$(dirname "$PSQL_PATH"):$PATH"

echo "✅ PostgreSQL client found at: $PSQL_PATH"

# Check if Docker is available
if command -v docker &> /dev/null; then
    echo "✅ Docker found"
    USE_DOCKER=true
else
    echo "⚠️  Docker not found - will use native .NET"
    USE_DOCKER=false
fi

echo ""

# Function to check if Endatix is already running
check_endatix() {
    if curl -s http://localhost:5001/api/health > /dev/null 2>&1; then
        return 0
    else
        return 1
    fi
}

# Check if Endatix is already running
if check_endatix; then
    echo "✅ Endatix is already running at http://localhost:5001"
    echo ""
    read -p "Do you want to restart it? (y/n) " -n 1 -r
    echo ""
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        if $USE_DOCKER; then
            echo "🔄 Restarting Docker container..."
            docker restart endatix-dev
        else
            echo "⚠️  Please stop the running Endatix process manually and run this script again"
            exit 1
        fi
    fi
else
    echo "ℹ️  Endatix is not running yet"
    echo ""
    
    # Step 1: Create database
    echo "Step 1: Creating Endatix database"
    echo "---------------------------------"
    
    # Try to detect the PostgreSQL user
    PG_USER="${USER}"
    if [ -f ".env" ]; then
        source .env
        if [ ! -z "$POSTGRES_USER" ]; then
            PG_USER="$POSTGRES_USER"
        fi
    fi
    
    read -p "Enter PostgreSQL username [$PG_USER]: " INPUT_USER
    PG_USER="${INPUT_USER:-$PG_USER}"
    
    echo "Creating database as user: $PG_USER"
    
    if psql -U "$PG_USER" -d postgres -c "CREATE DATABASE endatix;" 2>/dev/null; then
        echo "✅ Database 'endatix' created"
    else
        echo "ℹ️  Database 'endatix' might already exist (this is OK)"
    fi
    
    echo ""
    
    # Step 2: Start Endatix
    echo "Step 2: Starting Endatix"
    echo "------------------------"
    
    if $USE_DOCKER; then
        echo "🐳 Using Docker..."
        
        # Check if container already exists
        if docker ps -a | grep -q endatix-dev; then
            echo "Starting existing container..."
            docker start endatix-dev
        else
            echo "Creating new Docker container..."
            
            # Get PostgreSQL password
            read -s -p "Enter PostgreSQL password: " PG_PASS
            echo ""
            
            docker run -d \
                --name endatix-dev \
                -p 5001:80 \
                -e "ConnectionStrings__DefaultConnection=Host=host.docker.internal;Port=5432;Database=endatix;Username=$PG_USER;Password=$PG_PASS" \
                -e ASPNETCORE_ENVIRONMENT=Development \
                endatix/endatix:latest
            
            echo "✅ Docker container started"
        fi
        
    else
        echo "🔧 Using native .NET..."
        echo ""
        echo "⚠️  This script cannot automatically start native .NET Endatix"
        echo "   Please follow these steps manually:"
        echo ""
        echo "   1. Clone Endatix: git clone https://github.com/endatix/endatix.git"
        echo "   2. Configure appsettings.Development.json"
        echo "   3. Apply migrations: dotnet ef database update"
        echo "   4. Run: dotnet run --urls 'http://localhost:5001'"
        echo ""
        echo "   See MACOS-DEVELOPMENT-SETUP.md for detailed instructions"
        exit 0
    fi
    
    # Wait for Endatix to start
    echo ""
    echo "⏳ Waiting for Endatix to start..."
    sleep 5
    
    for i in {1..10}; do
        if check_endatix; then
            echo "✅ Endatix is running!"
            break
        else
            echo "   Still starting... ($i/10)"
            sleep 2
        fi
    done
fi

echo ""

# Check if Endatix is responsive
if check_endatix; then
    echo "✅ Endatix health check passed"
    echo "   URL: http://localhost:5001"
    echo ""
    
    # Check if template is already uploaded
    echo "Step 3: Checking for survey template"
    echo "------------------------------------"
    
    FORMS_COUNT=$(curl -s http://localhost:5001/api/forms | jq '. | length' 2>/dev/null || echo "0")
    
    if [ "$FORMS_COUNT" != "0" ]; then
        echo "✅ Templates already exist in Endatix"
        echo "   View them: curl http://localhost:5001/api/forms | jq ."
    else
        echo "ℹ️  No templates uploaded yet"
        echo ""
        read -p "Do you want to upload survey3.json now? (y/n) " -n 1 -r
        echo ""
        
        if [[ $REPLY =~ ^[Yy]$ ]]; then
            if [ -f "survey3.json" ]; then
                echo "📤 Uploading survey3.json..."
                
                RESPONSE=$(curl -s -X POST http://localhost:5001/api/forms \
                    -H "Content-Type: application/json" \
                    -d "{
                        \"name\": \"Leadership Competency Survey\",
                        \"description\": \"360-degree feedback survey\",
                        \"formDefinition\": $(cat survey3.json)
                    }")
                
                TEMPLATE_ID=$(echo $RESPONSE | jq -r '.id' 2>/dev/null || echo "")
                
                if [ ! -z "$TEMPLATE_ID" ] && [ "$TEMPLATE_ID" != "null" ]; then
                    echo "✅ Template uploaded successfully!"
                    echo ""
                    echo "📋 Template ID: $TEMPLATE_ID"
                    echo ""
                    echo "⚠️  IMPORTANT: Update appsettings.Development.json"
                    echo "   Set Survey:DefaultTemplateId to: $TEMPLATE_ID"
                    echo ""
                else
                    echo "❌ Failed to upload template"
                    echo "   Response: $RESPONSE"
                fi
            else
                echo "❌ survey3.json not found in current directory"
            fi
        fi
    fi
    
    echo ""
    echo "🎉 Setup Complete!"
    echo ""
    echo "Next steps:"
    echo "1. Update appsettings.Development.json with your template ID"
    echo "2. Apply 360Review migration: dotnet ef database update"
    echo "3. Start 360Review: dotnet run"
    echo "4. Test survey: http://localhost:5000/survey/{password}"
    echo ""
    echo "📚 Full guide: MACOS-DEVELOPMENT-SETUP.md"
    
else
    echo "❌ Endatix failed to start"
    echo "   Check logs: docker logs endatix-dev"
    exit 1
fi
