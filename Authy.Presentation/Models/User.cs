namespace Authy.Presentation.Models;

public class User
{
    public User(Organization organization)
    {
        Organization = organization;
    }

    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public DateTime CreatedAt { get; set; }

    public Organization Organization { get; set; }
    public ICollection<UserRole> UserRoles { get; set; } = [];
}
