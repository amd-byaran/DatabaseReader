# DatabaseReader .NET Assembly

This is a .NET 8.0 class library that provides PostgreSQL database connectivity for coverage analysis data.

## 📦 Assembly Details

- **Target Framework**: .NET 8.0
- **Output Type**: Class Library (DLL)
- **Main Assembly**: `DatabaseReader.dll`
- **Dependencies**:
  - Npgsql 8.0.3 (PostgreSQL driver)
  - .NET 8.0 runtime

## 🚀 Usage

### Method 1: Reference the DLL directly

1. Add `DatabaseReader.dll` to your project references
2. Add the required NuGet packages:
   ```bash
   dotnet add package Npgsql --version 8.0.3
   ```

3. Use in your code:
   ```csharp
   using DatabaseReader;

   // Example usage
   var projects = DcPgConn.GetAllProjects();
   var releases = DcPgConn.GetAllReleases();
   ```

### Method 2: Install NuGet Package

```bash
dotnet add package DatabaseReader --version 1.0.0 --source ./bin/Release
```

## 📋 Available Methods

The `DcPgConn` class provides the following static methods:

- `GetAllProjects()` - Returns all projects
- `GetAllReleases()` - Returns all releases
- `GetReportsForRelease(int releaseId)` - Get reports for a specific release
- `GetReportInfo(int reportId)` - Get detailed report information
- `GetChangelistsForReport(int reportId)` - Get changelists for a report
- `GetReportPath(int reportId)` - Generate file path for a report
- `SelectFirst<T>(string query, params object[] parameters)` - Generic first result query
- `SelectAll<T>(string query, params object[] parameters)` - Generic all results query

## 🔧 Build Instructions

```bash
# Build the DLL
dotnet build --configuration Release

# Create NuGet package
dotnet pack --configuration Release
```

## 📁 Output Files

After building, you'll find these files in `bin/Release/net8.0/`:
- `DatabaseReader.dll` - Main assembly
- `DatabaseReader.pdb` - Debug symbols
- `DatabaseReader.deps.json` - Dependencies file

And in `bin/Release/`:
- `DatabaseReader.1.0.0.nupkg` - NuGet package

## ⚠️ Requirements

- .NET 8.0 runtime
- PostgreSQL database with coverage analysis schema
- Valid database connection string (configured in code)

## 📝 Notes

- The assembly includes retry logic and error handling
- All database operations are asynchronous where appropriate
- Connection pooling is handled automatically by Npgsql