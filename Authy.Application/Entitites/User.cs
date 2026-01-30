namespace Authy.Application.Entitites;

public class User 
{
    public Guid Id { get; set; }
    public string Name { get; set; }

    // Navigation properties
    public ICollection<Role> Roles { get; set; } = new List<Role>();

    // Foreign Key to Organization
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; }
}
