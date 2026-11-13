using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using reviews.Models;

namespace reviews.Services;

public class ClientService : IClientService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<ClientService> _logger;
    private readonly IConfiguration _configuration;

    public ClientService(ApplicationDbContext db, ILogger<ClientService> logger, IConfiguration? configuration = null)
    {
        _db = db;
        _logger = logger;
        _configuration = configuration ?? new ConfigurationBuilder().Build();
    }

    public async Task<List<Client>> GetAllClientsAsync()
    {
        return await _db.Clients
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Client?> GetClientDetailsAsync(Guid clientId)
    {
        return await _db.Clients
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ClientId == clientId);
    }

    public async Task<bool> SaveClientAsync(Client model)
    {
        try
        {
            var client = await _db.Clients.FirstOrDefaultAsync(c => c.ClientId == model.ClientId);
            var isNew = client == null;

            if (client == null)
            {
                client = new Client
                {
                    ClientId = model.ClientId == Guid.Empty ? Guid.NewGuid() : model.ClientId
                };
            }

            // Update properties
            var properties = typeof(Client).GetProperties();
            foreach (var prop in properties)
            {
                if (prop.Name == "ClientId" || !prop.PropertyType.IsValueType && prop.PropertyType != typeof(string)) continue;

                var newValue = prop.GetValue(model);
                var oldValue = prop.GetValue(client);

                if (prop.PropertyType == typeof(DateTime?) && newValue != null)
                {
                    var dateValue = (DateTime?)newValue;
                    if (dateValue.HasValue)
                    {
                        // Convert to UTC for PostgreSQL timestamp with time zone
                        var utcValue = dateValue.Value.Kind == DateTimeKind.Unspecified 
                            ? DateTime.SpecifyKind(dateValue.Value, DateTimeKind.Utc)
                            : dateValue.Value.ToUniversalTime();

                        if (!oldValue?.Equals(utcValue) ?? true)
                        {
                            prop.SetValue(client, utcValue);
                        }
                    }
                }
                else if ((oldValue == null && newValue != null) || (oldValue != null && !oldValue.Equals(newValue)))
                {
                    prop.SetValue(client, newValue);
                }
            }

            if (isNew)
            {
                var version = await SurveyVersionBootstrapper.CreateInitialVersionAsync(client.ClientId, _configuration);
                _db.Clients.Add(client);
                _db.SurveyDefinitionVersions.Add(version);
            }
            else
            {
                _db.Clients.Update(client);
            }

            await _db.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save client {ClientId}", model.ClientId);
            return false;
        }
    }

    public Client CreateNewClient()
    {
        return new Client();
    }

    public async Task<bool> DeleteClientAsync(Guid clientId)
    {
        try
        {
            var client = await _db.Clients.FirstOrDefaultAsync(c => c.ClientId == clientId);
            if (client == null)
            {
                _logger.LogWarning("Client not found for deletion: {ClientId}", clientId);
                return false;
            }

            _db.Clients.Remove(client);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Deleted client {ClientId}", clientId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete client {ClientId}", clientId);
            return false;
        }
    }
}
