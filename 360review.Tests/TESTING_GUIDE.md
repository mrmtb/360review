# Testing Guide for 360 Review Application

## Overview
This guide provides best practices and examples for testing the 360 Review application.

## Testing Philosophy

### The Testing Pyramid
```
        /\
       /  \      E2E Tests (Few)
      /____\
     /      \    Integration Tests (Some)
    /________\
   /          \  Unit Tests (Many)
  /____________\
```

**Unit Tests (70%)**: Fast, isolated tests for individual components
**Integration Tests (20%)**: Test interactions between components
**E2E Tests (10%)**: Full application testing through UI

## Test Organization

```
360review.Tests/
├── Unit/
│   ├── Services/           # Service layer tests
│   ├── Controllers/        # Controller tests
│   └── Models/            # Model/validation tests
├── Integration/
│   ├── Database/          # Database integration tests
│   └── Api/               # API endpoint tests
└── Helpers/               # Test utilities and fixtures
```

## Key Testing Principles

### 1. AAA Pattern (Arrange, Act, Assert)
```csharp
[Fact]
public async Task MethodName_Scenario_ExpectedBehavior()
{
    // Arrange - Set up test data and dependencies
    var mockService = new Mock<IService>();
    mockService.Setup(x => x.Method()).ReturnsAsync(data);
    
    // Act - Execute the method under test
    var result = await controller.Action();
    
    // Assert - Verify the expected outcome
    Assert.NotNull(result);
    Assert.Equal(expected, actual);
}
```

### 2. Test Naming Convention
```
MethodName_StateUnderTest_ExpectedBehavior
```

Examples:
- `SaveParticipant_ValidParticipant_ReturnsTrue`
- `SaveParticipant_NullParticipant_ThrowsArgumentNullException`
- `GetParticipantById_NonExistentId_ReturnsNull`

### 3. One Assertion Per Test (when possible)
Each test should verify one behavior. Multiple asserts are OK if they verify the same behavior.

### 4. Test Independence
Tests should not depend on each other and should be runnable in any order.

## Mocking Strategies

### Service Layer Testing
Services depend on `ApplicationDbContext` - use InMemory database or mock DbContext.

### Controller Testing
Controllers depend on services - mock the services, test the controller logic.

## What to Test

### ✅ DO Test:
- **Business logic** in services
- **Controller actions** (routing, model binding, responses)
- **Validation logic**
- **Error handling**
- **Edge cases** (null values, empty lists, etc.)
- **Authorization logic**

### ❌ DON'T Test:
- Framework code (ASP.NET Core, Entity Framework)
- Third-party libraries
- Simple getters/setters
- Trivial methods with no logic

## Test Data Builders

Create helper classes to generate test data:

```csharp
public class TestDataBuilder
{
    public static Participant CreateParticipant(
        string firstName = "John",
        string lastName = "Doe")
    {
        return new Participant
        {
            ParticipantId = Guid.NewGuid(),
            ParticipantNameFirst = firstName,
            ParticipantNameLast = lastName,
            ParticipantEmail = $"{firstName.ToLower()}.{lastName.ToLower()}@test.com",
            ClientId = Guid.NewGuid()
        };
    }
}
```

## Coverage Goals

- **Service Layer**: 80%+ coverage
- **Controllers**: 70%+ coverage
- **Models/DTOs**: Focus on validation logic

## Running Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true

# Run specific test
dotnet test --filter "FullyQualifiedName~ParticipantService"

# Run tests in parallel
dotnet test --parallel
```

## Common Testing Patterns

### 1. Testing Async Methods
```csharp
[Fact]
public async Task AsyncMethod_ShouldReturnExpectedValue()
{
    // Arrange
    var service = new Service();
    
    // Act
    var result = await service.AsyncMethod();
    
    // Assert
    Assert.NotNull(result);
}
```

### 2. Testing Exceptions
```csharp
[Fact]
public async Task Method_InvalidInput_ThrowsException()
{
    // Arrange
    var service = new Service();
    
    // Act & Assert
    await Assert.ThrowsAsync<ArgumentNullException>(
        () => service.Method(null));
}
```

### 3. Testing with Theory (Data-Driven Tests)
```csharp
[Theory]
[InlineData("test@test.com", true)]
[InlineData("invalid", false)]
[InlineData("", false)]
public void ValidateEmail_VariousInputs_ReturnsExpected(
    string email, bool expected)
{
    // Act
    var result = EmailValidator.IsValid(email);
    
    // Assert
    Assert.Equal(expected, result);
}
```

### 4. Testing with Mock Setup
```csharp
[Fact]
public async Task GetParticipant_ExistingId_ReturnsParticipant()
{
    // Arrange
    var participant = TestDataBuilder.CreateParticipant();
    
    _mockService
        .Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
        .ReturnsAsync(participant);
    
    // Act
    var result = await _controller.GetParticipant(participant.ParticipantId);
    
    // Assert
    var okResult = Assert.IsType<OkObjectResult>(result);
    var returnedParticipant = Assert.IsType<Participant>(okResult.Value);
    Assert.Equal(participant.ParticipantId, returnedParticipant.ParticipantId);
}
```

### 5. Verifying Method Calls
```csharp
[Fact]
public async Task DeleteParticipant_ValidId_CallsServiceDelete()
{
    // Arrange
    var id = Guid.NewGuid();
    
    // Act
    await _controller.DeleteParticipant(id);
    
    // Assert
    _mockService.Verify(
        x => x.DeleteAsync(id), 
        Times.Once());
}
```

## Integration Testing

For database integration tests, use:
1. **In-Memory Database**: Fast, but doesn't test SQL-specific features
2. **SQLite In-Memory**: Better SQL compatibility
3. **Test Containers**: Full database (PostgreSQL) in Docker

Example:
```csharp
public class DatabaseIntegrationTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    
    public DatabaseIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
            
        _context = new ApplicationDbContext(options);
    }
    
    [Fact]
    public async Task CanAddAndRetrieveParticipant()
    {
        // Arrange
        var participant = TestDataBuilder.CreateParticipant();
        
        // Act
        _context.Participants.Add(participant);
        await _context.SaveChangesAsync();
        
        var retrieved = await _context.Participants
            .FirstOrDefaultAsync(p => p.ParticipantId == participant.ParticipantId);
        
        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(participant.ParticipantEmail, retrieved.ParticipantEmail);
    }
    
    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
```

## CI/CD Integration

Add to your pipeline:
```yaml
- name: Run Tests
  run: dotnet test --logger "trx;LogFileName=test-results.trx"
  
- name: Test Coverage
  run: |
    dotnet test /p:CollectCoverage=true \
                /p:CoverletOutputFormat=cobertura \
                /p:CoverletOutput=./coverage/
```

## Next Steps

1. **Start with Service Tests**: Highest value, easiest to test
2. **Add Controller Tests**: Test HTTP concerns
3. **Integration Tests**: Test database operations
4. **Refactor for Testability**: Extract interfaces where needed

## Resources

- [xUnit Documentation](https://xunit.net/)
- [Moq Quickstart](https://github.com/moq/moq4/wiki/Quickstart)
- [ASP.NET Core Testing](https://docs.microsoft.com/en-us/aspnet/core/test/)
