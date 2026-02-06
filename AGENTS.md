# AI Agent Guidelines for Authy Project

This document outlines the standards and conventions for AI agents working on the Authy codebase, specifically regarding testing.

## Testing Standards

### Frameworks & Tools
*   **Test Framework:** MSTest
*   **Mocking:** NSubstitute
*   **Database:** EF Core In-Memory (configured via `TestBase`)
*   **Time Abstraction:** `Microsoft.Extensions.Time.Testing.FakeTimeProvider`

### Naming Conventions
*   **Test Classes:** Must be named `[SourceClassName]Tests`.
    *   Example: `GenerateTokenCommandHandler` -> `GenerateTokenCommandHandlerTests`
*   **Test Methods:** Follow the pattern `[MethodName]_[ExpectedOutcome]_[Condition]`.
    *   Example: `HandleAsync_Should_ReturnFailure_When_UserNotFound`
    *   Example: `IsRootUser_ReturnsTrue_WhenIpIsInRootList`

### Structure & Organization
*   **Directory Structure:** The `tests/Authy.UnitTests` project structure must mirror the `Authy.Application` project structure.
    *   Source: `Authy.Application/Domain/Users/GenerateTokenCommandHandler.cs`
    *   Test: `tests/Authy.UnitTests/Domain/Users/GenerateTokenCommandHandlerTests.cs`
*   **Test Body:** Follow the **AAA** (Arrange, Act, Assert) pattern. Comments for `// Arrange`, `// Act`, and `// Assert` are optional but encouraged for complex tests.

### Implementation Guidelines
*   **Database Tests:** Tests requiring a database context **must** inherit from `TestBase`.
    *   The `TestBase` class provides a `DbContext` property initialized with an in-memory database and a `TimeProvider`.
    *   Use `TestContext` property if needed, but primary setup is in `TestBase`.
    *   Override `Setup()` (decorated with `[TestInitialize]`) to initialize your subject under test (SUT) and dependencies. Call `base.Setup()` first.
*   **Mocking:** Use `NSubstitute` to mock interfaces (e.g., `IJwtService`, `IHttpContextAccessor`).
*   **Time-Dependent Tests:** Use the `TimeProvider` property from `TestBase` (or inject `FakeTimeProvider`) to control time deterministically.

### Example

```csharp
using Authy.Application.Domain.Users;
using Authy.UnitTests;
using NSubstitute;

namespace Authy.UnitTests.Domain.Users;

[TestClass]
public class ExampleTests : TestBase
{
    private ISomeDependency _dependency;
    private ExampleHandler _handler;

    [TestInitialize]
    public override void Setup()
    {
        base.Setup(); // Initializes DbContext and TimeProvider
        _dependency = Substitute.For<ISomeDependency>();
        _handler = new ExampleHandler(DbContext, _dependency, TimeProvider);
    }

    [TestMethod]
    public async Task Handle_Should_ReturnSuccess_When_Valid()
    {
        // Arrange
        var input = "valid";

        // Act
        var result = await _handler.Handle(input);

        // Assert
        Assert.IsTrue(result.IsSuccess);
    }
}
```
