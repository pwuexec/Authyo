namespace Authy.Presentation.Entitites;

public class Scope
{
    public Guid Id { get; set; }
    public string Name { get; set; }

    // Foreign Key to Organization
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; }

    // Many-to-many back-reference
    public ICollection<Role> Roles { get; set; } = new List<Role>();
}