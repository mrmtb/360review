using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using reviews.Middleware;
using reviews.Models;
using reviews.Services;

var builder = WebApplication.CreateBuilder(args);


var connectionString = builder.Configuration.GetConnectionString("ApplicationDBConnection") ?? throw new InvalidOperationException("Connection string 'ApplicationDbContextConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Add session services
builder.Services.AddDistributedMemoryCache(); // Required
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60); // Optional timeout
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IReviewSessionService, ReviewSessionService>();
builder.Services.AddScoped<EmailQueueHelper>();
builder.Services.AddScoped<IEmailService, EmailFromSender>();

// Add service layer classes
builder.Services.AddScoped<IParticipantService, ParticipantService>();
builder.Services.AddScoped<IEvaluationService, EvaluationService>();
builder.Services.AddScoped<IInvitationService, InvitationService>();
builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<IEmailTemplateService, EmailTemplateService>();
builder.Services.AddScoped<ISurveyUrlBuilder, SurveyUrlBuilder>();
builder.Services.AddScoped<ISurveyCompletionService, SurveyCompletionService>();
builder.Services.AddScoped<IUserRoleService, UserRoleService>();
builder.Services.AddScoped<ISessionInitializationService, SessionInitializationService>();
builder.Services.AddScoped<ISessionBootstrapService, SessionBootstrapService>();
builder.Services.AddScoped<IClientDomainAccessService, ClientDomainAccessService>();
builder.Services.AddScoped<MigrationService>();

// SurveyJS Integration Services (Issue #9)
builder.Services.AddScoped<ISurveyTemplateResolver, SurveyTemplateResolver>();
builder.Services.AddScoped<ISurveyAssignmentResolver, SurveyAssignmentResolver>();
builder.Services.AddScoped<ISurveyResponseStore, SurveyResponseStore>();
builder.Services.AddScoped<IEvaluationParticipantStatusService, EvaluationParticipantStatusService>();

builder.Services.AddDistributedMemoryCache();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

builder.Services.Configure<IdentityOptions>(options =>
{
    // Password settings.
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;
    options.Password.RequiredUniqueChars = 1;

    // Lockout settings.
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(120);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings.
    options.User.AllowedUserNameCharacters =
    "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = true;
});



builder.Services.ConfigureApplicationCookie(options =>
{
    // Cookie settings
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(20);

    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.SlidingExpiration = true;
});

var entraAuthSection = builder.Configuration.GetSection("Authentication:Entra");
var entraEnabled = entraAuthSection.GetValue<bool>("Enabled");
var entraShowLoginOption = entraAuthSection.GetValue<bool?>("ShowLoginOption") ?? entraEnabled;
var entraClientId = entraAuthSection["ClientId"];
var entraClientSecret = entraAuthSection["ClientSecret"];
var entraTenantId = entraAuthSection["TenantId"] ?? "common";
var entraAuthorityBase = entraAuthSection["AuthorityBase"] ?? "https://login.microsoftonline.com";
var entraCallbackPath = entraAuthSection["CallbackPath"] ?? "/signin-oidc";

if (entraEnabled && entraShowLoginOption)
{
    if (string.IsNullOrWhiteSpace(entraClientId) || string.IsNullOrWhiteSpace(entraClientSecret))
    {
        Console.WriteLine("Entra authentication is enabled but ClientId/ClientSecret is missing. External login provider not configured.");
    }
    else
    {
        builder.Services
            .AddAuthentication()
            .AddOpenIdConnect("Microsoft Entra", options =>
            {
                options.SignInScheme = IdentityConstants.ExternalScheme;
                options.Authority = $"{entraAuthorityBase.TrimEnd('/')}/{entraTenantId}/v2.0";
                options.ClientId = entraClientId;
                options.ClientSecret = entraClientSecret;
                options.CallbackPath = entraCallbackPath;
                options.ResponseType = "code";
                options.UsePkce = true;
                options.SaveTokens = false;
                options.GetClaimsFromUserInfoEndpoint = false;

                options.Scope.Clear();
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.Scope.Add("email");

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    NameClaimType = "preferred_username"
                };

                options.Events = new OpenIdConnectEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var identity = context.Principal?.Identity as ClaimsIdentity;
                        if (identity == null)
                        {
                            context.Fail("Invalid external identity.");
                            return;
                        }

                        var email =
                            context.Principal?.FindFirstValue(ClaimTypes.Email) ??
                            context.Principal?.FindFirstValue("preferred_username") ??
                            context.Principal?.FindFirstValue("upn");

                        if (string.IsNullOrWhiteSpace(email))
                        {
                            context.Fail("Email claim is required.");
                            return;
                        }

                        if (!identity.HasClaim(c => c.Type == ClaimTypes.Email))
                        {
                            identity.AddClaim(new Claim(ClaimTypes.Email, email));
                        }

                        var domainAccess = context.HttpContext.RequestServices.GetRequiredService<IClientDomainAccessService>();
                        var allowed = await domainAccess.IsEmailDomainAllowedAsync(email);
                        if (!allowed)
                        {
                            context.Fail("Your organization domain is not allowed for this application.");
                        }
                    },
                    OnRemoteFailure = context =>
                    {
                        context.HandleResponse();
                        context.Response.Redirect("/Identity/Account/Login?error=ExternalAuthenticationFailed");
                        return Task.CompletedTask;
                    }
                };
            });
    }
}
else if (entraEnabled && !entraShowLoginOption)
{
    Console.WriteLine("Entra authentication is enabled but ShowLoginOption is false. External login provider is intentionally hidden.");
}

// Apply migrations BEFORE configuring DataProtection to ensure table exists
var tempConnectionString = builder.Configuration.GetConnectionString("ApplicationDBConnection");
var tempOptionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
tempOptionsBuilder.UseNpgsql(tempConnectionString);

using (var tempContext = new ApplicationDbContext(tempOptionsBuilder.Options, builder.Configuration))
{
    try
    {
        Console.WriteLine("Checking for pending database migrations...");
        var pendingMigrations = tempContext.Database.GetPendingMigrations();
        if (pendingMigrations.Any())
        {
            Console.WriteLine($"Applying {pendingMigrations.Count()} pending migrations: {string.Join(", ", pendingMigrations)}");
            tempContext.Database.Migrate();
            Console.WriteLine("Database migrations applied successfully");
        }
        else
        {
            Console.WriteLine("Database is up to date, no pending migrations");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error during migration: {ex.Message}");
        Console.WriteLine("Application will continue but may have database issues");
    }
}

// Now configure DataProtection with database storage (migrations are complete)
builder.Services.AddDataProtection()
    .PersistKeysToDbContext<ApplicationDbContext>()
    .SetApplicationName("360ReviewApp");

builder.Services.AddTransient<IEmailSender, EmailSender>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Use session middleware
app.UseSession();

// Add cookie middleware
app.UseCookiePolicy();

app.UseRouting();

app.UseAuthentication();
app.UseMiddleware<SessionBootstrapMiddleware>();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
