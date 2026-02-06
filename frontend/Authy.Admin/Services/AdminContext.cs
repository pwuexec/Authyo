using Authy.Application.Extensions;
using Authy.Application.Shared;
using Microsoft.Extensions.Options;

namespace Authy.Admin.Services;

public class AdminContext : IAdminContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IOptions<RootIpOptions> _rootIpOptions;

    public AdminContext(IHttpContextAccessor httpContextAccessor, IOptions<RootIpOptions> rootIpOptions)
    {
        _httpContextAccessor = httpContextAccessor;
        _rootIpOptions = rootIpOptions;
    }

    public bool IsRootUser
    {
        get
        {
            var result = _httpContextAccessor.HttpContext.EnsureRootIp(_rootIpOptions.Value.RootIps);
            return result.IsSuccess;
        }
    }

    public Guid? CurrentUserId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            return user?.GetUserId();
        }
    }
}

