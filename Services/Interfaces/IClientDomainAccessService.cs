namespace reviews.Services;

public interface IClientDomainAccessService
{
    Task<bool> IsEmailDomainAllowedAsync(string email);
}
