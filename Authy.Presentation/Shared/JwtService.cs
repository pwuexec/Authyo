using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Authy.Presentation.Shared.Abstractions;
using Microsoft.IdentityModel.Tokens;

namespace Authy.Presentation.Shared;

public class JwtService(IConfiguration configuration) : IJwtService
{
    public string GenerateToken(Guid userId, string userName, List<string> scopes)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, userName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        foreach (var scope in scopes)
        {
            claims.Add(new Claim("scope", scope));
        }

        var expirationInMinutes = configuration.GetValue<int>("Jwt:ExpirationInMinutes");

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationInMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
