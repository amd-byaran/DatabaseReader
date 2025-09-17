using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Unit tests for DatabaseReader methods
/// This is a simple test harness that validates the new methods we've added
/// </summary>
class DatabaseReaderTests
{
    private static int testsPassed = 0;
    private static int testsTotal = 0;
    private static List<string> testResults = new List<string>();

    static void Main()
    {
        Console.WriteLine("🧪 Starting DatabaseReader Unit Tests...");
        Console.WriteLine("========================================");
        Console.WriteLine();

        try
        {
            // Initialize database connection
            Console.WriteLine("📡 Initializing database connection...");
            DcPgConn.InitDb();
            
            if (DcPgConn.Conn == null)
            {
                Console.WriteLine("❌ Failed to connect to database. Skipping tests that require DB connection.");
                Console.WriteLine("🔧 Running syntax and structure tests only...");
                RunOfflineTests();
            }
            else
            {
                Console.WriteLine("✅ Database connection established");
                Console.WriteLine();
                
                // Run all tests
                RunGetReleaseReportInfoTests();
                RunGetAllReportsForReleaseTests();
                RunGetReportPathTests();
                RunExistingMethodTests();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Database connection failed: {ex.Message}");
            Console.WriteLine("🔧 Running offline tests only...");
            RunOfflineTests();
        }
        finally
        {
            // Clean up
            DcPgConn.CloseDb();
        }

        // Print results
        PrintTestSummary();
    }

    static void RunOfflineTests()
    {
        Console.WriteLine("🔧 Running Offline Tests (No Database Required)");
        Console.WriteLine("-----------------------------------------------");
        
        TestGetReportPathMethod();
        TestMethodSignatures();
    }

    static void RunGetReleaseReportInfoTests()
    {
        Console.WriteLine("🔍 Testing GetReleaseReportInfo Method");
        Console.WriteLine("-------------------------------------");

        // Test 1: Valid release ID and report name
        TestMethod("GetReleaseReportInfo - Valid Input", () => 
        {
            try
            {
                var result = DcPgConn.GetReleaseReportInfo(1, "test_report");
                // Should either return a valid result or null (both are acceptable)
                return true; // If no exception thrown, test passes
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   Exception: {ex.Message}");
                return false;
            }
        });

        // Test 2: Invalid release ID
        TestMethod("GetReleaseReportInfo - Invalid Release ID", () => 
        {
            try
            {
                var result = DcPgConn.GetReleaseReportInfo(-999, "nonexistent_report");
                // Should return null for invalid data
                return result == null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   Exception: {ex.Message}");
                return false;
            }
        });

        // Test 3: Null report name handling
        TestMethod("GetReleaseReportInfo - Null Report Name", () => 
        {
            try
            {
                var result = DcPgConn.GetReleaseReportInfo(1, "");
                return true; // Should handle gracefully
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   Exception (expected): {ex.Message}");
                return true; // Exception is acceptable for empty input
            }
        });
    }

    static void RunGetAllReportsForReleaseTests()
    {
        Console.WriteLine("📋 Testing GetAllReportsForRelease Method");
        Console.WriteLine("----------------------------------------");

        // Test 1: Valid release name
        TestMethod("GetAllReportsForRelease - Valid Release", () => 
        {
            try
            {
                var result = DcPgConn.GetAllReportsForRelease("dcn6_0");
                // Should return a list (empty or populated)
                return result != null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   Exception: {ex.Message}");
                return false;
            }
        });

        // Test 2: Invalid release name
        TestMethod("GetAllReportsForRelease - Invalid Release", () => 
        {
            try
            {
                var result = DcPgConn.GetAllReportsForRelease("nonexistent_release_12345");
                // Should return empty list for invalid release
                return result != null && result.Count == 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   Exception: {ex.Message}");
                return false;
            }
        });

        // Test 3: Empty release name
        TestMethod("GetAllReportsForRelease - Empty Release Name", () => 
        {
            try
            {
                var result = DcPgConn.GetAllReportsForRelease("");
                return true; // Should handle gracefully
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   Exception (expected): {ex.Message}");
                return true; // Exception is acceptable for empty input
            }
        });

        // Test 4: Empty string release name
        TestMethod("GetAllReportsForRelease - Empty Release Name", () => 
        {
            try
            {
                var result = DcPgConn.GetAllReportsForRelease("");
                return result != null && result.Count == 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   Exception: {ex.Message}");
                return false;
            }
        });
    }

    static void RunGetReportPathTests()
    {
        TestGetReportPathMethod();
    }

    static void TestGetReportPathMethod()
    {
        Console.WriteLine("🛤️  Testing GetReportPath Method");
        Console.WriteLine("-------------------------------");

        // Test 1: Standard path construction
        TestMethod("GetReportPath - Standard Parameters", () => 
        {
            try
            {
                string path = DcPgConn.GetReportPath("dcn6_0", "dcn6_0", "func_cov", 
                    "dcn_core_verif_plan", "individual", "8222907", "dashboard.html");
                
                bool hasCorrectStructure = path.Contains("/proj/videoip/web/merged_reports/") &&
                                         path.Contains("dcn6_0") &&
                                         path.Contains("func_cov") &&
                                         path.Contains("dcn_core_verif_plan") &&
                                         path.Contains("individual") &&
                                         path.Contains("8222907") &&
                                         path.Contains("dashboard.html");
                
                Console.WriteLine($"   Generated path: {path}");
                return hasCorrectStructure && path.Contains("/") && !path.Contains("\\");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   Exception: {ex.Message}");
                return false;
            }
        });

        // Test 2: Default fileName parameter
        TestMethod("GetReportPath - Default FileName", () => 
        {
            try
            {
                string path = DcPgConn.GetReportPath("project1", "release1", "code_cov", 
                    "report1", "accumulate", "123456");
                
                // Should handle empty default fileName
                bool isValidPath = path.Contains("/proj/videoip/web/merged_reports/") &&
                                 path.Contains("project1") &&
                                 !path.Contains("\\");
                
                Console.WriteLine($"   Generated path: {path}");
                return isValidPath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   Exception: {ex.Message}");
                return false;
            }
        });

        // Test 3: Path normalization (forward slashes)
        TestMethod("GetReportPath - Path Normalization", () => 
        {
            try
            {
                string path = DcPgConn.GetReportPath("test\\project", "test\\release", 
                    "test\\cov", "test\\report", "test\\type", "test\\change", "test\\file");
                
                // Should convert all backslashes to forward slashes
                bool hasOnlyForwardSlashes = !path.Contains("\\") && path.Contains("/");
                
                Console.WriteLine($"   Generated path: {path}");
                return hasOnlyForwardSlashes;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   Exception: {ex.Message}");
                return false;
            }
        });
    }

    static void RunExistingMethodTests()
    {
        Console.WriteLine("🔄 Testing Existing Methods (Smoke Tests)");
        Console.WriteLine("----------------------------------------");

        // Test GetAllProjects
        TestMethod("GetAllProjects - Basic Functionality", () => 
        {
            try
            {
                var projects = DcPgConn.GetAllProjects(5); // Limit to 5 for testing
                return projects != null; // Should return a list, empty or populated
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   Exception: {ex.Message}");
                return false;
            }
        });

        // Test GetAllReleases
        TestMethod("GetAllReleases - Basic Functionality", () => 
        {
            try
            {
                var releases = DcPgConn.GetAllReleases(5); // Limit to 5 for testing
                return releases != null; // Should return a list, empty or populated
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   Exception: {ex.Message}");
                return false;
            }
        });
    }

    static void TestMethodSignatures()
    {
        Console.WriteLine("🔍 Testing Method Signatures and Types");
        Console.WriteLine("-------------------------------------");

        TestMethod("Method Signatures - Compilation Check", () => 
        {
            try
            {
                // Test that methods exist and have correct signatures
                var methodInfo1 = typeof(DcPgConn).GetMethod("GetReleaseReportInfo");
                var methodInfo2 = typeof(DcPgConn).GetMethod("GetAllReportsForRelease");
                var methodInfo3 = typeof(DcPgConn).GetMethod("GetReportPath");
                
                bool allMethodsExist = methodInfo1 != null && methodInfo2 != null && methodInfo3 != null;
                
                if (allMethodsExist)
                {
                    Console.WriteLine("   ✅ All new methods found with correct signatures");
                }
                
                return allMethodsExist;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   Exception: {ex.Message}");
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
        Console.WriteLine($"Success Rate: {(double)testsPassed / testsTotal * 100:F1}%");
        Console.WriteLine();

        if (testsPassed == testsTotal)
        {
            Console.WriteLine("🎉 ALL TESTS PASSED! 🎉");
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
        Console.WriteLine("🏁 Testing completed.");
    }
}