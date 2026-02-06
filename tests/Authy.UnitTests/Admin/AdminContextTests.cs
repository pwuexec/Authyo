using System.Net;
using Authy.Admin.Services;
using Authy.Application.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Authy.UnitTests.Admin;

[TestClass]
public class AdminContextTests
{
    private IHttpContextAccessor _httpContextAccessor = null!;
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
        var options = Substitute.For<IOptions<RootIpOptions>>();
        options.Value.Returns(new RootIpOptions { RootIps = rootIps });

        return new AdminContext(_httpContextAccessor, options);
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
