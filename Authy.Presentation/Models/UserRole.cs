namespace Authy.Presentation.Models;

public class UserRole
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    public DateTime AssignedAt { get; set; }

    public required User User { get; set; }
    public required Role Role { get; set; }
}
