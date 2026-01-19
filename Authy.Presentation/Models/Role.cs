namespace Authy.Presentation.Models;

public class Role
{
    public Role(Organization organization)
    {
        Organization = organization;
    }

    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }

    public Organization Organization { get; set; }
    public ICollection<UserRole> UserRoles { get; set; } = [];
    public ICollection<RoleScope> RoleScopes { get; set; } = [];
}
