namespace Authy.Presentation.Models;

public class RoleScope
{
    public Guid RoleId { get; set; }
    public Guid ScopeId { get; set; }

    public required Role Role { get; set; }
    public required Scope Scope { get; set; }
}
