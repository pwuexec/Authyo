namespace Authy.Presentation.Entitites;

public class RefreshToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public string JwtId { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; }
    public DateTime ExpiresOn { get; set; }
    public DateTime? RevokedOn { get; set; }
    public Guid? ReplacedByTokenId { get; set; }
    public string UserAgent { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    
    public bool IsRevoked => RevokedOn != null;
    
    public bool IsExpired(DateTime utcNow) => utcNow >= ExpiresOn;

    public bool IsActive(DateTime utcNow) => !IsRevoked && !IsExpired(utcNow);
}
