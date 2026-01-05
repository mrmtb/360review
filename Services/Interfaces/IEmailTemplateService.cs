using reviews.Models;

namespace reviews.Services;

public interface IEmailTemplateService
{
    Task<List<EmailTemplate>> GetEmailTemplatesByClientAsync(Guid clientId);
    Task<EmailTemplate?> GetEmailTemplateDetailsAsync(Guid templateId, Guid clientId);
    Task<bool> SaveEmailTemplateAsync(EmailTemplate model);
    EmailTemplate CreateNewEmailTemplate(Guid clientId);
    Task<List<EmailQueue>> GetEmailQueueAsync();
    Task<EmailQueue?> GetEmailDetailsAsync(int emailQueueId);
    Task<bool> RetryEmailAsync(int emailId);
    Task<bool> CancelEmailAsync(int emailId);
}
