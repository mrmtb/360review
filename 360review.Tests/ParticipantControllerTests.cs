using Moq;
using Xunit;
using reviews.Controllers.ParticipantController;
using reviews.Services;
using Microsoft.Extensions.Logging;

public class ParticipantControllerTests
{
    private readonly Mock<IParticipantService> _mockParticipantService;
    private readonly Mock<IInvitationService> _mockInvitationService;
    private readonly Mock<ISurveyUrlBuilder> _mockSurveyUrlBuilder;
    private readonly Mock<ISurveyCompletionService> _mockSurveyCompletionService;
    private readonly Mock<IReviewSessionService> _mockReviewSessionService;
    private readonly Mock<ILogger<ParticipantController>> _mockLogger;
    private readonly ParticipantController _controller;

    public ParticipantControllerTests()
    {
        _mockParticipantService = new Mock<IParticipantService>();
        _mockInvitationService = new Mock<IInvitationService>();
        _mockSurveyUrlBuilder = new Mock<ISurveyUrlBuilder>();
        _mockSurveyCompletionService = new Mock<ISurveyCompletionService>();
        _mockReviewSessionService = new Mock<IReviewSessionService>();
        _mockLogger = new Mock<ILogger<ParticipantController>>();

        _controller = new ParticipantController(
            _mockParticipantService.Object,
            _mockInvitationService.Object,
            _mockSurveyUrlBuilder.Object,
            _mockSurveyCompletionService.Object,
            _mockReviewSessionService.Object,
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
