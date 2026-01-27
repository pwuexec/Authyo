using Authy.Presentation.Domain;
using Authy.Presentation.Domain.Organizations;
using Authy.Presentation.Persistence.Repositories;
using Authy.Presentation.Shared;
using Authy.Presentation.Shared.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using NSubstitute;

namespace Authy.UnitTests.Domain.Organizations;

[TestClass]
public class CreateOrganizationCommandHandlerTests : TestBase
{
    private readonly IHttpContextAccessor _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
    private readonly IConfiguration _configuration;
    private OrganizationRepository _organizationRepository = null!;
    private CreateOrganizationCommandHandler _handler = null!;

    public CreateOrganizationCommandHandlerTests()
    {
        const string localIp = "127.0.0.1";
        const string rootIpConfigKey = "RootIps:0";
        
        // Use real configuration for easier array binding
        var myConfiguration = new Dictionary<string, string?>
        {
            {rootIpConfigKey, localIp}
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(myConfiguration)
            .Build();
    }
    
    [TestInitialize]
    public override void Setup()
    {
        base.Setup();
        _organizationRepository = new OrganizationRepository(DbContext);
        _handler = new CreateOrganizationCommandHandler(_httpContextAccessor, _configuration, _organizationRepository);
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
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse(localIp);
        _httpContextAccessor.HttpContext.Returns(httpContext);

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(newOrgName, result.Value.Name);
        
        // Verify DB State
        var orgInDb = await DbContext.Organizations.FirstOrDefaultAsync(o => o.Name == newOrgName);
        Assert.IsNotNull(orgInDb);
    }
}
