using Moq;
using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using reviews.Controllers.Admin;
using reviews.Models;
using reviews.Services;
using reviews.Constants;
using reviews.Tests.Helpers;

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type
#pragma warning disable CS8602 // Dereference of a possibly null reference
#pragma warning disable CS8604 // Possible null reference argument

namespace reviews.Tests.Unit.Controllers;

/// <summary>
/// Unit tests for AdminController.
/// Tests controller logic with mocked service dependencies.
/// </summary>
public class AdminControllerTests
{
    private readonly Mock<IParticipantService> _mockParticipantService;
    private readonly Mock<IEvaluationService> _mockEvaluationService;
    private readonly Mock<IInvitationService> _mockInvitationService;
    private readonly Mock<IClientService> _mockClientService;
    private readonly Mock<IEmailTemplateService> _mockEmailTemplateService;
    private readonly Mock<IReviewSessionService> _mockReviewSessionService;
    private readonly Mock<ILogger<AdminController>> _mockLogger;
    private readonly AdminController _controller;

    public AdminControllerTests()
    {
        _mockParticipantService = new Mock<IParticipantService>();
        _mockEvaluationService = new Mock<IEvaluationService>();
        _mockInvitationService = new Mock<IInvitationService>();
        _mockClientService = new Mock<IClientService>();
        _mockEmailTemplateService = new Mock<IEmailTemplateService>();
        _mockReviewSessionService = new Mock<IReviewSessionService>();
        _mockLogger = new Mock<ILogger<AdminController>>();

        _controller = new AdminController(
            _mockParticipantService.Object,
            _mockEvaluationService.Object,
            _mockInvitationService.Object,
            _mockClientService.Object,
            _mockEmailTemplateService.Object,
            _mockReviewSessionService.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task People_ReturnsViewWithParticipants()
    {
        // Arrange
        var people = new List<AdminPersonRow>
        {
            new() { ParticipantId = Guid.NewGuid(), ParticipantNameFirst = "A", ParticipantNameLast = "One", ParticipantEmail = "a1@example.com" },
            new() { ParticipantId = Guid.NewGuid(), ParticipantNameFirst = "B", ParticipantNameLast = "Two", ParticipantEmail = "b2@example.com" },
            new() { ParticipantId = Guid.NewGuid(), ParticipantNameFirst = "C", ParticipantNameLast = "Three", ParticipantEmail = "c3@example.com" }
        };

        _mockParticipantService
            .Setup(x => x.GetAdminPeopleByClientAsync(It.IsAny<Guid>()))
            .ReturnsAsync(people);

        // Act
        var result = await _controller.People("participants");

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("ViewPeople", viewResult.ViewName);
        var model = Assert.IsType<AdminPeopleViewModel>(viewResult.Model);
        Assert.Equal(3, model.People.Count);
    }

    [Fact]
    public async Task People_ServiceReturnsEmpty_ReturnsEmptyView()
    {
        // Arrange
        _mockParticipantService
            .Setup(x => x.GetAdminPeopleByClientAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new List<AdminPersonRow>());

        // Act
        var result = await _controller.People("participants");

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("ViewPeople", viewResult.ViewName);
        var model = Assert.IsType<AdminPeopleViewModel>(viewResult.Model);
        Assert.Empty(model.People);
    }

    [Fact]
    public void ViewParticipants_RedirectsToPeopleParticipantsTab()
    {
        var result = _controller.ViewParticipants();
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("People", redirectResult.ActionName);
    }

    [Fact]
    public void RemindRatee_ValidParticipantId_RedirectsToProcessReminders()
    {
        // Arrange
        var participantId = Guid.NewGuid();

        // Act
        var result = _controller.RemindRatee(participantId);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ProcessReminders", redirectResult.ActionName);
        Assert.NotNull(redirectResult.RouteValues);
    }

    [Fact]
    public void RemindSelectedRatees_NoParticipantsSelected_ReturnsViewWithError()
    {
        // Arrange
        var emptyList = new List<Guid>();

        // Act
        var result = _controller.RemindSelectedRatees(emptyList);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.False(_controller.ModelState.IsValid);
        Assert.True(_controller.ModelState.ContainsKey(""));
    }

    [Fact]
    public void RemindSelectedRatees_NullList_ReturnsViewWithError()
    {
        // Arrange & Act
        var result = _controller.RemindSelectedRatees(null);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.False(_controller.ModelState.IsValid);
    }

    [Fact]
    public async Task ProcessReminders_Success_RedirectsToViewReminders()
    {
        // Arrange
        var participantIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        
        _mockInvitationService
            .Setup(x => x.ProcessRemindersAsync(
                It.IsAny<List<Guid>>(), 
                It.IsAny<Guid>(), 
                It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.ProcessReminders(participantIds);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ViewReminders", redirectResult.ActionName);
        Assert.True(_controller.ModelState.IsValid);
    }

    [Fact]
    public async Task ProcessReminders_Failure_ReturnsViewWithError()
    {
        // Arrange
        var participantIds = new List<Guid> { Guid.NewGuid() };
        
        _mockInvitationService
            .Setup(x => x.ProcessRemindersAsync(
                It.IsAny<List<Guid>>(), 
                It.IsAny<Guid>(), 
                It.IsAny<string>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.ProcessReminders(participantIds);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.False(_controller.ModelState.IsValid);
        Assert.Contains(ErrorMessages.FailedToProcessReminders, 
            _controller.ModelState[""].Errors[0].ErrorMessage);
    }

    [Fact]
    public async Task SaveParticipant_ValidParticipant_CallsServiceAndRedirects()
    {
        // Arrange
        var participant = TestDataBuilder.CreateParticipant();
        var returnUrl = "ViewParticipants";
        
        _mockParticipantService
            .Setup(x => x.SaveParticipantAsync(It.IsAny<Participant>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.SaveParticipant(participant, returnUrl);

        // Assert
        _mockParticipantService.Verify(
            x => x.SaveParticipantAsync(participant), 
            Times.Once());
        
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("People", redirectResult.ActionName);
    }

    [Fact]
    public async Task SaveParticipant_ServiceFails_AddsModelError()
    {
        // Arrange
        var participant = TestDataBuilder.CreateParticipant();
        var returnUrl = "ViewParticipants";
        
        _mockParticipantService
            .Setup(x => x.SaveParticipantAsync(It.IsAny<Participant>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.SaveParticipant(participant, returnUrl);

        // Assert
        Assert.False(_controller.ModelState.IsValid);
        Assert.Contains(ErrorMessages.FailedToSaveParticipant,
            _controller.ModelState[""].Errors[0].ErrorMessage);
    }

    [Fact]
    public async Task SaveEvaluation_InvalidModel_ReturnsBadRequest()
    {
        // Arrange
        Evaluation? nullModel = null;
        
        // Act
        var result = await _controller.SaveEvaluation(nullModel, "selectedModel");

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(ErrorMessages.InvalidEvaluationData, badRequestResult.Value);
    }

    [Fact]
    public async Task SaveEvaluation_ValidModel_CallsServiceAndRedirects()
    {
        // Arrange
        var evaluation = TestDataBuilder.CreateEvaluation();
        
        _mockEvaluationService
            .Setup(x => x.SaveEvaluationAsync(It.IsAny<Evaluation>(), It.IsAny<Guid>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.SaveEvaluation(evaluation, "selectedModel");

        // Assert
        _mockEvaluationService.Verify(
            x => x.SaveEvaluationAsync(evaluation, It.IsAny<Guid>()), 
            Times.Once());
        
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ManageIndex", redirectResult.ActionName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task SaveClient_InvalidModel_ReturnsBadRequest(string? nullCheck)
    {
        // Arrange
        Client? model = nullCheck == null ? null : new Client();
        
        // Act
        var result = await _controller.SaveClient(model, "selectedModel");

        // Assert
        // Null model returns BadRequest, empty object redirects
        if (nullCheck == null)
        {
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(ErrorMessages.InvalidClientData, badRequestResult.Value);
        }
        else
        {
            Assert.IsType<RedirectToActionResult>(result);
        }
    }

    [Fact]
    public async Task RetryEmail_ValidId_CallsServiceAndRedirects()
    {
        // Arrange
        var emailId = 123;
        
        _mockEmailTemplateService
            .Setup(x => x.RetryEmailAsync(emailId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.RetryEmail(emailId);

        // Assert
        _mockEmailTemplateService.Verify(
            x => x.RetryEmailAsync(emailId), 
            Times.Once());
        
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(ActionNames.EmailQueueDashboard, redirectResult.ActionName);
    }

    [Fact]
    public async Task RetryEmail_EmailNotFound_ReturnsBadRequest()
    {
        // Arrange
        var emailId = 999;
        
        _mockEmailTemplateService
            .Setup(x => x.RetryEmailAsync(emailId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.RetryEmail(emailId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(ErrorMessages.EmailNotFoundOrSent, badRequestResult.Value);
    }

    [Fact]
    public async Task ActOnSurveyVersionUsageParticipant_Invite_CallsInvitationServiceAndReturnsPartial()
    {
        var evaluationId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var evaluationRateeId = Guid.NewGuid();
        var participantId = Guid.NewGuid();

        _mockInvitationService
            .Setup(x => x.SendAssignmentEmailsAsync(It.IsAny<List<(Guid EvaluationRateeId, Guid ParticipantId)>>(), It.IsAny<Guid>(), It.IsAny<string>(), false))
            .ReturnsAsync(1);

        _mockEvaluationService
            .Setup(x => x.GetSurveyVersionUsageAsync(evaluationId, versionId, It.IsAny<Guid>(), "started"))
            .ReturnsAsync(new SurveyVersionUsageViewModel
            {
                EvaluationId = evaluationId,
                SurveyDefinitionVersionId = versionId,
                EvaluationName = "Eval",
                SurveyDisplayName = "Survey v1",
                UsageType = "started",
                Rows = new() { new() { EvaluationRateeId = evaluationRateeId, RaterParticipantId = participantId } }
            });

        var result = await _controller.ActOnSurveyVersionUsageParticipant(
            evaluationId,
            versionId,
            "started",
            evaluationRateeId,
            participantId,
            "invite");

        Assert.IsType<PartialViewResult>(result);
        _mockInvitationService.Verify(
            x => x.SendAssignmentEmailsAsync(
                It.Is<List<(Guid EvaluationRateeId, Guid ParticipantId)>>(list => list.Count == 1 && list[0].EvaluationRateeId == evaluationRateeId && list[0].ParticipantId == participantId),
                It.IsAny<Guid>(),
                It.IsAny<string>(), false),
            Times.Once());
    }

    [Fact]
    public async Task ActOnSurveyVersionUsageParticipant_Remind_CallsReminderServiceAndReturnsPartial()
    {
        var evaluationId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var evaluationRateeId = Guid.NewGuid();
        var participantId = Guid.NewGuid();

        _mockInvitationService
            .Setup(x => x.SendAssignmentEmailsAsync(It.IsAny<List<(Guid EvaluationRateeId, Guid ParticipantId)>>(), It.IsAny<Guid>(), It.IsAny<string>(), true))
            .ReturnsAsync(1);

        _mockEvaluationService
            .Setup(x => x.GetSurveyVersionUsageAsync(evaluationId, versionId, It.IsAny<Guid>(), "submitted"))
            .ReturnsAsync(new SurveyVersionUsageViewModel
            {
                EvaluationId = evaluationId,
                SurveyDefinitionVersionId = versionId,
                EvaluationName = "Eval",
                SurveyDisplayName = "Survey v1",
                UsageType = "submitted",
                Rows = new() { new() { EvaluationRateeId = evaluationRateeId, RaterParticipantId = participantId } }
            });

        var result = await _controller.ActOnSurveyVersionUsageParticipant(
            evaluationId,
            versionId,
            "submitted",
            evaluationRateeId,
            participantId,
            "remind");

        Assert.IsType<PartialViewResult>(result);
        _mockInvitationService.Verify(
            x => x.SendAssignmentEmailsAsync(
                It.Is<List<(Guid EvaluationRateeId, Guid ParticipantId)>>(list => list.Count == 1 && list[0].EvaluationRateeId == evaluationRateeId && list[0].ParticipantId == participantId),
                It.IsAny<Guid>(),
                It.IsAny<string>(), true),
            Times.Once());
    }

    [Fact]
    public async Task ActOnSurveyVersionUsageBulk_Invite_CallsInvitationServiceForFilteredSet()
    {
        var evaluationId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var rateeId1 = Guid.NewGuid();
        var rateeId2 = Guid.NewGuid();
        var participantId1 = Guid.NewGuid();
        var participantId2 = Guid.NewGuid();

        _mockEvaluationService
            .Setup(x => x.GetSurveyVersionUsageAsync(evaluationId, versionId, It.IsAny<Guid>(), "started"))
            .ReturnsAsync(new SurveyVersionUsageViewModel
            {
                EvaluationId = evaluationId,
                SurveyDefinitionVersionId = versionId,
                EvaluationName = "Eval",
                SurveyDisplayName = "Survey v1",
                UsageType = "started",
                Rows = new List<SurveyVersionUsageRow>
                {
                    new() { EvaluationRateeId = rateeId1, RaterParticipantId = participantId1, RaterName = "A", RaterEmail = "a@test.com", RateeName = "R1", RoleName = "Peer", ParticipantStatusName = "Pending" },
                    new() { EvaluationRateeId = rateeId2, RaterParticipantId = participantId2, RaterName = "B", RaterEmail = "b@test.com", RateeName = "R2", RoleName = "Peer", ParticipantStatusName = "Pending" }
                }
            });

        _mockInvitationService
            .Setup(x => x.SendAssignmentEmailsAsync(It.IsAny<List<(Guid EvaluationRateeId, Guid ParticipantId)>>(), It.IsAny<Guid>(), It.IsAny<string>(), false))
            .ReturnsAsync(2);

        var result = await _controller.ActOnSurveyVersionUsageBulk(evaluationId, versionId, "started", "invite");

        Assert.IsType<PartialViewResult>(result);
        _mockInvitationService.Verify(
            x => x.SendAssignmentEmailsAsync(
                It.Is<List<(Guid EvaluationRateeId, Guid ParticipantId)>>(list =>
                    list.Count == 2 &&
                    list.Any(item => item.EvaluationRateeId == rateeId1 && item.ParticipantId == participantId1) &&
                    list.Any(item => item.EvaluationRateeId == rateeId2 && item.ParticipantId == participantId2)),
                It.IsAny<Guid>(),
                It.IsAny<string>(), false),
            Times.Once());
    }

    [Fact]
    public async Task ActOnSurveyVersionUsageBulk_Remind_CallsReminderServiceForFilteredSet()
    {
        var evaluationId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var participantId1 = Guid.NewGuid();
        var participantId2 = Guid.NewGuid();

        _mockEvaluationService
            .Setup(x => x.GetSurveyVersionUsageAsync(evaluationId, versionId, It.IsAny<Guid>(), "submitted"))
            .ReturnsAsync(new SurveyVersionUsageViewModel
            {
                EvaluationId = evaluationId,
                SurveyDefinitionVersionId = versionId,
                EvaluationName = "Eval",
                SurveyDisplayName = "Survey v1",
                UsageType = "submitted",
                Rows = new List<SurveyVersionUsageRow>
                {
                    new() { EvaluationRateeId = Guid.NewGuid(), RaterParticipantId = participantId1, RaterName = "A", RaterEmail = "a@test.com", RateeName = "R1", RoleName = "Peer", ParticipantStatusName = "Submitted" },
                    new() { EvaluationRateeId = Guid.NewGuid(), RaterParticipantId = participantId2, RaterName = "B", RaterEmail = "b@test.com", RateeName = "R2", RoleName = "Peer", ParticipantStatusName = "Submitted" }
                }
            });

        _mockInvitationService
            .Setup(x => x.SendAssignmentEmailsAsync(It.IsAny<List<(Guid EvaluationRateeId, Guid ParticipantId)>>(), It.IsAny<Guid>(), It.IsAny<string>(), true))
            .ReturnsAsync(2);

        var result = await _controller.ActOnSurveyVersionUsageBulk(evaluationId, versionId, "submitted", "remind");

        Assert.IsType<PartialViewResult>(result);
        _mockInvitationService.Verify(
            x => x.SendAssignmentEmailsAsync(
                It.Is<List<(Guid EvaluationRateeId, Guid ParticipantId)>>(list =>
                    list.Count == 2 &&
                    list.Any(target => target.ParticipantId == participantId1) &&
                    list.Any(target => target.ParticipantId == participantId2)),
                It.IsAny<Guid>(),
                It.IsAny<string>(), true),
            Times.Once());
    }

    [Fact]
    public async Task CancelEmail_Success_RedirectsToEmailQueueDashboard()
    {
        // Arrange
        var emailId = 456;
        
        _mockEmailTemplateService
            .Setup(x => x.CancelEmailAsync(emailId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.CancelEmail(emailId);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(ActionNames.EmailQueueDashboard, redirectResult.ActionName);
    }

    [Fact]
    public async Task ActOnSurveyVersionUsageBulk_EmptyTargets_DoesNotSendEmails()
    {
        var evaluationId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        _mockEvaluationService
            .Setup(service => service.GetSurveyVersionUsageAsync(evaluationId, versionId, It.IsAny<Guid>(), "started"))
            .ReturnsAsync(new SurveyVersionUsageViewModel
            {
                EvaluationId = evaluationId,
                SurveyDefinitionVersionId = versionId,
                UsageType = "started"
            });

        await _controller.ActOnSurveyVersionUsageBulk(evaluationId, versionId, "started", "invite");

        _mockInvitationService.Verify(service => service.ProcessInvitationsAsync(
            It.IsAny<List<(Guid EvaluationRateeId, Guid ParticipantId)>>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Never());
        _mockInvitationService.Verify(service => service.SendAssignmentEmailsAsync(
            It.IsAny<List<(Guid EvaluationRateeId, Guid ParticipantId)>>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SaveSurveyVersionDraft_Failure_PreservesSubmittedTextOnlyForUnpublishedVersions(bool published)
    {
        var evaluationId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        const string submitted = "{invalid JSON, keep my edits";
        var model = new SurveyVersionDetailViewModel
        {
            EvaluationId = evaluationId, SurveyDefinitionVersionId = versionId,
            IsPublished = published, DraftJson = "stored content"
        };
        _mockEvaluationService
            .Setup(service => service.SaveSurveyVersionDraftAsync(versionId, It.IsAny<Guid>(), submitted, It.IsAny<string>()))
            .ReturnsAsync(new SurveyActionResult(false, "Invalid JSON", "danger"));
        _mockEvaluationService
            .Setup(service => service.GetSurveyVersionDetailAsync(evaluationId, versionId, It.IsAny<Guid>()))
            .ReturnsAsync(model);

        var result = Assert.IsType<PartialViewResult>(await _controller.SaveSurveyVersionDraft(evaluationId, versionId, submitted));

        Assert.Equal(published ? "stored content" : submitted, Assert.IsType<SurveyVersionDetailViewModel>(result.Model).DraftJson);
    }

    [Fact]
    public async Task ActOnSurveyVersionUsageParticipant_AssignmentOutsideSelection_IsRejected()
    {
        var evaluationId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        _mockEvaluationService
            .Setup(service => service.GetSurveyVersionUsageAsync(evaluationId, versionId, It.IsAny<Guid>(), "started"))
            .ReturnsAsync(new SurveyVersionUsageViewModel());

        var result = await _controller.ActOnSurveyVersionUsageParticipant(evaluationId, versionId, "started", Guid.NewGuid(), Guid.NewGuid(), "invite");

        Assert.IsType<BadRequestObjectResult>(result);
        _mockInvitationService.Verify(service => service.SendAssignmentEmailsAsync(
            It.IsAny<List<(Guid EvaluationRateeId, Guid ParticipantId)>>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task PublishSurveyVersion_PassesSubmittedDraftToAtomicPublication(bool saveSucceeds)
    {
        var evaluationId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        const string draftJson = "{\"pages\":[]}";
        _mockEvaluationService
            .Setup(service => service.PublishSurveyVersionAsync(versionId, It.IsAny<Guid>(), It.IsAny<string>(), draftJson))
            .ReturnsAsync(new SurveyActionResult(saveSucceeds, "Publish result", saveSucceeds ? "success" : "danger"));
        _mockEvaluationService
            .Setup(service => service.GetSurveyVersionDetailAsync(evaluationId, versionId, It.IsAny<Guid>()))
            .ReturnsAsync(new SurveyVersionDetailViewModel
            {
                EvaluationId = evaluationId,
                SurveyDefinitionVersionId = versionId
            });

        var result = await _controller.PublishSurveyVersion(evaluationId, versionId, draftJson);

        Assert.IsType<PartialViewResult>(result);
        _mockEvaluationService.Verify(service => service.SaveSurveyVersionDraftAsync(
            versionId, It.IsAny<Guid>(), draftJson, It.IsAny<string>()), Times.Never());
        _mockEvaluationService.Verify(service => service.PublishSurveyVersionAsync(
            versionId, It.IsAny<Guid>(), It.IsAny<string>(), draftJson), Times.Once());
        if (!saveSucceeds)
        {
            Assert.Equal(draftJson, Assert.IsType<SurveyVersionDetailViewModel>(Assert.IsType<PartialViewResult>(result).Model).DraftJson);
        }
    }
}
