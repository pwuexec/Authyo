namespace Authy.Presentation.Entitites;

public class Organization
{
    public Guid Id { get; set; }
    public string Name { get; set; }

    // Navigation properties
    public ICollection<Role> Roles { get; set; } = new List<Role>();
    public ICollection<Scope> Scopes { get; set; } = new List<Scope>();

    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<User> Owners { get; set; } = new List<User>();
}