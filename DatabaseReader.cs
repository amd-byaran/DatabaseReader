using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using Npgsql;
using System.Linq;

/**
 * DatabaseReader.cs
 *
 * This file contains C# classes and methods for reading data from a PostgreSQL database
 * containing coverage analysis information. It provides a clean interface for querying
 * projects, releases, reports, and changelists with proper error handling and retry logic.
 *
 * Key Features:
 * - Singleton-like database connection management
 * - Automatic retry logic for transient database errors
 * - Type-safe data structures for query results
 * - Configurable result limiting for performance
 * - Comprehensive error handling and logging
 *
 * Database Schema Assumptions:
 * - PostgreSQL database with tables: project, release, rel.coverage_merge_reports, info.merge_reports
 * - Tables may have creation timestamp columns (created_at) for sorting
 * - Changelist tables: code_coverage_merge_individuals, code_coverage_merge_accumulates
 *
 * Usage:
 * - Call static methods on DcPgConn class for database operations
 * - Use the provided data types (Project, Release, Report) for type safety
 * - Handle potential null returns for failed queries
 *
 * USAGE EXAMPLES:
 *
 * // 1. Get comprehensive report information
 * var reportInfo = DcPgConn.GetReportInfo("dcn6_0", "dcn_core_verif_plan");
 * if (reportInfo.HasValue)
 * {
 *     int releaseId = reportInfo.Value.releaseId;
 *     int reportId = reportInfo.Value.reportId;
 *     string projectName = reportInfo.Value.projectName;
 *     Console.WriteLine($"Found report {reportId} for release {releaseId} in project {projectName}");
 * }
 *
 * // 2. Get all projects (limited to most recent 10)
 * var recentProjects = DcPgConn.GetAllProjects(10);
 * foreach (var project in recentProjects)
 * {
 *     Console.WriteLine($"Project {project.ProjectId}: {project.ProjectName}");
 * }
 *
 * // 3. Get all releases (unlimited)
 * var allReleases = DcPgConn.GetAllReleases();
 * Console.WriteLine($"Found {allReleases.Count} releases");
 *
 * // 4. Get reports for a specific release
 * var reports = DcPgConn.GetReportsForRelease(325); // release ID
 * foreach (var report in reports)
 * {
 *     Console.WriteLine($"Report {report.ReportId}: {report.ReportName}");
 * }
 *
 * // 5. Get changelists for a report (limited to most recent 5)
 * var changelists = DcPgConn.GetChangelistsForReport(16076, "individual", 5);
 * foreach (var changelist in changelists)
 * {
 *     Console.WriteLine($"Changelist: {changelist}");
 * }
 *
 * // 6. Construct report file paths
 * string dashboardPath = DcPgConn.GetReportPath("dcn6_0", "dcn6_0", "func_cov",
 *                                               "dcn_core_verif_plan", "individual", "8222907");
 * string customPath = DcPgConn.GetReportPath("dcn6_0", "dcn6_0", "func_cov",
 *                                           "dcn_core_verif_plan", "individual", "8222907", "index.html");
 * Console.WriteLine($"Dashboard: {dashboardPath}");
 * Console.WriteLine($"Custom file: {customPath}");
 *
 * // 7. Error handling example
 * try
 * {
 *     var projects = DcPgConn.GetAllProjects();
 *     if (projects.Count == 0)
 *     {
 *         Console.WriteLine("No projects found");
 *     }
 * }
 * catch (Exception ex)
 * {
 *     Console.WriteLine($"Database error: {ex.Message}");
 * }
 */

/// <summary>
/// Represents a project in the database
/// </summary>
public record Project(int ProjectId, string ProjectName);

/// <summary>
/// Represents a release in the database
/// </summary>
public record Release(int ReleaseId, string ReleaseName);

/// <summary>
/// Represents a report in the database
/// </summary>
public record Report(int ReportId, string ReportName);

/// <summary>
/// Main database connection and query class for coverage analysis database.
/// Provides static methods for querying various database entities with proper error handling.
/// </summary>
public static class DcPgConn
{
    private static NpgsqlConnection? _dbConn;
    private static readonly object _mutex = new object();
    private static readonly int _sleepTime = 60;

    /// <summary>
    /// Static constructor - initializes the database connection when the class is first accessed
    /// </summary>
    static DcPgConn()
    {
        InitDb();
    }

    /// <summary>
    /// Initializes or re-initializes the database connection.
    /// Uses hardcoded connection parameters for the coverage analysis database.
    /// Includes error handling for connection failures.
    /// </summary>
    public static void InitDb()
    {
        // Database connection parameters - hardcoded for coverage analysis system
        string db = "videoip";
        string host = "atldbpgsql07";
        string port = "5432";
        string user = "dcip";
        string passwd = "dcip";

        string connString = $"Host={host};Port={port};Database={db};Username={user};Password={passwd};";

        lock (_mutex)
        {
            _dbConn = new NpgsqlConnection(connString);
            try
            {
                _dbConn.Open();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to open connection: {ex.Message}");
                _dbConn = null;
            }
        }
    }

    /// <summary>
    /// Closes the current database connection and sets it to null.
    /// Thread-safe operation using mutex lock.
    /// </summary>
    public static void CloseDb()
    {
        lock (_mutex)
        {
            if (_dbConn != null)
            {
                _dbConn.Close();
                _dbConn = null;
            }
        }
    }

    /// <summary>
    /// Gets the current database connection.
    /// Returns null if connection is not established.
    /// </summary>
    public static NpgsqlConnection? Conn => _dbConn;

    /// <summary>
    /// Executes a SELECT query and returns the first row of results.
    /// Includes comprehensive retry logic for handling transient database errors.
    ///
    /// Parameters:
    /// - q: SQL query string with positional parameters ($1, $2, etc.)
    /// - args: Variable number of arguments to substitute for query parameters
    ///
    /// Returns:
    /// - object[]: First row of results as an array of objects
    /// - null: If no results found or query fails
    ///
    /// Retry Logic:
    /// - Retries up to 10 times for general exceptions
    /// - Waits 60 seconds between retries
    /// - Reconnects to database after 5 failed attempts
    /// - Handles specific PostgreSQL errors (UndefinedTable, UniqueViolation)
    /// </summary>
    public static object[]? SelectFirst(string q, params object[] args)
    {
        object[]? ret = null;

        lock (_mutex)
        {
            // Ensure connection is available
            if (_dbConn == null || _dbConn.State != ConnectionState.Open)
            {
                InitDb();
                if (_dbConn == null) return null;
            }

            int retries = 0;
            while (true)
            {
                try
                {
                    using (var cmd = new NpgsqlCommand(q, _dbConn))
                    {
                        // Add parameters with proper positional indexing ($1, $2, etc.)
                        for (int i = 0; i < args.Length; i++)
                        {
                            cmd.Parameters.Add(new NpgsqlParameter { Value = args[i] });
                        }

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                ret = new object[reader.FieldCount];
                                reader.GetValues(ret);
                            }
                        }
                    }
                    break; // Success - exit retry loop
                }
                catch (PostgresException ex) when (ex.SqlState == "42P01") // UndefinedTable
                {
                    // Table doesn't exist - no retry needed
                    ret = null;
                    break;
                }
                catch (PostgresException ex) when (ex.SqlState == "23505") // UniqueViolation
                {
                    // Unique constraint violation - re-raise to caller
                    ret = null;
                    throw;
                }
                catch (Exception ex)
                {
                    // General error - log and retry
                    Console.WriteLine($"[ERROR] An error occurred for {q} {string.Join(", ", args)}");
                    Console.WriteLine($"Error is {ex.GetType()}");
                    Console.WriteLine(ex.Message);

                    retries++;
                    Thread.Sleep(_sleepTime * 1000); // Wait before retry

                    if (retries > 5)
                    {
                        // After 5 retries, try reconnecting
                        Console.WriteLine("Reconnecting Database");
                        CloseDb();
                        InitDb();
                        if (_dbConn == null) throw new Exception("Cannot reconnect to database");
                    }

                    if (retries > 10)
                    {
                        // Max retries exceeded - re-raise exception
                        throw;
                    }
                }
            }
        }

        return ret;
    }

    /// <summary>
    /// Executes a SELECT query and returns all rows of results.
    /// Includes the same retry logic as SelectFirst for robustness.
    ///
    /// Parameters:
    /// - q: SQL query string with positional parameters ($1, $2, etc.)
    /// - args: Variable number of arguments to substitute for query parameters
    ///
    /// Returns:
    /// - List<object[]>: All rows of results, each as an array of objects
    /// - null: If query fails with UndefinedTable error
    /// - Empty list: If no results found
    ///
    /// Note: Same retry logic and error handling as SelectFirst method
    /// </summary>
    public static List<object[]>? SelectAll(string q, params object[] args)
    {
        var ret = new List<object[]>();

        lock (_mutex)
        {
            if (_dbConn == null || _dbConn.State != ConnectionState.Open)
            {
                InitDb();
                if (_dbConn == null) return null;
            }

            int retries = 0;
            while (true)
            {
                try
                {
                    using (var cmd = new NpgsqlCommand(q, _dbConn))
                    {
                        for (int i = 0; i < args.Length; i++)
                        {
                            cmd.Parameters.Add(new NpgsqlParameter { Value = args[i] });
                        }
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var row = new object[reader.FieldCount];
                                reader.GetValues(row);
                                ret.Add(row);
                            }
                        }
                    }
                    break;
                }
                catch (PostgresException ex) when (ex.SqlState == "42P01") // UndefinedTable
                {
                    ret = null;
                    break;
                }
                catch (PostgresException ex) when (ex.SqlState == "23505") // UniqueViolation
                {
                    ret = null;
                    throw;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] An error occurred for {q} {string.Join(", ", args)}");
                    Console.WriteLine($"Error is {ex.GetType()}");
                    Console.WriteLine(ex.Message);

                    retries++;
                    Thread.Sleep(_sleepTime * 1000);
                    if (retries > 5)
                    {
                        Console.WriteLine("Reconnecting Database");
                        CloseDb();
                        InitDb();
                        if (_dbConn == null) throw new Exception("Cannot reconnect to database");
                    }
                    if (retries > 10)
                    {
                        throw;
                    }
                }
            }
        }

        return ret;
    }

    // Similar for SelectAllRaw and Execute, but since not used in main, can add if needed

    /// <summary>
    /// Retrieves comprehensive report information for a given release and report name.
    /// This is a high-level convenience method that combines multiple database queries.
    ///
    /// Parameters:
    /// - releaseName: Name of the release (e.g., "dcn6_0")
    /// - reportName: Name of the report (e.g., "dcn_core_verif_plan")
    ///
    /// Returns:
    /// - Tuple containing: (releaseId, reportId, projectName)
    /// - null: If release or report is not found
    ///
    /// Queries performed:
    /// 1. Get release ID from release name
    /// 2. Get report ID and project name for the release
    /// </summary>
    public static (int releaseId, int reportId, string projectName)? GetReportInfo(string releaseName, string reportName)
    {
        var result = SelectFirst("SELECT release_id FROM release WHERE release_name = $1", releaseName);
        if (result == null) return null;
        int releaseId = Convert.ToInt32(result[0]);

        string query = @"
            SELECT cmr.id, p.project_name FROM rel.coverage_merge_reports cmr
            INNER JOIN project p ON p.project_id = cmr.project_ref
            INNER JOIN info.merge_reports mr ON mr.id = cmr.merge_report_ref
            WHERE release_ref = $1 AND mr.name = $2
        ";

        var result2 = SelectFirst(query, releaseId, reportName);
        if (result2 == null) return null;
        int reportId = Convert.ToInt32(result2[0]);
        string projectName = (string)result2[1];

        return (releaseId, reportId, projectName);
    }

    /// <summary>
    /// Gets the release report ID and project name for a given release ID and report name.
    /// This method directly corresponds to the Ruby code query for getting report information.
    ///
    /// Parameters:
    /// - releaseId: The ID of the release
    /// - reportName: Name of the report (e.g., "dcn_core_verif_plan")
    ///
    /// Returns:
    /// - Tuple containing: (releaseReportId, projectName)
    /// - null: If report is not found for the given release
    ///
    /// Query performed:
    /// SELECT cmr.id, p.project_name FROM rel.coverage_merge_reports cmr
    /// INNER JOIN project p ON p.project_id = cmr.project_ref
    /// INNER JOIN info.merge_reports mr ON mr.id = cmr.merge_report_ref
    /// WHERE release_ref = $1 AND mr.name = $2
    /// </summary>
    public static (int releaseReportId, string projectName)? GetReleaseReportInfo(int releaseId, string reportName)
    {
        string query = @"
            SELECT cmr.id, p.project_name FROM rel.coverage_merge_reports cmr
            INNER JOIN project p ON p.project_id = cmr.project_ref
            INNER JOIN info.merge_reports mr ON mr.id = cmr.merge_report_ref
            WHERE release_ref = $1 AND mr.name = $2
        ";

        var result = SelectFirst(query, releaseId, reportName);
        if (result == null) return null;
        
        int releaseReportId = Convert.ToInt32(result[0]);
        string projectName = (string)result[1];

        return (releaseReportId, projectName);
    }

    /// <summary>
    /// Retrieves all projects from the database, sorted by creation time (ID DESC as proxy).
    /// Optionally limits the number of results for performance.
    ///
    /// Parameters:
    /// - limit: Optional maximum number of projects to return (null = all)
    ///
    /// Returns:
    /// - List<Project>: All projects or limited subset, sorted by ID descending
    ///
    /// Notes:
    /// - Uses project_id DESC for sorting (higher IDs = more recent)
    /// - If database has 'created_at' column, replace 'project_id DESC' with 'created_at DESC'
    /// - Returns empty list if no projects found or query fails
    /// </summary>
    public static List<Project> GetAllProjects(int? limit = null)
    {
        // Note: Replace 'project_id DESC' with 'created_at DESC' if your table has a creation timestamp column
        string query = "SELECT project_id, project_name FROM project ORDER BY project_id DESC";
        if (limit.HasValue)
        {
            query += " LIMIT $1";
        }
        var results = SelectAll(query, limit.HasValue ? new object[] { limit.Value } : Array.Empty<object>());
        if (results == null) return new List<Project>();
        return results.Select(row => new Project(Convert.ToInt32(row[0]), (string)row[1])).ToList();
    }

    /// <summary>
    /// Retrieves all releases from the database, sorted by creation time (ID DESC as proxy).
    /// Optionally limits the number of results for performance.
    ///
    /// Parameters:
    /// - limit: Optional maximum number of releases to return (null = all)
    ///
    /// Returns:
    /// - List<Release>: All releases or limited subset, sorted by ID descending
    ///
    /// Notes:
    /// - Uses release_id DESC for sorting (higher IDs = more recent)
    /// - If database has 'created_at' column, replace 'release_id DESC' with 'created_at DESC'
    /// - Returns empty list if no releases found or query fails
    /// </summary>
    public static List<Release> GetAllReleases(int? limit = null)
    {
        // Note: Replace 'release_id DESC' with 'created_at DESC' if your table has a creation timestamp column
        string query = "SELECT release_id, release_name FROM release ORDER BY release_id DESC";
        if (limit.HasValue)
        {
            query += " LIMIT $1";
        }
        var results = SelectAll(query, limit.HasValue ? new object[] { limit.Value } : Array.Empty<object>());
        if (results == null) return new List<Release>();
        return results.Select(row => new Release(Convert.ToInt32(row[0]), (string)row[1])).ToList();
    }

    /// <summary>
    /// Retrieves all reports for a specific release, sorted by creation time (ID DESC as proxy).
    ///
    /// Parameters:
    /// - releaseId: The ID of the release to get reports for
    ///
    /// Returns:
    /// - List<Report>: All reports for the specified release, sorted by ID descending
    ///
    /// Notes:
    /// - Uses cmr.id DESC for sorting (higher IDs = more recent reports)
    /// - If database has 'created_at' column in rel.coverage_merge_reports,
    ///   replace 'cmr.id DESC' with 'cmr.created_at DESC'
    /// - Returns empty list if no reports found or query fails
    /// </summary>
    public static List<Report> GetReportsForRelease(int releaseId)
    {
        string query = @"
            SELECT cmr.id, mr.name FROM rel.coverage_merge_reports cmr
            INNER JOIN info.merge_reports mr ON mr.id = cmr.merge_report_ref
            WHERE cmr.release_ref = $1
            ORDER BY cmr.id DESC";  // Replace with 'cmr.created_at DESC' if available
        var results = SelectAll(query, releaseId);
        if (results == null) return new List<Report>();
        return results.Select(row => new Report(Convert.ToInt32(row[0]), (string)row[1])).ToList();
    }

    /// <summary>
    /// Retrieves all changelists for a specific report, sorted by changelist number (chronological).
    /// Optionally limits the number of results for performance.
    ///
    /// Parameters:
    /// - reportId: The ID of the report to get changelists for
    /// - reportType: Type of report ("individual" or "accumulate") - determines which table to query
    /// - limit: Optional maximum number of changelists to return (null = all)
    ///
    /// Returns:
    /// - List<string>: All changelist numbers for the report, sorted descending
    ///
    /// Notes:
    /// - Changelists are naturally chronological (higher numbers = more recent)
    /// - Uses different tables based on report type:
    ///   * "individual" -> code_coverage_merge_individuals.changelist
    ///   * "accumulate" -> code_coverage_merge_accumulates.end_changelist
    /// - If database has 'created_at' columns, can add to ORDER BY clause
    /// - Returns empty list if no changelists found or query fails
    /// </summary>
    public static List<string> GetChangelistsForReport(int reportId, string reportType, int? limit = null)
    {
        string tableName = reportType == "individual" ? "code_coverage_merge_individuals" : "code_coverage_merge_accumulates";
        string columnName = reportType == "individual" ? "changelist" : "end_changelist";

        // Note: Replace with 'ORDER BY created_at DESC, changelist DESC' if created_at column exists
        string query = $@"
            SELECT DISTINCT {columnName} AS changelist FROM {tableName}
            WHERE coverage_merge_report_ref = $1
            ORDER BY {columnName} DESC";
        if (limit.HasValue)
        {
            query += " LIMIT $2";
        }
        var results = SelectAll(query, limit.HasValue ? new object[] { reportId, limit.Value } : new object[] { reportId });
        if (results == null) return new List<string>();
        return results.Select(row => row[0].ToString()).ToList();
    }

    /// <summary>
    /// Constructs a report file path based on the provided parameters.
    /// This creates paths compatible with the coverage analysis system's directory structure.
    ///
    /// Parameters:
    /// - projectName: Name of the project (e.g., "dcn6_0")
    /// - releaseName: Name of the release (e.g., "dcn6_0")
    /// - covType: Coverage type (e.g., "func_cov", "code_cov")
    /// - reportName: Name of the report (e.g., "dcn_core_verif_plan")
    /// - reportType: Type of report (e.g., "individual", "accumulate")
    /// - changelist: Changelist number (e.g., "8222907")
    /// - fileName: Name of the file (defaults to "dashboard.html")
    ///
    /// Returns:
    /// - string: Complete file path with forward slashes
    ///
    /// Path Structure:
    /// /proj/videoip/web/merged_reports/{projectName}/{releaseName}/{covType}/{reportName}/{reportType}/{changelist}/{fileName}
    ///
    /// Notes:
    /// - Always uses forward slashes (/) regardless of platform
    /// - Path points to symlinked report directory in the coverage system
    /// - Default file is "dashboard.html" but can be customized
    /// </summary>
    public static string GetReportPath(string projectName, string releaseName, string covType, string reportName, string reportType, string changelist, string fileName = "")
    {
        string path = Path.Combine("/proj/videoip/web/merged_reports/", projectName, releaseName, covType, reportName, reportType, changelist, fileName);
        return path.Replace("\\", "/");
    }
}