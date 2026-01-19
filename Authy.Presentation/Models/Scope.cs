namespace Authy.Presentation.Models;

public class Scope
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }

    public Organization Organization { get; set; } = null!;
    public ICollection<RoleScope> RoleScopes { get; set; } = [];
}
