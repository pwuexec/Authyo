using System.Net;
using Authy.Presentation.Domain;
using Authy.Presentation.Domain.Organizations;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Authy.Tests;

public class CreateOrganizationCommandHandlerTests
{
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Mock<IOrganizationRepository> _repositoryMock;
    private readonly IConfiguration _configuration;

    public CreateOrganizationCommandHandlerTests()
    {
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _repositoryMock = new Mock<IOrganizationRepository>();

        var ip = "127.0.0.1";
        var inMemorySettings = new Dictionary<string, string> {
            {"RootIps:0", ip}
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();
    }

    [Fact]
    public async Task HandleAsync_ShouldCreateOrganization_WhenIpIsAllowed()
    {
        // Arrange
        var ip = "127.0.0.1";
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse(ip);

        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(context);

        var handler = new CreateOrganizationCommandHandler(
            _httpContextAccessorMock.Object,
            _configuration,
            _repositoryMock.Object
        );

        var command = new CreateOrganizationCommand("Test Org");

        // Act
        var result = await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Test Org", result.Value.Name);
        Assert.NotEqual(Guid.Empty, result.Value.Id);

        // Verify Version 7
        Assert.Equal(7, result.Value.Id.Version);
    }
}
