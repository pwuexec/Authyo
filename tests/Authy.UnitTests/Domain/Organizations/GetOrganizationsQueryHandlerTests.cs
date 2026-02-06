using Authy.Application.Domain;
using Authy.Application.Domain.Organizations;
using Authy.Application.Entitites;
using Authy.Application.Data.Repositories;
using Authy.Application.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Authy.UnitTests.Domain.Organizations;

[TestClass]
public class GetOrganizationsQueryHandlerTests : TestBase
{
    private readonly IHttpContextAccessor _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
    private IOptions<RootIpOptions> _rootIpOptions = null!;
    private OrganizationRepository _organizationRepository = null!;
    private GetOrganizationsQueryHandler _handler = null!;

    [TestInitialize]
    public override void Setup()
    {
        base.Setup();
        
        const string localIp = "127.0.0.1";
        
        _rootIpOptions = Substitute.For<IOptions<RootIpOptions>>();
        _rootIpOptions.Value.Returns(new RootIpOptions { RootIps = [localIp] });

        _organizationRepository = new OrganizationRepository(DbContext);
        _handler = new GetOrganizationsQueryHandler(_organizationRepository, _httpContextAccessor, _rootIpOptions);
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


