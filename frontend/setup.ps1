# Quick Start Script for Windows PowerShell

Write-Host "🚀 Starting AI.EnterpriseRAG Frontend Setup..." -ForegroundColor Green
Write-Host ""

# Check if Node.js is installed
Write-Host "Checking Node.js installation..." -ForegroundColor Yellow
$nodeVersion = node --version 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Node.js is not installed!" -ForegroundColor Red
    Write-Host "Please download and install Node.js from https://nodejs.org/" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Node.js version: $nodeVersion" -ForegroundColor Green
Write-Host ""

# Check if we're in the right directory
if (!(Test-Path "package.json")) {
    Write-Host "❌ package.json not found!" -ForegroundColor Red
    Write-Host "Please run this script from the frontend directory" -ForegroundColor Red
    exit 1
}

# Install dependencies
Write-Host "📦 Installing dependencies..." -ForegroundColor Yellow
npm install
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ npm install failed!" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Dependencies installed successfully" -ForegroundColor Green
Write-Host ""

# Create .env file if it doesn't exist
if (!(Test-Path ".env")) {
    Write-Host "📝 Creating .env file..." -ForegroundColor Yellow
    "VITE_API_URL=http://localhost:5000" | Out-File -FilePath ".env" -Encoding UTF8
    Write-Host "✅ .env file created" -ForegroundColor Green
    Write-Host ""
}

# Display next steps
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "✅ Setup Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "1. Start the backend server (in another terminal):" -ForegroundColor White
Write-Host "   cd ..\AI.EnterpriseRAG.WebAPI" -ForegroundColor Gray
Write-Host "   dotnet run" -ForegroundColor Gray
Write-Host ""
Write-Host "2. Start the frontend dev server:" -ForegroundColor White
Write-Host "   npm run dev" -ForegroundColor Gray
Write-Host ""
Write-Host "3. Open your browser:" -ForegroundColor White
Write-Host "   http://localhost:3000" -ForegroundColor Gray
Write-Host ""
Write-Host "4. Login with demo account:" -ForegroundColor White
Write-Host "   Username: admin" -ForegroundColor Gray
Write-Host "   Password: Admin@123" -ForegroundColor Gray
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan

# Ask if user wants to start dev server
Write-Host ""
$start = Read-Host "Do you want to start the dev server now? (Y/N)"
if ($start -eq "Y" -or $start -eq "y") {
    Write-Host ""
    Write-Host "🚀 Starting dev server..." -ForegroundColor Green
    Write-Host "Press Ctrl+C to stop the server" -ForegroundColor Yellow
    Write-Host ""
    npm run dev
}
