using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using reviews.Models;

namespace reviews.Services;

public class EmailTemplateService : IEmailTemplateService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<EmailTemplateService> _logger;

    public EmailTemplateService(ApplicationDbContext db, ILogger<EmailTemplateService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<List<EmailTemplate>> GetEmailTemplatesByClientAsync(Guid clientId)
    {
        return await _db.EmailTemplates
            .Include(et => et.Client)
            .AsNoTracking()
            .Where(r => r.ClientId == clientId)
            .ToListAsync();
    }

    public async Task<EmailTemplate?> GetEmailTemplateDetailsAsync(Guid templateId, Guid clientId)
    {
        return await _db.EmailTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.EmailTemplateId == templateId && c.ClientId == clientId);
    }

    public async Task<bool> SaveEmailTemplateAsync(EmailTemplate model)
    {
        try
        {
            var emailTemplate = await _db.EmailTemplates
                .FirstOrDefaultAsync(c => c.EmailTemplateId == model.EmailTemplateId);

            if (emailTemplate == null)
            {
                emailTemplate = new EmailTemplate
                {
                    ClientId = model.ClientId
                };
            }

            // Update properties
            var properties = typeof(EmailTemplate).GetProperties();
            foreach (var prop in properties)
            {
                if (prop.Name == "ClientId" || prop.Name == "EmailTemplateId") continue;

                var newValue = prop.GetValue(model);
                var oldValue = prop.GetValue(emailTemplate);

                if (prop.PropertyType == typeof(DateTime?) && newValue != null)
                {
                    var dateValue = (DateTime?)newValue;
                    if (dateValue.HasValue)
                    {
                        if (!oldValue?.Equals(dateValue.Value) ?? true)
                        {
                            prop.SetValue(emailTemplate, dateValue.Value);
                        }
                    }
                }
                else if ((oldValue == null && newValue != null) || (oldValue != null && !oldValue.Equals(newValue)))
                {
                    prop.SetValue(emailTemplate, newValue);
                }
            }

            if (emailTemplate.EmailTemplateId == Guid.Empty)
            {
                emailTemplate.EmailTemplateId = Guid.NewGuid();
                _db.EmailTemplates.Add(emailTemplate);
            }
            else
            {
                _db.EmailTemplates.Update(emailTemplate);
            }

            await _db.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save email template {TemplateId}", model.EmailTemplateId);
            return false;
        }
    }

    public EmailTemplate CreateNewEmailTemplate(Guid clientId)
    {
        return new EmailTemplate
        {
            ClientId = clientId
        };
    }

    public async Task<List<EmailQueue>> GetEmailQueueAsync()
    {
        return await _db.EmailQueue
            .AsNoTracking()
            .OrderByDescending(e => e.EmailQueuedDateTime)
            .ToListAsync();
    }

    public async Task<EmailQueue?> GetEmailDetailsAsync(int emailQueueId)
    {
        return await _db.EmailQueue
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.EmailQueueId == emailQueueId);
    }

    public async Task<bool> RetryEmailAsync(int emailId)
    {
        try
        {
            var email = await _db.EmailQueue.FirstOrDefaultAsync(e => e.EmailQueueId == emailId);
            if (email == null || email.EmailSent)
            {
                return false;
            }

            email.EmailSent = false;
            await _db.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retry email {EmailId}", emailId);
            return false;
        }
    }

    public async Task<bool> CancelEmailAsync(int emailId)
    {
        try
        {
            var email = await _db.EmailQueue.FirstOrDefaultAsync(e => e.EmailQueueId == emailId);
            if (email == null || email.EmailSent)
            {
                return false;
            }

            _db.EmailQueue.Remove(email);
            await _db.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel email {EmailId}", emailId);
            return false;
        }
    }
}
