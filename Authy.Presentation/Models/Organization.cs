namespace Authy.Presentation.Models;

public class Organization
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public bool AllowSelfRegistration { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    
    public ICollection<User> Owners { get; set; } = [];

    public ICollection<User> Users { get; set; } = [];
    public ICollection<Role> Roles { get; set; } = [];
}
