using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using reviews.Models;
using reviews.Services;
using reviews.Constants;

namespace reviews.Controllers.MigrationController;

/// <summary>
/// One-time migration controller for initial system setup.
/// Should be disabled in production via configuration.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class MigrationController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly MigrationService _migrationService;
    private readonly ILogger<MigrationController> _logger;
    private readonly IConfiguration _configuration;

    public MigrationController(
        ApplicationDbContext context,
        RoleManager<IdentityRole> roleManager,
        UserManager<ApplicationUser> userManager,
        MigrationService migrationService,
        ILogger<MigrationController> logger,
        IConfiguration configuration)
    {
        _context = context;
        _roleManager = roleManager;
        _userManager = userManager;
        _migrationService = migrationService;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Seeds initial roles and admin users. Should only run once during initial setup.
    /// Requires MigrationsEnabled=true in configuration.
    /// </summary>
    [HttpGet("seed-roles")]
    //[Authorize(Roles = "Admin")] // Require existing admin or remove after first run
    // To allow initial setup without existing admins, we comment out the Authorize attribute.
    public async Task<IActionResult> SeedRolesAndAdmins()
    {
        if (!IsMigrationEnabled())
        {
            _logger.LogWarning("Migration attempted but MigrationsEnabled is false");
            return BadRequest("Migrations are disabled in configuration.");
        }

        try
        {
            // Define roles
            string[] roles = { "Admin", "Client", "Participant" };

            // Create roles if they don't exist
            foreach (var role in roles)
            {
                if (!await _roleManager.RoleExistsAsync(role))
                {
                    var result = await _roleManager.CreateAsync(new IdentityRole(role));
                    if (result.Succeeded)
                    {
                        _logger.LogInformation("Created role: {Role}", role);
                    }
                    else
                    {
                        _logger.LogError("Failed to create role {Role}: {Errors}", 
                            role, string.Join(", ", result.Errors));
                    }
                }
            }

            // Get admin credentials from configuration (NOT hardcoded)
            var adminEmails = _configuration.GetSection("Migration:AdminEmails").Get<string[]>();
            var adminPassword = _configuration["Migration:AdminPassword"];

            if (adminEmails == null || string.IsNullOrEmpty(adminPassword))
            {
                _logger.LogError("Admin configuration missing in appsettings");
                return BadRequest("Admin configuration not found. Check appsettings.json Migration section.");
            }

            // Create admin users
            foreach (var email in adminEmails)
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    user = new ApplicationUser
                    {
                        UserName = email,
                        Email = email,
                        EmailConfirmed = true
                    };

                    var result = await _userManager.CreateAsync(user, adminPassword);
                    if (result.Succeeded)
                    {
                        await _userManager.AddToRoleAsync(user, "Admin");
                        _logger.LogInformation("Created admin user: {Email}", email);
                    }
                    else
                    {
                        _logger.LogError("Failed to create admin user {Email}: {Errors}", 
                            email, string.Join(", ", result.Errors));
                    }
                }
                else
                {
                    _logger.LogInformation("Admin user already exists: {Email}", email);
                }
            }

            return Ok(new { message = "Roles and admin users seeded successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding roles and admins");
            return StatusCode(500, new { error = ErrorMessages.MigrationFailed, details = ex.Message });
        }
    }

    /// <summary>
    /// Seeds test users for development/testing. Only works when MigrationsEnabled=true.
    /// </summary>
    [HttpGet("seed-test-users")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SeedTestUsers()
    {
        if (!IsMigrationEnabled())
        {
            return BadRequest(new { error = ErrorMessages.MigrationDisabled });
        }

        try
        {
            var testUsers = _configuration.GetSection("Migration:TestUsers").Get<TestUserConfig[]>();
            var defaultPassword = _configuration["Migration:TestUserPassword"];

            if (testUsers == null || string.IsNullOrEmpty(defaultPassword))
            {
                return BadRequest("Test user configuration not found.");
            }

            foreach (var testUser in testUsers)
            {
                var user = await _userManager.FindByEmailAsync(testUser.Email);
                if (user == null)
                {
                    user = new ApplicationUser
                    {
                        UserName = testUser.Email,
                        Email = testUser.Email,
                        EmailConfirmed = true
                    };

                    var result = await _userManager.CreateAsync(user, defaultPassword);
                    if (result.Succeeded)
                    {
                        if (!string.IsNullOrEmpty(testUser.Role))
                        {
                            await _userManager.AddToRoleAsync(user, testUser.Role);
                        }
                        _logger.LogInformation("Created test user: {Email}", testUser.Email);
                    }
                    else
                    {
                        _logger.LogError("Failed to create test user {Email}: {Errors}", 
                            testUser.Email, string.Join(", ", result.Errors));
                    }
                }
            }

            return Ok(new { message = "Test users seeded successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding test users");
            return StatusCode(500, new { error = ErrorMessages.MigrationFailed, details = ex.Message });
        }
    }

    /// <summary>
    /// Migrates data from legacy schema to new schema. One-time operation.
    /// Requires MigrationsEnabled=true and checks for existing data.
    /// </summary>
    [HttpGet("migrate-data")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> MigrateData()
    {
        if (!IsMigrationEnabled())
        {
            return BadRequest(new { error = "Migrations are disabled in configuration." });
        }

        try
        {
            // Check if data already exists
            if (await _migrationService.HasExistingDataAsync())
            {
                _logger.LogWarning("Migration attempted but data already exists");
                return BadRequest(new { error = ErrorMessages.MigrationDataExists });
            }

            // Check if legacy schema exists
            var legacyExists = await _migrationService.CheckLegacySchemaExistsAsync();
            if (!legacyExists)
            {
                _logger.LogError("Legacy schema not found");
                return BadRequest(new { error = ErrorMessages.MigrationLegacyNotFound });
            }

            // Execute migrations
            var results = await _migrationService.ExecuteMigrationsAsync();
            
            var hasFailures = results.Any(r => r.Status == "Failed");
            if (hasFailures)
            {
                return StatusCode(500, new 
                { 
                    error = ErrorMessages.MigrationRolledBack,
                    results 
                });
            }

            return Ok(new 
            { 
                message = SuccessMessages.MigrationCompleted, 
                totalScripts = results.Count,
                totalDuration = results.Sum(r => r.Duration.TotalSeconds),
                results 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during migration");
            return StatusCode(500, new { error = ErrorMessages.MigrationFailed, details = ex.Message });
        }
    }

    /// <summary>
    /// Validates migration scripts without executing them.
    /// Useful for checking scripts before running the actual migration.
    /// </summary>
    [HttpGet("validate-scripts")]
    [Authorize(Roles = "Admin")]
    public IActionResult ValidateScripts()
    {
        if (!IsMigrationEnabled())
        {
            return BadRequest(new { error = ErrorMessages.MigrationDisabled });
        }

        try
        {
            var validations = _migrationService.ValidateMigrationScripts();
            var hasIssues = validations.Any(v => !v.IsValid || v.Warnings.Any());

            return Ok(new
            {
                totalScripts = validations.Count,
                hasIssues,
                validations
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating scripts");
            return StatusCode(500, new { error = ErrorMessages.ValidationFailed, details = ex.Message });
        }
    }

    private bool IsMigrationEnabled()
    {
        return _configuration.GetValue<bool>("Migration:Enabled", false);
    }
}

public class TestUserConfig
{
    public string Email { get; set; } = "";
    public string Role { get; set; } = "";
}
