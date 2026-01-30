using System.Net;
using Authy.Admin.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using NSubstitute;

namespace Authy.UnitTests.Admin;

[TestClass]
public class AdminContextTests
{
    private IHttpContextAccessor _httpContextAccessor = null!;
    private IConfiguration _configuration = null!;
    private HttpContext _httpContext = null!;

    [TestInitialize]
    public void Setup()
    {
        _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        _httpContext = new DefaultHttpContext();
        _httpContextAccessor.HttpContext.Returns(_httpContext);
    }

    private AdminContext CreateAdminContext(string[] rootIps)
    {
        var configData = new Dictionary<string, string?>
        {
            { "RootIps:0", rootIps.Length > 0 ? rootIps[0] : null },
            { "RootIps:1", rootIps.Length > 1 ? rootIps[1] : null },
            { "RootIps:2", rootIps.Length > 2 ? rootIps[2] : null }
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData.Where(x => x.Value != null)!)
            .Build();

        return new AdminContext(_httpContextAccessor, _configuration);
    }

    [TestMethod]
    public void IsRootUser_ReturnsTrue_WhenIpIsInRootList_IPv4()
    {
        // Arrange
        var rootIps = new[] { "127.0.0.1", "192.168.1.1" };
        _httpContext.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");
        var adminContext = CreateAdminContext(rootIps);

        // Act
        var result = adminContext.IsRootUser;

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsRootUser_ReturnsTrue_WhenIpIsSecondInRootList()
    {
        // Arrange
        var rootIps = new[] { "127.0.0.1", "192.168.1.100" };
        _httpContext.Connection.RemoteIpAddress = IPAddress.Parse("192.168.1.100");
        var adminContext = CreateAdminContext(rootIps);

        // Act
        var result = adminContext.IsRootUser;

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsRootUser_ReturnsFalse_WhenIpIsNotInRootList()
    {
        // Arrange
        var rootIps = new[] { "127.0.0.1", "192.168.1.1" };
        _httpContext.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.1");
        var adminContext = CreateAdminContext(rootIps);

        // Act
        var result = adminContext.IsRootUser;

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsRootUser_ReturnsFalse_WhenRootIpsListIsEmpty()
    {
        // Arrange
        var rootIps = Array.Empty<string>();
        _httpContext.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");
        var adminContext = CreateAdminContext(rootIps);

        // Act
        var result = adminContext.IsRootUser;

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsRootUser_ReturnsFalse_WhenRemoteIpAddressIsNull()
    {
        // Arrange
        var rootIps = new[] { "127.0.0.1" };
        _httpContext.Connection.RemoteIpAddress = null;
        var adminContext = CreateAdminContext(rootIps);

        // Act
        var result = adminContext.IsRootUser;

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsRootUser_ReturnsFalse_WhenHttpContextIsNull()
    {
        // Arrange
        var rootIps = new[] { "127.0.0.1" };
        _httpContextAccessor.HttpContext.Returns((HttpContext?)null);
        var adminContext = CreateAdminContext(rootIps);

        // Act
        var result = adminContext.IsRootUser;

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsRootUser_ReturnsTrue_WhenIPv6LoopbackIsInRootList()
    {
        // Arrange
        var rootIps = new[] { "::1" };
        _httpContext.Connection.RemoteIpAddress = IPAddress.IPv6Loopback;
        var adminContext = CreateAdminContext(rootIps);

        // Act
        var result = adminContext.IsRootUser;

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsRootUser_ReturnsTrue_WhenIPv4MappedToIPv6AndMatchesRootList()
    {
        // Arrange - IPv4 mapped to IPv6 (::ffff:127.0.0.1)
        var rootIps = new[] { "127.0.0.1" };
        _httpContext.Connection.RemoteIpAddress = IPAddress.Parse("::ffff:127.0.0.1");
        var adminContext = CreateAdminContext(rootIps);

        // Act
        var result = adminContext.IsRootUser;

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void CurrentUserId_ReturnsNull_WhenNoUserAuthenticated()
    {
        // Arrange
        var rootIps = new[] { "127.0.0.1" };
        var adminContext = CreateAdminContext(rootIps);

        // Act
        var result = adminContext.CurrentUserId;

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void CurrentUserId_ReturnsNull_WhenHttpContextIsNull()
    {
        // Arrange
        var rootIps = new[] { "127.0.0.1" };
        _httpContextAccessor.HttpContext.Returns((HttpContext?)null);
        var adminContext = CreateAdminContext(rootIps);

        // Act
        var result = adminContext.CurrentUserId;

        // Assert
        Assert.IsNull(result);
    }
}
