using Moq;
using Xunit;
using Microsoft.AspNetCore.Mvc;
using reviews.Controllers.ParticipantController;
using reviews.Models;
using Microsoft.AspNetCore.Identity;
using reviews.Services; // Add this or replace with the correct namespace for ReviewSessionService
using Microsoft.EntityFrameworkCore;

public class ParticipantControllerTests
{
    private readonly Mock<ApplicationDBContext> _mockContext;
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly Mock<ReviewSessionService> _mockReviewSessionService;
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly Mock<ILogger<ParticipantController>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<EmailQueueHelper> _mockEmailQueueHelper;
    private readonly ParticipantController _controller;

    public ParticipantControllerTests()
    {
        _mockContext = new Mock<ApplicationDBContext>();
        _mockUserManager = new Mock<UserManager<ApplicationUser>>(
            new Mock<IUserStore<ApplicationUser>>().Object //, null, null, null, null, null, null, null, null
        );
        _mockReviewSessionService = new Mock<ReviewSessionService>();
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _mockLogger = new Mock<ILogger<ParticipantController>>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockEmailQueueHelper = new Mock<EmailQueueHelper>();

        _controller = new ParticipantController(
            _mockContext.Object,
            _mockUserManager.Object,
            _mockReviewSessionService.Object,
            _mockHttpContextAccessor.Object,
            _mockLogger.Object,
            _mockConfiguration.Object,
            _mockEmailQueueHelper.Object
        );
    }

    [Fact]
    public void ParticipantIndex_ReturnsViewResult()
    {
        // Arrange
        var participants = new List<Participant>
        {
            new Participant { ParticipantId = Guid.NewGuid(), ParticipantNameFirst = "John", ParticipantNameLast = "Doe" },
            new Participant { ParticipantId = Guid.NewGuid(), ParticipantNameFirst = "Jane", ParticipantNameLast = "Smith" }
        }.AsQueryable();

        var mockSet = new Mock<DbSet<Participant>>();
        mockSet.As<IQueryable<Participant>>().Setup(m => m.Provider).Returns(participants.Provider);
        mockSet.As<IQueryable<Participant>>().Setup(m => m.Expression).Returns(participants.Expression);
        mockSet.As<IQueryable<Participant>>().Setup(m => m.ElementType).Returns(participants.ElementType);
        mockSet.As<IQueryable<Participant>>().Setup(m => m.GetEnumerator()).Returns(participants.GetEnumerator());

        _mockContext.Setup(c => c.Participants).Returns(mockSet.Object);

        // Act
        int? evaluationId = null; // or set to a valid int value if needed
        Guid? someGuid = null; // or set to a valid Guid value if needed
        var result = _controller.ParticipantIndex(evaluationId, someGuid);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<Participant>>(viewResult.Model);
        Assert.Equal(2, model.Count());
    }
}