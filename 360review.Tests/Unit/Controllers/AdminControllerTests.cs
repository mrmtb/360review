using Moq;
using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using reviews.Controllers.Admin;
using reviews.Models;
using reviews.Services;
using reviews.Tests.Helpers;
using reviews.Constants;

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
    private readonly Mock<ISessionInitializationService> _mockSessionInitService;
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
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
        _mockSessionInitService = new Mock<ISessionInitializationService>();
        _mockUserManager = MockHelpers.MockUserManager<ApplicationUser>();
        _mockLogger = new Mock<ILogger<AdminController>>();

        _controller = new AdminController(
            _mockParticipantService.Object,
            _mockEvaluationService.Object,
            _mockInvitationService.Object,
            _mockClientService.Object,
            _mockEmailTemplateService.Object,
            _mockReviewSessionService.Object,
            _mockSessionInitService.Object,
            _mockUserManager.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task ViewParticipants_ReturnsViewWithParticipants()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var participants = TestDataBuilder.CreateAdminParticipantList(3);
        
        _mockParticipantService
            .Setup(x => x.GetParticipantsByClientAsync(It.IsAny<Guid>()))
            .ReturnsAsync(participants);

        // Act
        var result = await _controller.ViewParticipants();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<List<vwAdminParticipants>>(viewResult.Model);
        Assert.Equal(3, model.Count);
    }

    [Fact]
    public async Task ViewParticipants_ServiceReturnsEmpty_ReturnsEmptyView()
    {
        // Arrange
        _mockParticipantService
            .Setup(x => x.GetParticipantsByClientAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new List<vwAdminParticipants>());

        // Act
        var result = await _controller.ViewParticipants();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<List<vwAdminParticipants>>(viewResult.Model);
        Assert.Empty(model);
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
        Assert.Equal(returnUrl, redirectResult.ActionName);
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
}
