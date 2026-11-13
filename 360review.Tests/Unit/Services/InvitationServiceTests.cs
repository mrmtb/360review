using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using reviews.Constants;
using reviews.Models;
using reviews.Services;
using reviews.Tests.Helpers;

namespace reviews.Tests.Unit.Services;

public class InvitationServiceTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SendAssignmentEmailsAsync_ResendsOnlySelectedAssignment(bool reminder)
    {
        await using var db = CreateDbContext();
        var client = TestDataBuilder.CreateClient();
        var rater = TestDataBuilder.CreateParticipant(clientId: client.ClientId);
        var ratee = TestDataBuilder.CreateParticipant(clientId: client.ClientId, email: "ratee@test.com");
        var status = new EvaluationParticipantStatus
        {
            EvaluationParticipantStatusId = EvaluationParticipantStatusIds.InvitationSent,
            EvaluationParticipantStatusName = "Invited", EvaluationParticipantStatusIsActive = 1,
            EvaluationParticipantStatusDecoration = ""
        };
        var role = new ParticipantRole { ParticipantRoleId = 3, ParticipantRoleName = "Peer" };
        db.Clients.Add(client);
        db.Participants.AddRange(rater, ratee);
        db.EvaluationParticipantStatuses.Add(status);
        db.ParticipantRoles.Add(role);
        var originalTimestamp = DateTime.UtcNow.AddDays(-2);
        var assignments = new List<EvaluationParticipant>();
        for (var index = 0; index < 2; index++)
        {
            var evaluation = TestDataBuilder.CreateEvaluation(clientId: client.ClientId);
            evaluation.Client = client;
            var evaluationRatee = TestDataBuilder.CreateEvaluationRatee(evaluation.EvaluationId, ratee.ParticipantId);
            evaluationRatee.Evaluations = evaluation;
            evaluationRatee.Participants = ratee;
            var assignment = TestDataBuilder.CreateEvaluationParticipant(evaluationRatee.EvaluationRateeId, rater.ParticipantId, statusId: status.EvaluationParticipantStatusId);
            assignment.EvaluationRatee = evaluationRatee;
            assignment.Participants = rater;
            assignment.ParticipantRoles = role;
            assignment.EvaluationParticipantStatus = status;
            assignment.EvaluationParticipantDateLastEmailed = originalTimestamp;
            db.EvaluationParticipants.Add(assignment);
            assignments.Add(assignment);
        }

        var template = TestDataBuilder.CreateEmailTemplate(clientId: client.ClientId,
            name: reminder ? EmailTemplateNames.ReminderGeneral : EmailTemplateNames.InvitationRater,
            subject: reminder ? "Reminder" : "Invitation", content: "Test message");
        db.EmailTemplates.Add(template);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var count = await service.SendAssignmentEmailsAsync(
            new() { (assignments[0].EvaluationRateeId, rater.ParticipantId) }, client.ClientId, "actor@test.com", reminder);

        Assert.Equal(1, count);
        var queued = Assert.Single(await db.EmailQueue.ToListAsync());
        Assert.Equal(assignments[0].EvaluationRateeId, queued.EvaluationRateeId);
        Assert.NotEqual(originalTimestamp, assignments[0].EvaluationParticipantDateLastEmailed);
        Assert.Equal(originalTimestamp, assignments[1].EvaluationParticipantDateLastEmailed);
    }

    [Fact]
    public async Task EmptyTargets_DoNotQueueEmails()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        Assert.Equal(0, await service.SendAssignmentEmailsAsync(new(), Guid.NewGuid(), "actor@test.com", false));
        Assert.False(await service.ProcessInvitationsAsync(new(), Guid.NewGuid(), "actor@test.com"));
        Assert.Empty(await db.EmailQueue.ToListAsync());
    }

    private static InvitationService CreateService(ApplicationDbContext db)
    {
        var helper = new EmailQueueHelper(db, Mock.Of<IEmailService>(), new HttpContextAccessor());
        return new InvitationService(db, helper, Mock.Of<ILogger<InvitationService>>());
    }

    private static ApplicationDbContext CreateDbContext()
    {
        return new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, null!);
    }
}