using Authy.Application.Domain;
using Authy.Application.Domain.Organizations;
using Authy.Application.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Authy.Application.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Authy.UnitTests.Domain.Organizations;

[TestClass]
public class CreateOrganizationCommandHandlerTests : TestBase
{
    private readonly IHttpContextAccessor _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
    private readonly IOptions<RootIpOptions> _rootIpOptions = Substitute.For<IOptions<RootIpOptions>>();
    private OrganizationRepository _organizationRepository = null!;
    private CreateOrganizationCommandHandler _handler = null!;

    public CreateOrganizationCommandHandlerTests()
    {
        const string localIp = "127.0.0.1";
        _rootIpOptions.Value.Returns(new RootIpOptions { RootIps = [localIp] });
    }

    [TestInitialize]
    public override void Setup()
    {
        base.Setup();
        _organizationRepository = new OrganizationRepository(DbContext);
        _handler = new CreateOrganizationCommandHandler(_httpContextAccessor, _rootIpOptions, _organizationRepository);
    }

    [TestMethod]
    public async Task HandleAsync_Should_ReturnFailure_When_NameIsEmpty()
    {
        // Arrange
        var command = new CreateOrganizationCommand("");

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken);

        // Assert
        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(DomainErrors.Organization.NameEmpty, result.Error);
    }

    [TestMethod]
    public async Task HandleAsync_Should_CreateOrganization_When_Valid()
    {
        // Arrange
        const string newOrgName = "New Org";
        const string localIp = "127.0.0.1";

        var command = new CreateOrganizationCommand(newOrgName);

        // Mock HttpContext for EnsureRootIp checking
        var httpContext = new DefaultHttpContext
        {
            Connection =
            {
                RemoteIpAddress = System.Net.IPAddress.Parse(localIp)
            }
        };
        _httpContextAccessor.HttpContext.Returns(httpContext);

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(newOrgName, result.Value.Name);

        // Verify DB State
        var orgInDb =
            await DbContext.Organizations.FirstOrDefaultAsync(o => o.Name == newOrgName, TestContext.CancellationToken);
        Assert.IsNotNull(orgInDb);
    }
}

