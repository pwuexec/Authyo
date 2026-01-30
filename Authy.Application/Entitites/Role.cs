namespace Authy.Application.Entitites;

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
