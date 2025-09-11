using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using reviews.Models;
using reviews.Services; 

namespace reviews.Services
{
    public class EmailQueueHelper
    {
        private readonly ApplicationDbContext _db;
        private readonly IEmailService _emailService;
        private readonly IHttpContextAccessor _httpContextAccessor;


        public EmailQueueHelper(ApplicationDbContext db, IEmailService emailService, IHttpContextAccessor httpContextAccessor)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        public async Task ProcessParticipantsForEmailQueueAsync(IEnumerable<vwInvitations> invitations, IEnumerable<EmailTemplate> emailTemplates, bool isTest = true, string actor = "System")
        {
            if (invitations == null || !invitations.Any())
            {
                throw new ArgumentException("Email list cannot be null or empty.");
            }

            if (emailTemplates == null || !emailTemplates.Any())
            {
                throw new ArgumentException("Email templates cannot be null or empty.");
            }

            var emailQueueEntries = new List<EmailQueue>();

            foreach (var invitation in invitations)
            {
                var template = emailTemplates.FirstOrDefault(t => t.EmailTemplateName == invitation.MessageToSend);

                if (template == null)
                {
                    throw new InvalidOperationException($"No email template found for {invitation.MessageToSend}");
                }

                // Perform merge operation (e.g., replace placeholders in the email template)
                var emailBody = MergeTemplateWithData(template.EmailTemplateContent, invitation);
                var emailSender = MergeTemplateWithData(template.EmailFromName, invitation);
                var emailSubject = MergeTemplateWithData(template.EmailSubject, invitation);

                // Create a new EmailQueue entry
                var emailQueueEntry = new EmailQueue
                {
                    ParticipantId = invitation.RaterParticipantId,
                    EvaluationRateeId = invitation.EvaluationRateeId,
                    ClientId = invitation.ClientId,
                    EmailActor = actor,
                    EmailQueuedDateTime = DateTime.UtcNow,
                    EmailTest = isTest || invitation.EvaluationTestEmails,
                    EmailFrom = emailSender,
                    EmailTo = invitation.RaterEmail,
                    EmailSubject = emailSubject,
                    EmailBodyText = emailBody,
                    EmailBodyHTML = emailBody,
                    EmailSent = false
                };

                emailQueueEntries.Add(emailQueueEntry);
            }

            // Add entries to the EmailQueue table
            await _db.EmailQueue.AddRangeAsync(emailQueueEntries);

            // Save changes to the database
            await _db.SaveChangesAsync();

            foreach (var message in emailQueueEntries)
            {
                // Log the email queue entry for debugging
                Console.WriteLine($"Queued email to {message.EmailTo} with subject '{message.EmailSubject}'");
                // Process the email queue
                await ProcessEmailItemFromQueue(message.EmailQueueId);
            }

            return;
        }

        private string MergeTemplateWithData(string template, vwInvitations invitation)
        {
            // Use reflection to get all properties of the invitation object
            var properties = invitation.GetType().GetProperties();
            // Get the current request context to build the site URL
            var request = _httpContextAccessor.HttpContext?.Request;
            var siteUrl = request != null ? $"{request.Scheme}://{request.Host}" : "";

            template = template.Replace($"*Url*", siteUrl);

            foreach (var property in properties)
            {
                // Get the property name and value
                var propertyName = property.Name;
                var propertyValue = property.GetValue(invitation)?.ToString() ?? string.Empty;

                // Replace occurrences of *PropertyName* in the template
                template = template.Replace($"*{propertyName}*", propertyValue);
            }

            if (template.Contains("*"))
            {
                throw new InvalidOperationException("Unresolved placeholders in the email template.");
            }

            return template;
        }

        private async Task ProcessEmailFromQueue(Guid? clientId)
        {
            var emailQueue = new List<EmailQueue>();

            if (clientId == null)
            {
                emailQueue = await _db.EmailQueue
                    .Where(eq => !eq.EmailSent)
                    .ToListAsync();
            }
            else
            {
                emailQueue = await _db.EmailQueue
                    .Where(eq => eq.ClientId == clientId && !eq.EmailSent)
                    .ToListAsync();
            }

            if (emailQueue != null && emailQueue.Any())
            {
                foreach (var email in emailQueue)
                {
                    await _emailService.SendEmailWithFromAsync(email.EmailTest ? email.EmailActor : email.EmailTo, email.EmailFrom, email.EmailSubject, email.EmailBodyText, email.EmailBodyHTML, email.EmailTest);

                    // After sending, mark the email as sent
                    email.EmailSent = true;
                    email.EmailSentDateTime = DateTime.UtcNow;

                    // Update the record in the EvaluationParticipant table
                    // this needs to be tweaked to handle reminders - one reminder goes out and updates all EvaluationId + ParticipantId rows
                    var participant = await _db.EvaluationParticipants
                        .FirstOrDefaultAsync(ep => ep.EvaluationRateeId == email.EvaluationRateeId && ep.ParticipantId == email.ParticipantId);
                    if (participant != null)
                    {
                        participant.EvaluationParticipantDateLastEmailed = email.EmailSentDateTime;
                        participant.EvaluationParticipantStatusId = 4; // Status for "Invitation Sent"; Todo: define state machine :) 
                        _db.EvaluationParticipants.Update(participant);
                    }
                }
            }
            // Save changes to the database
            await _db.SaveChangesAsync();

            return;
        }

        private async Task ProcessEmailItemFromQueue(int emailId)
        {
            var emailQueue = new List<EmailQueue>();

            emailQueue = await _db.EmailQueue
                .Where(eq => !eq.EmailSent && eq.EmailQueueId == emailId)
                .ToListAsync();

            if (emailQueue != null && emailQueue.Any())
            {
                foreach (var email in emailQueue)
                {
                    await _emailService.SendEmailWithFromAsync(email.EmailTest ? email.EmailActor : email.EmailTo, email.EmailFrom, email.EmailSubject, email.EmailBodyText, email.EmailBodyHTML, email.EmailTest);

                    // After sending, mark the email as sent
                    email.EmailSent = true;
                    email.EmailSentDateTime = DateTime.UtcNow;

                    await MarkLastSentDate(email);
                }
            }
            // Save changes to the database
            await _db.SaveChangesAsync();

            return;
        }

        private async Task MarkLastSentDate(EmailQueue message)
        {
            var participant = new List<EvaluationParticipant>();

            if (message.EmailSubject.ToLower().Contains("reminder"))
            {
                var evaluationRatees = new List<Guid>(_db.EvaluationRatees
                    .Where(er => er.EvaluationId == _db.EvaluationRatees
                        .Where(innerEr => innerEr.EvaluationRateeId == message.EvaluationRateeId)
                        .Select(innerEr => innerEr.EvaluationId)
                        .FirstOrDefault())
                    .Select(er => er.EvaluationRateeId)
                    .ToList());
                participant = _db.EvaluationParticipants.Where(ep => ep.ParticipantId == message.ParticipantId
                                                                  && evaluationRatees.Contains(ep.EvaluationRateeId)
                                                                  && ep.EvaluationParticipantStatusId == 4).ToList();
            }
            else
            {
                participant = _db.EvaluationParticipants.Where(ep => ep.EvaluationRateeId == message.EvaluationRateeId && ep.ParticipantId == message.ParticipantId).ToList();
            }

            foreach (var evaluationParticipant in participant)
            {
                // Update the EvaluationParticipant record with the last emailed date
                evaluationParticipant.EvaluationParticipantDateLastEmailed = message.EmailSentDateTime;
                evaluationParticipant.EvaluationParticipantStatusId = 4; // Status for "Invitation Sent"; Todo: define state machine :) 
                _db.EvaluationParticipants.Update(evaluationParticipant);
            }

            await _db.SaveChangesAsync();
        }
    }
}