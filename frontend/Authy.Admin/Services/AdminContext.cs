using Authy.Application.Extensions;

namespace Authy.Admin.Services;

public class AdminContext : IAdminContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;

    public AdminContext(IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
    {
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
    }

    public bool IsRootUser
    {
        get
        {
            var result = _httpContextAccessor.HttpContext.EnsureRootIp(_configuration);
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

