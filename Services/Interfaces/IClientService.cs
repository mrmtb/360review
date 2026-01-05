using reviews.Models;

namespace reviews.Services;

public interface IClientService
{
    Task<List<Client>> GetAllClientsAsync();
    Task<Client?> GetClientDetailsAsync(Guid clientId);
    Task<bool> SaveClientAsync(Client model);
    Client CreateNewClient();
    Task<bool> DeleteClientAsync(Guid clientId);
}
