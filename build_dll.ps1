#!/usr/bin/env pwsh

Write-Host "🔨 Building DatabaseReader.dll directly..." -ForegroundColor Green

# Create directories
$binDir = "bin\Release\net8.0"
New-Item -ItemType Directory -Path $binDir -Force | Out-Null

# Get .NET reference assemblies path
$dotnetPath = (Get-Command dotnet).Source | Split-Path
$runtimePath = Join-Path $dotnetPath "shared\Microsoft.NETCore.App"
$latestRuntime = Get-ChildItem $runtimePath | Sort-Object Name -Descending | Select-Object -First 1
$runtimeDlls = Join-Path $latestRuntime.FullName

# Find Npgsql package
$nugetPath = [Environment]::GetFolderPath("UserProfile") + "\.nuget\packages"
$npgsqlPath = Join-Path $nugetPath "npgsql\8.0.3\lib\net8.0\Npgsql.dll"

Write-Host "Runtime path: $runtimeDlls" -ForegroundColor Gray
Write-Host "Npgsql path: $npgsqlPath" -ForegroundColor Gray

if (-not (Test-Path $npgsqlPath)) {
    Write-Host "❌ Npgsql not found. Installing..." -ForegroundColor Red
    dotnet add package Npgsql --version 8.0.3
}

# Compile using dotnet
$compileArgs = @(
    "DatabaseReader.cs"
    "/target:library"
    "/out:$binDir\DatabaseReader.dll"
    "/reference:$runtimeDlls\System.Runtime.dll"
    "/reference:$runtimeDlls\System.Data.Common.dll"
    "/reference:$runtimeDlls\System.Threading.dll"
    "/reference:$runtimeDlls\System.Collections.dll"
    "/reference:$runtimeDlls\System.Linq.dll"
    "/reference:$npgsqlPath"
)

try {
    # Use the C# compiler from the .NET SDK
    $cscPath = Join-Path $dotnetPath "Roslyn\bincore\csc.dll"
    
    if (Test-Path $cscPath) {
        Write-Host "✅ Using Roslyn compiler..." -ForegroundColor Green
        & dotnet $cscPath $compileArgs
    } else {
        Write-Host "⚠️ Falling back to dotnet build..." -ForegroundColor Yellow
        # Try direct dotnet compilation
        dotnet build DatabaseReader.csproj --configuration Release --no-restore --verbosity minimal 2>$null
    }
    
    if (Test-Path "$binDir\DatabaseReader.dll") {
        Write-Host "✅ DatabaseReader.dll built successfully!" -ForegroundColor Green
        $fileInfo = Get-Item "$binDir\DatabaseReader.dll"
        Write-Host "   Size: $($fileInfo.Length) bytes" -ForegroundColor Gray
        Write-Host "   Modified: $($fileInfo.LastWriteTime)" -ForegroundColor Gray
    } else {
        Write-Host "❌ Failed to build DatabaseReader.dll" -ForegroundColor Red
    }
} catch {
    Write-Host "❌ Build failed: $($_.Exception.Message)" -ForegroundColor Red
}