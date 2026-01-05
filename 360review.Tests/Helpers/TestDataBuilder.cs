using reviews.Models;
using reviews.Constants;

namespace reviews.Tests.Helpers;

/// <summary>
/// Helper class to create test data with sensible defaults.
/// Makes tests more readable and maintainable.
/// </summary>
public static class TestDataBuilder
{
    public static Participant CreateParticipant(
        Guid? participantId = null,
        Guid? clientId = null,
        string firstName = "John",
        string lastName = "Doe",
        string? email = null,
        bool isActive = true)
    {
        var id = participantId ?? Guid.NewGuid();
        var client = clientId ?? Guid.NewGuid();
        
        return new Participant
        {
            ParticipantId = id,
            ClientId = client,
            ParticipantNameFirst = firstName,
            ParticipantNameLast = lastName,
            ParticipantEmail = email ?? $"{firstName.ToLower()}.{lastName.ToLower()}@test.com",
            ParticipantIsActive = isActive,
            ParticipantHasRegistered = false
        };
    }

    public static Client CreateClient(
        Guid? clientId = null,
        string name = "Test Client",
        string domain = "@testclient.com",
        bool isActive = true)
    {
        return new Client
        {
            ClientId = clientId ?? Guid.NewGuid(),
            ClientName = name,
            ClientEmailDomain = domain,
            ClientIsActive = isActive,
            ClientLogoLocation = "/images/logo.png",
            ClientHelpContact = "help@testclient.com"
        };
    }

    public static Evaluation CreateEvaluation(
        Guid? evaluationId = null,
        Guid? clientId = null,
        string name = "Test Evaluation",
        DateTime? openDate = null,
        DateTime? rateeCloseDate = null,
        DateTime? raterCloseDate = null)
    {
        var now = DateTime.UtcNow;
        
        return new Evaluation
        {
            EvaluationId = evaluationId ?? Guid.NewGuid(),
            ClientId = clientId ?? Guid.NewGuid(),
            EvaluationName = name,
            EvaluationOpenDate = openDate ?? now,
            EvaluationRateeDueDate = rateeCloseDate ?? now.AddDays(30),
            EvaluationRaterDueDate = raterCloseDate ?? now.AddDays(45),
            EvaluationFolder = "/evaluations/test",
            EvaluationActive = true
        };
    }

    public static EvaluationRatee CreateEvaluationRatee(
        Guid? evaluationId = null,
        Guid? participantId = null,
        int statusId = 1)
    {
        return new EvaluationRatee
        {
            EvaluationRateeId = Guid.NewGuid(),
            EvaluationId = evaluationId ?? Guid.NewGuid(),
            ParticipantId = participantId ?? Guid.NewGuid(),
            EvaluationRateeStatusId = statusId,
            EvaluationRateeDateAdded = DateTime.UtcNow
        };
    }

    public static EvaluationParticipant CreateEvaluationParticipant(
        Guid? evaluationRateeId = null,
        Guid? participantId = null,
        int roleId = 3, // Peer
        int statusId = 1) // Invitation Not Sent
    {
        return new EvaluationParticipant
        {
            EvaluationRateeId = evaluationRateeId ?? Guid.NewGuid(),
            ParticipantId = participantId ?? Guid.NewGuid(),
            ParticipantRoleId = roleId,
            EvaluationParticipantStatusId = statusId,
            EvaluationParticipantSurveyPassword = "TestPass123",
            EvaluationParticipantDateAdded = DateTime.UtcNow
        };
    }

    public static EmailTemplate CreateEmailTemplate(
        Guid? clientId = null,
        string name = "Test Template",
        string subject = "Test Subject",
        string content = "Test Content")
    {
        return new EmailTemplate
        {
            EmailTemplateId = Guid.NewGuid(),
            ClientId = clientId ?? Guid.NewGuid(),
            EmailTemplateName = name,
            EmailFromName = "Test Sender <test@test.com>",
            EmailSubject = subject,
            EmailTemplateContent = content
        };
    }

    public static EmailQueue CreateEmailQueue(
        Guid? participantId = null,
        Guid? clientId = null,
        string to = "test@test.com",
        string subject = "Test Email",
        bool sent = false)
    {
        return new EmailQueue
        {
            EmailQueueId = 1,
            ParticipantId = participantId ?? Guid.NewGuid(),
            ClientId = clientId ?? Guid.NewGuid(),
            EmailTo = to,
            EmailFrom = "system@test.com",
            EmailSubject = subject,
            EmailBodyText = "Test body",
            EmailBodyHTML = "<p>Test body</p>",
            EmailQueuedDateTime = DateTime.UtcNow,
            EmailSent = sent,
            EmailSentDateTime = DateTime.UtcNow,
            EmailTest = false,
            EmailActor = "System"
        };
    }

    /// <summary>
    /// Creates a list of participants for testing collections
    /// </summary>
    public static List<Participant> CreateParticipantList(int count = 3)
    {
        var participants = new List<Participant>();
        var clientId = Guid.NewGuid();
        
        for (int i = 0; i < count; i++)
        {
            participants.Add(CreateParticipant(
                clientId: clientId,
                firstName: $"User{i}",
                lastName: "Test"));
        }
        
        return participants;
    }

    /// <summary>
    /// Creates a list of vwAdminParticipants for testing admin views
    /// </summary>
    public static List<vwAdminParticipants> CreateAdminParticipantList(int count = 3)
    {
        var participants = new List<vwAdminParticipants>();
        var clientId = Guid.NewGuid();
        
        for (int i = 0; i < count; i++)
        {
            participants.Add(new vwAdminParticipants
            {
                ClientId = clientId,
                ClientName = "Test Client",
                ParticipantId = Guid.NewGuid(),
                ParticipantNameFirst = $"User{i}",
                ParticipantNameLast = "Test",
                ParticipantEmail = $"user{i}.test@test.com",
                IsRatee = "Yes",
                IsActive = "Yes",
                ParticipantIsActive = true,
                EvaluationParticipantDateLastEmailed = null
            });
        }
        
        return participants;
    }

    /// <summary>
    /// Creates a complete evaluation setup with ratees and raters
    /// </summary>
    public static (Evaluation evaluation, List<EvaluationRatee> ratees, List<EvaluationParticipant> raters) 
        CreateCompleteEvaluation(int rateeCount = 2, int ratersPerRatee = 3)
    {
        var clientId = Guid.NewGuid();
        var evaluation = CreateEvaluation(clientId: clientId);
        var ratees = new List<EvaluationRatee>();
        var raters = new List<EvaluationParticipant>();

        for (int i = 0; i < rateeCount; i++)
        {
            var ratee = CreateEvaluationRatee(
                evaluationId: evaluation.EvaluationId);
            ratees.Add(ratee);

            for (int j = 0; j < ratersPerRatee; j++)
            {
                var rater = CreateEvaluationParticipant(
                    evaluationRateeId: ratee.EvaluationRateeId);
                raters.Add(rater);
            }
        }

        return (evaluation, ratees, raters);
    }
}
