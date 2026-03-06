using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using reviews.Models;

namespace reviews.Services;

/// <summary>
/// Service for handling database migrations from legacy system.
/// Loads SQL scripts from files and executes them in order.
/// </summary>
public class MigrationService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<MigrationService> _logger;
    private readonly string _sqlScriptsPath;

    public MigrationService(
        ApplicationDbContext db,
        ILogger<MigrationService> logger,
        IWebHostEnvironment environment)
    {
        _db = db;
        _logger = logger;
        _sqlScriptsPath = Path.Combine(environment.ContentRootPath, "Migrations", "SQL");
    }

    /// <summary>
    /// Checks if data already exists in the system.
    /// </summary>
    public async Task<bool> HasExistingDataAsync()
    {
        return await _db.Clients.AnyAsync();
    }

    /// <summary>
    /// Checks if the legacy schema exists and is accessible.
    /// </summary>
    public async Task<bool> CheckLegacySchemaExistsAsync()
    {
        try
        {
            await using var connection = _db.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync();
            }

            await using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT 1
                FROM information_schema.tables
                WHERE table_schema = 'legacy'
                  AND table_name = '360_colleagues'
                LIMIT 1";

            var result = await command.ExecuteScalarAsync();
            return result != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for legacy schema");
            return false;
        }
    }

    /// <summary>
    /// Loads all SQL migration scripts from the SQL directory.
    /// Scripts are loaded in alphabetical order (use numeric prefixes for ordering).
    /// </summary>
    public List<MigrationScript> LoadMigrationScripts()
    {
        var scripts = new List<MigrationScript>();

        try
        {
            if (!Directory.Exists(_sqlScriptsPath))
            {
                _logger.LogError("SQL scripts directory not found: {Path}", _sqlScriptsPath);
                return scripts;
            }

            var sqlFiles = Directory.GetFiles(_sqlScriptsPath, "*.sql")
                .OrderBy(f => f)
                .ToList();

            foreach (var filePath in sqlFiles)
            {
                var fileName = Path.GetFileName(filePath);
                var sql = File.ReadAllText(filePath);

                // Extract description from first comment line
                var description = ExtractDescriptionFromSql(sql) ?? fileName;

                scripts.Add(new MigrationScript
                {
                    Name = fileName,
                    Description = description,
                    Sql = sql,
                    FilePath = filePath
                });

                _logger.LogDebug("Loaded migration script: {FileName}", fileName);
            }

            _logger.LogInformation("Loaded {Count} migration scripts", scripts.Count);
            return scripts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading migration scripts from {Path}", _sqlScriptsPath);
            return scripts;
        }
    }

    /// <summary>
    /// Executes all migration scripts within a transaction.
    /// Returns a list of results for each script.
    /// </summary>
    public async Task<List<MigrationResult>> ExecuteMigrationsAsync()
    {
        var results = new List<MigrationResult>();
        var scripts = LoadMigrationScripts();

        if (scripts.Count == 0)
        {
            _logger.LogWarning("No migration scripts found");
            return results;
        }

        await using var transaction = await _db.Database.BeginTransactionAsync();

        try
        {
            foreach (var script in scripts)
            {
                var startTime = DateTime.UtcNow;

                try
                {
                    _logger.LogInformation("Executing migration: {Name}", script.Name);
                    
                    await _db.Database.ExecuteSqlRawAsync(script.Sql);
                    
                    var duration = DateTime.UtcNow - startTime;
                    
                    results.Add(new MigrationResult
                    {
                        Name = script.Name,
                        Description = script.Description,
                        Status = "Success",
                        Duration = duration
                    });

                    _logger.LogInformation("Migration succeeded: {Name} in {Duration}ms", 
                        script.Name, duration.TotalMilliseconds);
                }
                catch (Exception ex)
                {
                    var duration = DateTime.UtcNow - startTime;

                    results.Add(new MigrationResult
                    {
                        Name = script.Name,
                        Description = script.Description,
                        Status = "Failed",
                        Error = ex.Message,
                        Duration = duration
                    });

                    _logger.LogError(ex, "Migration failed: {Name}", script.Name);
                    throw; // Rollback transaction
                }
            }

            await transaction.CommitAsync();
            _logger.LogInformation("All migrations completed successfully");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Migration transaction rolled back");
            throw;
        }

        return results;
    }

    /// <summary>
    /// Validates SQL scripts without executing them.
    /// Useful for checking syntax before running migrations.
    /// </summary>
    public List<MigrationValidation> ValidateMigrationScripts()
    {
        var validations = new List<MigrationValidation>();
        var scripts = LoadMigrationScripts();

        foreach (var script in scripts)
        {
            var validation = new MigrationValidation
            {
                Name = script.Name,
                IsValid = true,
                Warnings = new List<string>()
            };

            // Check for common issues
            if (string.IsNullOrWhiteSpace(script.Sql))
            {
                validation.IsValid = false;
                validation.Warnings.Add("Script is empty");
            }

            if (!script.Sql.Contains("INSERT", StringComparison.OrdinalIgnoreCase) &&
                !script.Sql.Contains("UPDATE", StringComparison.OrdinalIgnoreCase) &&
                !script.Sql.Contains("DELETE", StringComparison.OrdinalIgnoreCase))
            {
                validation.Warnings.Add("Script contains no DML statements");
            }

            // Check for potentially unsafe operations
            if (script.Sql.Contains("DROP", StringComparison.OrdinalIgnoreCase))
            {
                validation.Warnings.Add("WARNING: Script contains DROP statement");
            }

            if (script.Sql.Contains("TRUNCATE", StringComparison.OrdinalIgnoreCase))
            {
                validation.Warnings.Add("WARNING: Script contains TRUNCATE statement");
            }

            validations.Add(validation);
        }

        return validations;
    }

    private string? ExtractDescriptionFromSql(string sql)
    {
        // Extract first comment line as description
        var lines = sql.Split('\n');
        var firstLine = lines.FirstOrDefault(l => l.Trim().StartsWith("--"));
        
        if (firstLine != null)
        {
            return firstLine.Replace("--", "").Trim();
        }

        return null;
    }
}

public class MigrationScript
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Sql { get; set; } = "";
    public string FilePath { get; set; } = "";
}

public class MigrationResult
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Status { get; set; } = "";
    public string? Error { get; set; }
    public TimeSpan Duration { get; set; }
}

public class MigrationValidation
{
    public string Name { get; set; } = "";
    public bool IsValid { get; set; }
    public List<string> Warnings { get; set; } = new();
}
