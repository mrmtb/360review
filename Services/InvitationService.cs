using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using reviews.Constants;
using reviews.Models;

namespace reviews.Services;

public class InvitationService : IInvitationService
{
    private readonly ApplicationDbContext _db;
    private readonly EmailQueueHelper _emailQueueHelper;
    private readonly ILogger<InvitationService> _logger;

    public InvitationService(
        ApplicationDbContext db, 
        EmailQueueHelper emailQueueHelper,
        ILogger<InvitationService> logger)
    {
        _db = db;
        _emailQueueHelper = emailQueueHelper;
        _logger = logger;
    }

    public async Task<List<vwReminders>> GetRemindersByClientAsync(Guid clientId)
    {
        return await _db.ViewReminders
            .AsNoTracking()
            .Where(p => p.ClientId == clientId)
            .ToListAsync();
    }

    public async Task<bool> ProcessRemindersAsync(List<Guid> participantIds, Guid clientId, string userEmail)
    {
        try
        {
            if (participantIds == null || !participantIds.Any())
            {
                _logger.LogWarning("No participants selected for reminders");
                return false;
            }

            var reminders = await _db.ViewReminders
                .Where(p => p.ClientId == clientId && participantIds.Contains(p.ParticipantId))
                .OrderBy(o => o.ParticipantEmail)
                .ToListAsync();

            if (!reminders.Any())
            {
                _logger.LogWarning("No reminders found for the selected participants");
                return false;
            }

            var emailTemplates = await _db.EmailTemplates
                .Where(p => p.ClientId == clientId && p.EmailTemplateName == StatusNames.ReminderGeneral)
                .ToListAsync();

            if (!emailTemplates.Any())
            {
                _logger.LogWarning("No reminder email templates found for client {ClientId}", clientId);
                return false;
            }

            // Get unique reminders by email
            var uniqueReminders = reminders
                .GroupBy(r => r.ParticipantEmail)
                .Select(g => g.First())
                .ToList();

            foreach (var reminder in uniqueReminders)
            {
                await _emailQueueHelper.ProcessParticipantsForEmailQueueAsync(
                    MapRemindersToInvitations(new List<vwReminders> { reminder }),
                    emailTemplates,
                    false,
                    userEmail);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process reminders for client {ClientId}", clientId);
            return false;
        }
    }

    public async Task<List<vwInvitations>> GetInvitationsByClientAsync(Guid clientId)
    {
        var raterInvitations = new List<vwInvitations>(await _db.ViewRaterInvitations
            .AsNoTracking()
            .Where(p => p.ClientId == clientId)
            .ToListAsync());

        var rateeInvitations = new List<vwInvitations>(await _db.ViewRateeInvitations
            .AsNoTracking()
            .Where(p => p.ClientId == clientId)
            .ToListAsync());

        return raterInvitations
            .Union(rateeInvitations)
            .OrderBy(p => p.RaterLastName)
            .ThenBy(p => p.RaterFirstName)
            .ToList();
    }

    public async Task<bool> ProcessInvitationsAsync(
        List<(Guid EvaluationRateeId, Guid ParticipantId)>? participantTuples,
        Guid clientId,
        string userEmail)
    {
        try
        {
            var invitations = await GetInvitationsByClientAsync(clientId);

            if (participantTuples != null && participantTuples.Any())
            {
                invitations = invitations
                    .Where(i => participantTuples.Any(t => 
                        t.ParticipantId == i.RaterParticipantId && 
                        t.EvaluationRateeId == i.EvaluationRateeId))
                    .ToList();
            }

            if (!invitations.Any())
            {
                _logger.LogWarning("No invitations found to process");
                return false;
            }

            var messages = await _db.EmailTemplates
                .AsNoTracking()
                .Where(t => t.ClientId == clientId)
                .ToListAsync();

            await _emailQueueHelper.ProcessParticipantsForEmailQueueAsync(
                invitations,
                messages,
                false,
                userEmail);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process invitations for client {ClientId}", clientId);
            return false;
        }
    }

    private List<vwInvitations> MapRemindersToInvitations(List<vwReminders> reminders)
    {
        return reminders.Select(reminder => new vwInvitations
        {
            RateeFirstName = reminder.RateeNameFirst,
            RateeLastName = reminder.RateeNameLast,
            RaterRole = reminder.ParticipantRoleName,
            RaterFirstName = reminder.ParticipantNameFirst,
            RaterLastName = reminder.ParticipantNameLast,
            RaterEmail = reminder.ParticipantEmail,
            RaterParticipantId = reminder.ParticipantId,
            EvaluationRateeId = reminder.EvaluationRateeId,
            EvaluationTestEmails = reminder.EvaluationTestEmails,
            DueDate = reminder.DueDate,
            ClientName = reminder.ClientName,
            ClientId = reminder.ClientId,
            ClientHelpContact = reminder.ClientHelpContact,
            MessageToSend = reminder.MessageToSend
        }).ToList();
    }
}
