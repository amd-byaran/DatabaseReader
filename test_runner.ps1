#!/usr/bin/env pwsh

Write-Host "🧪 Running DatabaseReader Tests..." -ForegroundColor Cyan

# Navigate to project root
Set-Location "c:\Users\byaran\OneDrive - Advanced Micro Devices Inc\programming\coverage_analyzer\DatabaseReader"

Write-Host "📦 Building Release version..." -ForegroundColor Yellow
dotnet build DatabaseReader.csproj --configuration Release --verbosity minimal

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Build successful!" -ForegroundColor Green
    
    Write-Host "🔍 Running DLL verification test..." -ForegroundColor Yellow
    dotnet run --project VerifyDLL\VerifyDLL.csproj
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ All tests passed!" -ForegroundColor Green
    } else {
        Write-Host "❌ Tests failed!" -ForegroundColor Red
    }
} else {
    Write-Host "❌ Build failed!" -ForegroundColor Red
}