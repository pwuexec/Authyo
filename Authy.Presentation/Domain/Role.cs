namespace Authy.Presentation.Domain;

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

public class Role
{
    public Guid Id { get; set; }
    public string Name { get; set; }

    // Foreign Key to Organization
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; }

    // The "Alias" logic: A role points to multiple scopes
    public ICollection<Scope> Scopes { get; set; } = new List<Scope>();
}

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