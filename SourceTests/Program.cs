using System;
using System.Collections.Generic;

/// <summary>
/// Simple test program to display all reports for release "dcn6_0"
/// This program directly references the DatabaseReader source code
/// </summary>
class Program
{
    static void Main()
    {
        Console.WriteLine("🧪 Testing GetAllReportsForRelease for release 'dcn6_0'");
        Console.WriteLine("======================================================");
        Console.WriteLine();

        try
        {
            // Initialize database connection
            Console.WriteLine("📡 Initializing database connection...");
            DcPgConn.InitDb();

            if (DcPgConn.Conn == null)
            {
                Console.WriteLine("❌ Failed to connect to database.");
                Console.WriteLine("💡 Make sure the database server is running and accessible.");
                return;
            }

            Console.WriteLine("✅ Database connection established");
            Console.WriteLine();

            // Call GetAllReportsForRelease with "dcn6_0"
            Console.WriteLine("🔍 Fetching all reports for release 'dcn6_0'...");
            Console.WriteLine();

            var reports = DcPgConn.GetAllReportsForRelease("dcn6_0");

            if (reports == null)
            {
                Console.WriteLine("❌ Query failed - returned null");
                return;
            }

            if (reports.Count == 0)
            {
                Console.WriteLine("📭 No reports found for release 'dcn6_0'");
                Console.WriteLine("💡 This could mean:");
                Console.WriteLine("   - The release doesn't exist");
                Console.WriteLine("   - The release has no reports");
                Console.WriteLine("   - Database connection issues");
                return;
            }

            // Display results
            Console.WriteLine($"✅ Found {reports.Count} report(s) for release 'dcn6_0':");
            Console.WriteLine();
            Console.WriteLine("┌─────┬─────────┬─────────────────────┬─────────────────────────────┐");
            Console.WriteLine("│  #  │ ReportID │ Project Name       │ Report Name                 │");
            Console.WriteLine("├─────┼─────────┼─────────────────────┼─────────────────────────────┤");

            int count = 1;
            foreach (var report in reports)
            {
                string projectName = report.projectName.Length > 19 ? report.projectName.Substring(0, 16) + "..." : report.projectName.PadRight(19);
                string reportName = report.reportName.Length > 27 ? report.reportName.Substring(0, 24) + "..." : report.reportName.PadRight(27);

                Console.WriteLine($"│ {count.ToString().PadLeft(3)} │ {report.reportId.ToString().PadLeft(7)} │ {projectName} │ {reportName} │");
                count++;
            }

            Console.WriteLine("└─────┴─────────┴─────────────────────┴─────────────────────────────┘");
            Console.WriteLine();
            Console.WriteLine($"📊 Summary: {reports.Count} reports found");
            Console.WriteLine($"🏷️  All reports are for Release ID: {reports[0].releaseId}");

            // Show detailed information for first few reports
            if (reports.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine("📋 Detailed Information for First Report:");
                var firstReport = reports[0];
                Console.WriteLine($"   Release ID: {firstReport.releaseId}");
                Console.WriteLine($"   Report ID: {firstReport.reportId}");
                Console.WriteLine($"   Project Name: {firstReport.projectName}");
                Console.WriteLine($"   Report Name: {firstReport.reportName}");
            }

            // Show a few more reports if available
            if (reports.Count > 1)
            {
                Console.WriteLine();
                Console.WriteLine("📋 Additional Reports:");
                for (int i = 1; i < Math.Min(5, reports.Count); i++)
                {
                    var report = reports[i];
                    Console.WriteLine($"   {i + 1}. Report ID: {report.reportId}, Project: {report.projectName}, Name: {report.reportName}");
                }

                if (reports.Count > 5)
                {
                    Console.WriteLine($"   ... and {reports.Count - 5} more reports");
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
        Console.WriteLine("🎯 Test completed!");
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
}