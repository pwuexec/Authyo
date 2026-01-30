namespace Authy.Admin.Services;

public interface IAdminContext
{
    bool IsRootUser { get; }
    Guid? CurrentUserId { get; }
}
