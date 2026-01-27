using Authy.Presentation.Domain;
using Authy.Presentation.Domain.Organizations;
using Authy.Presentation.Entitites;
using Authy.Presentation.Persistence.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using NSubstitute;

namespace Authy.UnitTests.Domain.Organizations;

[TestClass]
public class GetOrganizationsQueryHandlerTests : TestBase
{
    private readonly IHttpContextAccessor _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
    private IConfiguration _configuration = null!;
    private OrganizationRepository _organizationRepository = null!;
    private GetOrganizationsQueryHandler _handler = null!;

    [TestInitialize]
    public override void Setup()
    {
        base.Setup();
        
        const string localIp = "127.0.0.1";
        const string rootIpConfigKey = "RootIps:0";
        
        var myConfiguration = new Dictionary<string, string?>
        {
            {rootIpConfigKey, localIp}
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(myConfiguration)
            .Build();

        _organizationRepository = new OrganizationRepository(DbContext);
        _handler = new GetOrganizationsQueryHandler(_organizationRepository, _httpContextAccessor, _configuration);
    }

    [TestMethod]
    public async Task HandleAsync_Should_ReturnFailure_When_IpUnauthorized()
    {
        // Arrange
        const string unauthorizedIp = "10.0.0.1";
        var query = new GetOrganizationsQuery();
        
        var httpContext = new DefaultHttpContext
        {
            Connection =
            {
                RemoteIpAddress = System.Net.IPAddress.Parse(unauthorizedIp)
            }
        };
        _httpContextAccessor.HttpContext.Returns(httpContext);

        // Act
        var result = await _handler.HandleAsync(query, CancellationToken);

        // Assert
        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(DomainErrors.User.UnauthorizedIp, result.Error);
    }
    
    [TestMethod]
    public async Task HandleAsync_Should_ReturnEmptyList_When_NoOrgsExist()
    {
        // Arrange
        const string localIp = "127.0.0.1";
        var query = new GetOrganizationsQuery();
        
        var httpContext = new DefaultHttpContext
        {
            Connection =
            {
                RemoteIpAddress = System.Net.IPAddress.Parse(localIp)
            }
        };
        _httpContextAccessor.HttpContext.Returns(httpContext);

        // Act
        var result = await _handler.HandleAsync(query, CancellationToken);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsEmpty(result.Value);
    }

    [TestMethod]
    public async Task HandleAsync_Should_ReturnOrgs_When_OrgsExist()
    {
        // Arrange
        const string orgName1 = "Org 1";
        const string orgName2 = "Org 2";
        const string localIp = "127.0.0.1";
        
        await DbContext.Organizations.AddRangeAsync(
            new Organization { Name = orgName1 },
            new Organization { Name = orgName2 }
        );
        await DbContext.SaveChangesAsync(TestContext.CancellationToken);

        var query = new GetOrganizationsQuery();
        
        var httpContext = new DefaultHttpContext
        {
            Connection =
            {
                RemoteIpAddress = System.Net.IPAddress.Parse(localIp)
            }
        };
        _httpContextAccessor.HttpContext.Returns(httpContext);

        // Act
        var result = await _handler.HandleAsync(query, CancellationToken);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.HasCount(2, result.Value);
        Assert.IsTrue(result.Value.Exists(o => o.Name == orgName1));
        Assert.IsTrue(result.Value.Exists(o => o.Name == orgName2));
    }
}
