using Microsoft.EntityFrameworkCore;
using reviews.Models;

namespace reviews.Services;

public class ClientDomainAccessService : IClientDomainAccessService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<ClientDomainAccessService> _logger;

    public ClientDomainAccessService(
        ApplicationDbContext db,
        ILogger<ClientDomainAccessService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<bool> IsEmailDomainAllowedAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            return false;
        }

        var emailDomain = email.Split('@').LastOrDefault()?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(emailDomain))
        {
            return false;
        }

        var configuredDomains = await _db.Clients
            .AsNoTracking()
            .Where(c => c.ClientIsActive && !string.IsNullOrWhiteSpace(c.ClientEmailDomain))
            .Select(c => c.ClientEmailDomain!)
            .ToListAsync();

        var allowedDomains = configuredDomains
            .SelectMany(SplitDomains)
            .Select(d => d.Trim().TrimStart('@').ToLowerInvariant())
            .Where(d => !string.IsNullOrWhiteSpace(d))
            .Distinct()
            .ToList();

        var allowed = allowedDomains.Contains(emailDomain);
        if (!allowed)
        {
            _logger.LogWarning("Rejected external login for email domain {Domain}", emailDomain);
        }

        return allowed;
    }

    private static IEnumerable<string> SplitDomains(string value)
    {
        return value.Split(new[] { ',', ';', '|', ' ' }, StringSplitOptions.RemoveEmptyEntries);
    }
}
