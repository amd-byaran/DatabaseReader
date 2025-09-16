// DatabaseReader DLL Usage Example
// This shows how to reference and use the DatabaseReader.dll assembly

using System;
using DatabaseReader; // Reference to your DLL

class Program
{
    static void Main(string[] args)
    {
        // Example usage of the DatabaseReader assembly
        try
        {
            // Get all projects
            var projects = DcPgConn.GetAllProjects();
            Console.WriteLine($"Found {projects.Count} projects");

            // Get all releases
            var releases = DcPgConn.GetAllReleases();
            Console.WriteLine($"Found {releases.Count} releases");

            // Get reports for a specific release (example with ID 1)
            var reports = DcPgConn.GetReportsForRelease(1);
            Console.WriteLine($"Found {reports.Count} reports for release 1");

            // Get report info
            var reportInfo = DcPgConn.GetReportInfo(1);
            if (reportInfo != null)
            {
                Console.WriteLine($"Report 1: {reportInfo.Name}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}