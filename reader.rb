#!/tool/pandora64/.package/ruby-2.7.2-gcc1020-a/bin/ruby

require 'pg'
require 'singleton'
require 'json'

class DcPgConn
    include Singleton

    def initialize
        init_db
        # Time to sleep in case of a db error
        @sleep_time = 60
    end

    def init_db
        db     = "videoip"
        host   = "atldbpgsql07"
        port   = "5432"
        user   = "dcip"
        passwd = "dcip"

        while @db_conn.nil? || @db_conn.status != 0
            @db_conn = PG::Connection.open(dbname: db, host: host, port: port, user: user, password: passwd)
            #@db_conn = PGconn.open(dbname: db, host: host, port: port, user: user, password: passwd)
        end

        @mutex = Mutex.new
        @db_conn
    end

    def close_db
        @db_conn.close unless @db_conn.nil?
        @db_conn = nil;
    end

    def conn
        @db_conn
    end

    def select_first(q, *args)
        ret = []

        # Retry 10 times if some errors occurs, right now, because we haven't been tracking the error types, we
        # will retry for all errors, eventually, we might only retry for specific types of failures
        @mutex.synchronize do
            begin
                retries ||= 0
                @db_conn.async_exec(q, args) do |res|
                    ret = res.values.first
                end
            rescue PG::UndefinedTable
                ret = nil
            rescue PG::UniqueViolation => e
                ret = nil
                raise e # re-raise the exception to be handled by the caller block
            rescue => e
                puts "[ERROR] An error occurred for #{q} #{args}"
                puts "Error is #{e.class}"
                puts e.message

                retries += 1

                # Wait a bit in case server was busy
                sleep(@sleep_time)
                if retries > 5
                  puts "Reconnecting Database"
                  close_db
                  init_db
                end
                retry if retries <= 10
            end
        end

        ret
    end

    def select_all(q, *args)
        ret = []

        # Retry 10 times if some errors occurs, right now, because we haven't been tracking the error types, we
        # will retry for all errors, eventually, we might only retry for specific types of failures
        @mutex.synchronize do
            begin
                retries ||= 0
                @db_conn.async_exec(q, args) do |res|
                    ret = res.values
                end
            rescue PG::UndefinedTable
                ret = nil
            rescue PG::UniqueViolation => e
                ret = nil
                raise e # re-raise the exception to be handled by the caller block
            rescue => e
                puts "[ERROR] An error occurred for #{q} #{args}"
                puts "Error is #{e.class}"
                puts e.message

                retries += 1

                # Wait a bit in case server was busy
                sleep(@sleep_time)
                if retries > 5
                  puts "Reconnecting Database"
                  close_db
                  init_db
                end
                retry if retries <= 10
            end
        end
        ret
    end

    def select_all_raw(q, *args)
        ret = []

        # Retry 10 times if some errors occurs, right now, because we haven't been tracking the error types, we
        # will retry for all errors, eventually, we might only retry for specific types of failures
        @mutex.synchronize do
            begin
                retries ||= 0
                ret = @db_conn.async_exec(q, args)
            rescue PG::UndefinedTable
                ret = nil
            rescue PG::UniqueViolation => e
                ret = nil
                raise e # re-raise the exception to be handled by the caller block
            rescue => e
                puts "[ERROR] An error occurred for #{q} #{args}"
                puts "Error is #{e.class}"
                puts e.message

                retries += 1

                # Wait a bit in case server was busy
                sleep(@sleep_time)
                if retries > 5
                  puts "Reconnecting Database"
                  close_db
                  init_db
                end
                retry if retries <= 10
            end
        end

        ret
    end

    def execute(q, *args)
        # Retry 10 times if some errors occurs, right now, because we haven't been tracking the error types, we
        # will retry for all errors, eventually, we might only retry for specific types of failures
        @mutex.synchronize do
            begin
                retries ||= 0
                @db_conn.async_exec(q, args)
            rescue PG::UniqueViolation => e
                raise e # re-raise the exception to be handled by the caller block
            rescue => e
                puts "[ERROR] An error occurred for #{q} #{args}"
                puts "Error is #{e.class}"
                puts e.message

                retries += 1

                # Wait a bit in case server was busy
                sleep(@sleep_time)
                if retries > 5
                  puts "Reconnecting Database"
                  close_db
                  init_db
                end

                if retries <= 10
                    retry
                else
                    raise e
                end
            end
        end
    end

    # Get report information for a specific release and report name
    # Returns [releaseReportId, projectName] or [nil, nil] if not found
    def get_report_info(release_id, report_name)
        query = <<~QUERY
            SELECT cmr.id, p.project_name FROM rel.coverage_merge_reports cmr
            INNER JOIN project p ON p.project_id = cmr.project_ref
            INNER JOIN info.merge_reports mr ON mr.id = cmr.merge_report_ref
            WHERE release_ref = $1 AND mr.name = $2
        QUERY
        
        result = select_first(query, release_id, report_name)
        return [nil, nil] if result.nil?
        
        [result[0], result[1]]
    end
end


# wsiddele: ACTUAL CODE START


# Change these params to how you see fit
releaseName = "dcn6_0"
reportName = "dcn_core_verif_plan"

# Get release ID from release name
releaseId, = DcPgConn.instance.select_first("SELECT release_id FROM release WHERE release_name = $1", releaseName)

puts "Release ID for '#{releaseName}': #{releaseId}"

# Get the ID of the report for this release (also get project name)
releaseReportId, projectName = DcPgConn.instance.get_report_info(releaseId, reportName)

puts "Report ID for '#{reportName}': #{releaseReportId}"

# Get an individual report by changelist for this report
changelist = "8222907"
reportType = "individual" # or "accumulate"
covType = "func_cov" # or "code_cov"

# Build the path to where the report is stored
# NOTE: This is a symlink to the report directory
reportPath = File.join("/proj/videoip/web/merged_reports/", projectName, releaseName, covType, reportName, reportType, changelist, "dashboard.html")

puts "Report path for changelist #{changelist}: #{reportPath}"




# get all project names
query = <<~QUERY
  SELECT project_id, project_name FROM project
QUERY

allProjects = DcPgConn.instance.select_all_raw(query)

allProjects.each do |row|
    puts "#{row["project_id"]}:  #{row["project_name"]}"
end

# get all release names
query = <<~QUERY
  SELECT release_id, release_name FROM release
QUERY

allReleases = DcPgConn.instance.select_all_raw(query)

allReleases.each do |row|
    puts "#{row["release_id"]}:  #{row["release_name"]}"
end

# get all report names as part of a release
query = <<~QUERY
    SELECT cmr.id, mr.name FROM rel.coverage_merge_reports cmr
    INNER JOIN info.merge_reports mr ON mr.id = cmr.merge_report_ref
    WHERE cmr.release_ref = $1
QUERY

allReports = DcPgConn.instance.select_all_raw(query, releaseId)

reportIdOfInterest = nil
reportNameOfInterest = nil
allReports.each do |row|
    puts "#{row["name"]}"
    # this variable is used in the next query AS AN EXAMPLE
    # for this example, just take the first id we see
    reportIdOfInterest = row["id"] if reportIdOfInterest.nil?
    reportNameOfInterest = row["name"] if reportNameOfInterest.nil?
end

# get all changelists for a given report in a release
# NOTE, the table and column name depends on what type of report it is
tableName = reportType == "individual" ? "code_coverage_merge_individuals" : "code_coverage_merge_accumulates"
columnName = reportType == "individual" ? "changelist" : "end_changelist"

# in this example, we would use the query crom 

query = <<~QUERY
    SELECT DISTINCT #{columnName} AS changelist FROM #{tableName}
    WHERE coverage_merge_report_ref = $1
    ORDER BY #{columnName} DESC
QUERY

allChangelists = DcPgConn.instance.select_all_raw(query, reportIdOfInterest)

puts "All changelists for report '#{reportNameOfInterest}' (ID #{reportIdOfInterest}):"
allChangelists.each do |row|
    puts "#{row["changelist"]}"
end