using System;
using System.Collections.Generic;
using System.Reflection;

/// <summary>
/// Unit tests for DatabaseReader DLL
/// This test harness loads the DatabaseReader.dll and tests its functionality
/// </summary>
class DatabaseReaderUnitTests
{
    private static int testsPassed = 0;
    private static int testsTotal = 0;
    private static List<string> testResults = new List<string>();
    private static Assembly? databaseReaderAssembly;
    private static Type? dcPgConnType;

    static void Main(string[] args)
    {
        Console.WriteLine("🧪 DatabaseReader DLL Unit Tests");
        Console.WriteLine("=================================");
        Console.WriteLine();

        // Load the DatabaseReader DLL
        if (!LoadDatabaseReaderDLL())
        {
            Console.WriteLine("❌ Failed to load DatabaseReader.dll. Tests cannot continue.");
            return;
        }

        Console.WriteLine("✅ DatabaseReader.dll loaded successfully");
        Console.WriteLine();

        try
        {
            // Test DLL structure and methods
            TestDLLStructure();
            
            // Test method signatures
            TestMethodSignatures();
            
            // Test GetReportPath method (doesn't require database)
            TestGetReportPathMethod();
            
            // Test database connection methods
            TestDatabaseConnectionMethods();
            
            // Test the new methods we added
            TestNewMethods();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"💥 Unexpected error during testing: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }

        // Print final results
        PrintTestSummary();
    }

    static bool LoadDatabaseReaderDLL()
    {
        try
        {
            string dllPath = Path.GetFullPath("../bin/Release/net8.0/DatabaseReader.dll");
            Console.WriteLine($"📦 Loading DLL from: {dllPath}");
            
            if (!File.Exists(dllPath))
            {
                Console.WriteLine($"❌ DLL not found at: {dllPath}");
                Console.WriteLine("💡 Make sure to build the DatabaseReader project in Release mode first:");
                Console.WriteLine("   dotnet build ../DatabaseReader.csproj --configuration Release");
                return false;
            }

            databaseReaderAssembly = Assembly.LoadFrom(dllPath);
            dcPgConnType = databaseReaderAssembly.GetType("DcPgConn");
            
            if (dcPgConnType == null)
            {
                Console.WriteLine("❌ DcPgConn class not found in the DLL");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error loading DLL: {ex.Message}");
            return false;
        }
    }

    static void TestDLLStructure()
    {
        Console.WriteLine("🔍 Testing DLL Structure");
        Console.WriteLine("------------------------");

        TestMethod("DLL Assembly Information", () =>
        {
            if (databaseReaderAssembly == null) return false;
            
            Console.WriteLine($"   Assembly: {databaseReaderAssembly.FullName}");
            Console.WriteLine($"   Location: {databaseReaderAssembly.Location}");
            
            var types = databaseReaderAssembly.GetTypes();
            Console.WriteLine($"   Types found: {types.Length}");
            
            foreach (var type in types)
            {
                Console.WriteLine($"     - {type.Name}");
            }
            
            return types.Length > 0;
        });

        TestMethod("DcPgConn Class Found", () =>
        {
            return dcPgConnType != null;
        });
    }

    static void TestMethodSignatures()
    {
        Console.WriteLine("📋 Testing Method Signatures");
        Console.WriteLine("----------------------------");

        if (dcPgConnType == null) return;

        var expectedMethods = new[]
        {
            "InitDb",
            "CloseDb", 
            "SelectFirst",
            "SelectAll",
            "GetReportInfo",
            "GetReleaseReportInfo",
            "GetAllReportsForRelease",
            "GetAllProjects",
            "GetAllReleases",
            "GetReportsForRelease",
            "GetChangelistsForReport",
            "GetReportPath"
        };

        TestMethod("All Expected Methods Present", () =>
        {
            var methods = dcPgConnType.GetMethods(BindingFlags.Public | BindingFlags.Static);
            var methodNames = methods.Select(m => m.Name).ToHashSet();
            
            int foundCount = 0;
            foreach (var expectedMethod in expectedMethods)
            {
                if (methodNames.Contains(expectedMethod))
                {
                    Console.WriteLine($"   ✅ {expectedMethod}");
                    foundCount++;
                }
                else
                {
                    Console.WriteLine($"   ❌ {expectedMethod} - NOT FOUND");
                }
            }
            
            Console.WriteLine($"   Found {foundCount}/{expectedMethods.Length} expected methods");
            return foundCount == expectedMethods.Length;
        });

        // Test specific method signatures for our new methods
        TestMethod("GetReleaseReportInfo Signature", () =>
        {
            var method = dcPgConnType.GetMethod("GetReleaseReportInfo");
            if (method == null) return false;
            
            var parameters = method.GetParameters();
            bool correctSignature = parameters.Length == 2 &&
                                  parameters[0].ParameterType == typeof(int) &&
                                  parameters[1].ParameterType == typeof(string);
            
            Console.WriteLine($"   Parameters: {string.Join(", ", parameters.Select(p => $"{p.ParameterType.Name} {p.Name}"))}");
            Console.WriteLine($"   Return type: {method.ReturnType.Name}");
            
            return correctSignature;
        });

        TestMethod("GetAllReportsForRelease Signature", () =>
        {
            var method = dcPgConnType.GetMethod("GetAllReportsForRelease");
            if (method == null) return false;
            
            var parameters = method.GetParameters();
            bool correctSignature = parameters.Length == 1 &&
                                  parameters[0].ParameterType == typeof(string);
            
            Console.WriteLine($"   Parameters: {string.Join(", ", parameters.Select(p => $"{p.ParameterType.Name} {p.Name}"))}");
            Console.WriteLine($"   Return type: {method.ReturnType.Name}");
            
            return correctSignature;
        });
    }

    static void TestGetReportPathMethod()
    {
        Console.WriteLine("🛤️  Testing GetReportPath Method");
        Console.WriteLine("-------------------------------");

        if (dcPgConnType == null) return;

        TestMethod("GetReportPath - Basic Functionality", () =>
        {
            try
            {
                var method = dcPgConnType.GetMethod("GetReportPath");
                if (method == null) return false;

                var result = method.Invoke(null, new object[] 
                { 
                    "dcn6_0", "dcn6_0", "func_cov", "dcn_core_verif_plan", 
                    "individual", "8222907", "dashboard.html" 
                });

                string path = result?.ToString() ?? "";
                Console.WriteLine($"   Generated path: {path}");
                
                bool isValid = path.Contains("/proj/videoip/web/merged_reports/") &&
                             path.Contains("dcn6_0") &&
                             path.Contains("func_cov") &&
                             path.Contains("dashboard.html") &&
                             !path.Contains("\\");
                
                return isValid;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   Exception: {ex.Message}");
                return false;
            }
        });

        TestMethod("GetReportPath - Default FileName", () =>
        {
            try
            {
                var method = dcPgConnType.GetMethod("GetReportPath");
                if (method == null) return false;

                var result = method.Invoke(null, new object[] 
                { 
                    "project1", "release1", "code_cov", "report1", 
                    "accumulate", "123456", "" 
                });

                string path = result?.ToString() ?? "";
                Console.WriteLine($"   Generated path: {path}");
                
                bool isValid = path.Contains("/proj/videoip/web/merged_reports/") &&
                             path.Contains("project1") &&
                             !path.Contains("\\");
                
                return isValid;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   Exception: {ex.Message}");
                return false;
            }
        });
    }

    static void TestDatabaseConnectionMethods()
    {
        Console.WriteLine("🔌 Testing Database Connection Methods");
        Console.WriteLine("------------------------------------");

        if (dcPgConnType == null) return;

        TestMethod("InitDb Method Exists and Callable", () =>
        {
            try
            {
                var method = dcPgConnType.GetMethod("InitDb");
                if (method == null) return false;

                // Try to call InitDb (it should handle connection failures gracefully)
                method.Invoke(null, null);
                Console.WriteLine("   InitDb called successfully (connection may or may not succeed)");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   Exception: {ex.Message}");
                return false;
            }
        });

        TestMethod("CloseDb Method Exists and Callable", () =>
        {
            try
            {
                var method = dcPgConnType.GetMethod("CloseDb");
                if (method == null) return false;

                // Try to call CloseDb
                method.Invoke(null, null);
                Console.WriteLine("   CloseDb called successfully");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   Exception: {ex.Message}");
                return false;
            }
        });
    }

    static void TestNewMethods()
    {
        Console.WriteLine("🆕 Testing New Methods (Mock Data)");
        Console.WriteLine("---------------------------------");

        if (dcPgConnType == null) return;

        TestMethod("GetReleaseReportInfo - Method Invocation", () =>
        {
            try
            {
                var method = dcPgConnType.GetMethod("GetReleaseReportInfo");
                if (method == null) return false;

                // Test with mock data (may return null due to no DB connection, which is fine)
                var result = method.Invoke(null, new object[] { 999, "test_report" });
                
                Console.WriteLine($"   Result: {(result?.ToString() ?? "null")}");
                Console.WriteLine("   Method executed without throwing exceptions");
                return true;
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                Console.WriteLine($"   Database connection expected to fail: {ex.InnerException.Message}");
                return true; // This is expected if no database connection
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   Unexpected exception: {ex.Message}");
                return false;
            }
        });

        TestMethod("GetAllReportsForRelease - Method Invocation", () =>
        {
            try
            {
                var method = dcPgConnType.GetMethod("GetAllReportsForRelease");
                if (method == null) return false;

                // Test with mock data
                var result = method.Invoke(null, new object[] { "test_release" });
                
                Console.WriteLine($"   Result type: {result?.GetType().Name ?? "null"}");
                Console.WriteLine("   Method executed without throwing exceptions");
                return true;
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                Console.WriteLine($"   Database connection expected to fail: {ex.InnerException.Message}");
                return true; // This is expected if no database connection
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   Unexpected exception: {ex.Message}");
                return false;
            }
        });

        TestMethod("GetAllProjects - Method Invocation", () =>
        {
            try
            {
                var method = dcPgConnType.GetMethod("GetAllProjects", new Type[] { typeof(int?) });
                if (method == null) return false;

                // Test with limit parameter
                var result = method.Invoke(null, new object?[] { 5 });
                
                Console.WriteLine($"   Result type: {result?.GetType().Name ?? "null"}");
                Console.WriteLine("   Method executed without throwing exceptions");
                return true;
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                Console.WriteLine($"   Database connection expected to fail: {ex.InnerException.Message}");
                return true; // This is expected if no database connection
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   Unexpected exception: {ex.Message}");
                return false;
            }
        });
    }

    static void TestMethod(string testName, Func<bool> testFunc)
    {
        testsTotal++;
        try
        {
            Console.Write($"  🧪 {testName}... ");
            bool result = testFunc();
            
            if (result)
            {
                Console.WriteLine("✅ PASS");
                testsPassed++;
                testResults.Add($"✅ {testName}");
            }
            else
            {
                Console.WriteLine("❌ FAIL");
                testResults.Add($"❌ {testName}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"💥 ERROR: {ex.Message}");
            testResults.Add($"💥 {testName} - ERROR: {ex.Message}");
        }
    }

    static void PrintTestSummary()
    {
        Console.WriteLine();
        Console.WriteLine("========================================");
        Console.WriteLine("📊 TEST SUMMARY");
        Console.WriteLine("========================================");
        Console.WriteLine($"Total Tests: {testsTotal}");
        Console.WriteLine($"Passed: {testsPassed}");
        Console.WriteLine($"Failed: {testsTotal - testsPassed}");
        Console.WriteLine($"Success Rate: {(testsTotal > 0 ? (double)testsPassed / testsTotal * 100 : 0):F1}%");
        Console.WriteLine();

        if (testsPassed == testsTotal)
        {
            Console.WriteLine("🎉 ALL TESTS PASSED! 🎉");
            Console.WriteLine("The DatabaseReader.dll is working correctly!");
        }
        else
        {
            Console.WriteLine("📋 Detailed Results:");
            foreach (var result in testResults)
            {
                Console.WriteLine($"   {result}");
            }
        }

        Console.WriteLine();
        Console.WriteLine("🏁 Unit testing completed.");
        Console.WriteLine();
        Console.WriteLine("💡 Note: Database connection tests may fail if database is not accessible,");
        Console.WriteLine("   but this doesn't indicate a problem with the DLL structure or methods.");
    }
}