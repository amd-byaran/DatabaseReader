using System;
using System.IO;

class Program
{
    static void Main(string[] args)
    {
        // Change these params to how you see fit
        string releaseName = "dcn6_0";
        string reportName = "dcn_core_verif_plan";

        var info = DcPgConn.GetReportInfo(releaseName, reportName);
        if (info == null)
        {
            Console.WriteLine("No data found");
            return;
        }

        int releaseId = info.Value.releaseId;
        int releaseReportId = info.Value.reportId;
        string projectName = info.Value.projectName;

        Console.WriteLine($"Release ID for '{releaseName}': {releaseId}");
        Console.WriteLine($"Report ID for '{reportName}': {releaseReportId}");

        // Get an individual report by changelist for this report
        string changelist = "8222907";
        string reportType = "individual"; // or "accumulate"
        string covType = "func_cov"; // or "code_cov"

        // Build the path to where the report is stored
        // NOTE: This is a symlink to the report directory
        string reportPath = DcPgConn.GetReportPath(projectName, releaseName, covType, reportName, reportType, changelist);

        Console.WriteLine($"Report path for changelist {changelist}: {reportPath}");

        // Example of using GetReportPath with different file name
        string customReportPath = DcPgConn.GetReportPath(projectName, releaseName, covType, reportName, reportType, changelist, "index.html");
        Console.WriteLine($"Custom report path: {customReportPath}");

        // Get all projects (limited to 5 most recent)
        var projects = DcPgConn.GetAllProjects(5);
        Console.WriteLine("\nLast 5 Projects (most recent):");
        foreach (var proj in projects)
        {
            Console.WriteLine($"{proj.ProjectId}: {proj.ProjectName}");
        }

        // Get all releases (limited to 5 most recent)
        var releases = DcPgConn.GetAllReleases(5);
        Console.WriteLine("\nLast 5 Releases (most recent):");
        foreach (var rel in releases)
        {
            Console.WriteLine($"{rel.ReleaseId}: {rel.ReleaseName}");
        }

        // Get all reports for the release
        var reports = DcPgConn.GetReportsForRelease(releaseId);
        Console.WriteLine($"\nAll Reports for release '{releaseName}' (ID {releaseId}):");
        foreach (var rep in reports)
        {
            Console.WriteLine($"{rep.ReportId}: {rep.ReportName}");
        }

        // Get changelists for the first report (limited to 5 most recent)
        if (reports.Any())
        {
            var firstReport = reports.First();
            var changelists = DcPgConn.GetChangelistsForReport(firstReport.ReportId, reportType, 5);
            Console.WriteLine($"\nLast 5 Changelists for report '{firstReport.ReportName}' (ID {firstReport.ReportId}):");
            foreach (var cl in changelists)
            {
                Console.WriteLine(cl);
            }
        }
    }
}