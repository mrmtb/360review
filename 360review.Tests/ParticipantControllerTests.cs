using Moq;
using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using reviews.Controllers.ParticipantController;
using reviews.Models;
using reviews.Services;
using reviews.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class ParticipantControllerTests
{
    private readonly Mock<IParticipantService> _mockParticipantService;
    private readonly Mock<IInvitationService> _mockInvitationService;
    private readonly Mock<ISurveyUrlBuilder> _mockSurveyUrlBuilder;
    private readonly Mock<ISurveyCompletionService> _mockSurveyCompletionService;
    private readonly Mock<IReviewSessionService> _mockReviewSessionService;
    private readonly Mock<ISessionInitializationService> _mockSessionInitService;
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly Mock<ILogger<ParticipantController>> _mockLogger;
    private readonly ParticipantController _controller;

    public ParticipantControllerTests()
    {
        _mockParticipantService = new Mock<IParticipantService>();
        _mockInvitationService = new Mock<IInvitationService>();
        _mockSurveyUrlBuilder = new Mock<ISurveyUrlBuilder>();
        _mockSurveyCompletionService = new Mock<ISurveyCompletionService>();
        _mockReviewSessionService = new Mock<IReviewSessionService>();
        _mockSessionInitService = new Mock<ISessionInitializationService>();
        _mockUserManager = MockHelpers.MockUserManager<ApplicationUser>();
        _mockLogger = new Mock<ILogger<ParticipantController>>();

        _controller = new ParticipantController(
            _mockParticipantService.Object,
            _mockInvitationService.Object,
            _mockSurveyUrlBuilder.Object,
            _mockSurveyCompletionService.Object,
            _mockReviewSessionService.Object,
            _mockSessionInitService.Object,
            _mockUserManager.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public void ParticipantController_CanBeInstantiated()
    {
        // Assert - controller was successfully created in constructor
        Assert.NotNull(_controller);
    }
}