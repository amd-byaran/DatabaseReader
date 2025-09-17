using System;
using System.Collections.Generic;
using System.Reflection;

/// <summary>
/// DLL-based test program to display all reports for release "dcn6_0"
/// This test loads the DatabaseReader.dll and calls GetAllReportsForRelease
/// </summary>
class DLLTest
{
    static void Main()
    {
        Console.WriteLine("🧪 DLL-Based Test: GetAllReportsForRelease for release 'dcn6_0'");
        Console.WriteLine("============================================================");
        Console.WriteLine();

        try
        {
            // Load the DatabaseReader.dll
            string dllPath = @"C:\Users\byaran\OneDrive - Advanced Micro Devices Inc\programming\coverage_analyzer\DatabaseReader\bin\Release\net8.0\DatabaseReader.dll";
            Console.WriteLine($"📦 Loading DLL from: {dllPath}");

            var assembly = Assembly.LoadFrom(dllPath);
            Console.WriteLine($"✅ Assembly loaded: {assembly.FullName}");

            // Get the DcPgConn type
            var dcPgConnType = assembly.GetType("DcPgConn");
            if (dcPgConnType == null)
            {
                Console.WriteLine("❌ DcPgConn class not found in DLL");
                return;
            }
            Console.WriteLine("✅ DcPgConn class found");

            // Get the GetAllReportsForRelease method
            var method = dcPgConnType.GetMethod("GetAllReportsForRelease", BindingFlags.Public | BindingFlags.Static);
            if (method == null)
            {
                Console.WriteLine("❌ GetAllReportsForRelease method not found");
                return;
            }
            Console.WriteLine("✅ GetAllReportsForRelease method found");

            // Initialize database connection
            Console.WriteLine();
            Console.WriteLine("📡 Initializing database connection...");
            var initMethod = dcPgConnType.GetMethod("InitDb", BindingFlags.Public | BindingFlags.Static);
            if (initMethod != null)
            {
                initMethod.Invoke(null, null);
                Console.WriteLine("✅ Database initialization called");
            }

            // Call GetAllReportsForRelease with "dcn6_0"
            Console.WriteLine();
            Console.WriteLine("🔍 Fetching all reports for release 'dcn6_0' using DLL...");

            var result = method.Invoke(null, new object[] { "dcn6_0" });

            if (result == null)
            {
                Console.WriteLine("❌ Query failed - returned null");
                return;
            }

            // Cast the result to the expected type
            var reports = result as IEnumerable<object>;
            if (reports == null)
            {
                Console.WriteLine("❌ Result is not a collection");
                return;
            }

            // Convert to list to count items
            var reportsList = new List<object>();
            foreach (var report in reports)
            {
                reportsList.Add(report);
            }

            if (reportsList.Count == 0)
            {
                Console.WriteLine("📭 No reports found for release 'dcn6_0'");
                Console.WriteLine("💡 This could mean:");
                Console.WriteLine("   - The release doesn't exist");
                Console.WriteLine("   - The release has no reports");
                Console.WriteLine("   - Database connection issues");
                return;
            }

            // Display results
            Console.WriteLine($"✅ Found {reportsList.Count} report(s) for release 'dcn6_0':");
            Console.WriteLine();
            Console.WriteLine("┌─────┬─────────┬─────────────────────┬─────────────────────────────┐");
            Console.WriteLine("│  #  │ ReportID │ Project Name       │ Report Name                 │");
            Console.WriteLine("├─────┼─────────┼─────────────────────┼─────────────────────────────┤");

            int count = 1;
            foreach (var report in reportsList)
            {
                // Get the tuple values using reflection
                var tupleType = report.GetType();
                var item1 = tupleType.GetProperty("Item1")?.GetValue(report); // releaseId
                var item2 = tupleType.GetProperty("Item2")?.GetValue(report); // reportId
                var item3 = tupleType.GetProperty("Item3")?.GetValue(report); // projectName
                var item4 = tupleType.GetProperty("Item4")?.GetValue(report); // reportName

                string projectName = item3?.ToString() ?? "";
                string reportName = item4?.ToString() ?? "";

                projectName = projectName.Length > 19 ? projectName.Substring(0, 16) + "..." : projectName.PadRight(19);
                reportName = reportName.Length > 27 ? reportName.Substring(0, 24) + "..." : reportName.PadRight(27);

                Console.WriteLine($"│ {count.ToString().PadLeft(3)} │ {item2?.ToString()?.PadLeft(7) ?? "N/A"} │ {projectName} │ {reportName} │");
                count++;
            }

            Console.WriteLine("└─────┴─────────┴─────────────────────┴─────────────────────────────┘");
            Console.WriteLine();
            Console.WriteLine($"📊 Summary: {reportsList.Count} reports found");
            Console.WriteLine($"🏷️  All reports are for Release ID: {reportsList[0].GetType().GetProperty("Item1")?.GetValue(reportsList[0])}");

            // Show detailed information for first report
            if (reportsList.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine("📋 Detailed Information for First Report:");
                var firstReport = reportsList[0];
                var tupleType = firstReport.GetType();
                var item1 = tupleType.GetProperty("Item1")?.GetValue(firstReport);
                var item2 = tupleType.GetProperty("Item2")?.GetValue(firstReport);
                var item3 = tupleType.GetProperty("Item3")?.GetValue(firstReport);
                var item4 = tupleType.GetProperty("Item4")?.GetValue(firstReport);

                Console.WriteLine($"   Release ID: {item1}");
                Console.WriteLine($"   Report ID: {item2}");
                Console.WriteLine($"   Project Name: {item3}");
                Console.WriteLine($"   Report Name: {item4}");
            }

            // Show a few more reports if available
            if (reportsList.Count > 1)
            {
                Console.WriteLine();
                Console.WriteLine("📋 Additional Reports:");
                for (int i = 1; i < Math.Min(5, reportsList.Count); i++)
                {
                    var report = reportsList[i];
                    var tupleType = report.GetType();
                    var item2 = tupleType.GetProperty("Item2")?.GetValue(report);
                    var item3 = tupleType.GetProperty("Item3")?.GetValue(report);
                    var item4 = tupleType.GetProperty("Item4")?.GetValue(report);

                    Console.WriteLine($"   {i + 1}. Report ID: {item2}, Project: {item3}, Name: {item4}");
                }

                if (reportsList.Count > 5)
                {
                    Console.WriteLine($"   ... and {reportsList.Count - 5} more reports");
                }
            }

        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Test failed with exception: {ex.Message}");
            Console.WriteLine($"🔍 Exception Type: {ex.GetType().Name}");

            if (ex.InnerException != null)
            {
                Console.WriteLine($"🔍 Inner Exception: {ex.InnerException.Message}");
            }
        }

        Console.WriteLine();
        Console.WriteLine("🎯 DLL-based test completed!");
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
}