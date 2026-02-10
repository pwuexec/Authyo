using Authy.Application.Extensions;
using Authy.Application.Shared;
using Microsoft.Extensions.Options;

namespace Authy.Admin.Services;

public class AdminContext(IHttpContextAccessor httpContextAccessor, IOptions<RootIpOptions> rootIpOptions)
    : IAdminContext
{
    public bool IsRootUser
    {
        get
        {
            var result = httpContextAccessor.HttpContext.EnsureRootIp(rootIpOptions.Value.RootIps);
            return result.IsSuccess;
        }
    }

    public Guid? CurrentUserId
    {
        get
        {
            var user = httpContextAccessor.HttpContext?.User;
            return user?.GetUserId();
        }
    }
}

