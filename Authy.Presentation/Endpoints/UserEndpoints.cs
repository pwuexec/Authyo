using Authy.Presentation.Filters;
using System.Security.Cryptography;
using Authy.Presentation.Data;
using Authy.Presentation.Models;
using Microsoft.EntityFrameworkCore;
using Authy.Presentation.Authorization;

namespace Authy.Presentation.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/organizations/{orgId:guid}/users");

        group.MapGet("/", ListUsers)
            .AuthorizeOrgAction(Scopes.UsersRead);
        group.MapGet("/{userId:guid}", GetUser)
            .AuthorizeOrgAction(Scopes.UsersRead);
        group.MapPatch("/{userId:guid}", PatchUser)
            .AuthorizeOrgAction(Scopes.UsersEdit);
        group.MapDelete("/{userId:guid}", DeleteUser)
            .AuthorizeOrgAction(Scopes.UsersDelete);
    }

    private static async Task<IResult> ListUsers(Guid orgId, AuthyDbContext db)
    {
        var organization = await db.Organizations.FindAsync(orgId);

        if (organization == null)
        {
            return Results.NotFound("Organization not found");
        }

        var users = await db.Users
            .Where(u => u.OrganizationId == orgId)
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .Select(u => new UserResponse(
                u.Id,
                u.OrganizationId,
                u.Email,
                u.UserRoles.Select(ur => ur.Role.Name).ToList(),
                u.CreatedAt))
            .ToListAsync();

        return Results.Ok(users);
    }

    private static async Task<IResult> GetUser(Guid orgId, Guid userId, AuthyDbContext db)
    {
        var user = await db.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.OrganizationId == orgId && u.Id == userId);

        if (user == null)
        {
            return Results.NotFound();
        }

        var roleNames = user.UserRoles.Select(ur => ur.Role.Name).ToList();

        return Results.Ok(new UserResponse(
            user.Id,
            user.OrganizationId,
            user.Email,
            roleNames,
            user.CreatedAt));
    }

    private static async Task<IResult> PatchUser(
        Guid orgId,
        Guid userId,
        PatchUserRequest request,
        AuthyDbContext db)
    {
        var organization = await db.Organizations
            .Include(o => o.Roles)
            .FirstOrDefaultAsync(o => o.Id == orgId);

        if (organization == null)
        {
            return Results.NotFound("Organization not found");
        }

        var existingUser = await db.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.OrganizationId == orgId && u.Id == userId);

        bool isNew = existingUser == null;

        if (isNew)
        {
            // Create new user
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            {
                return Results.BadRequest("Email and Password are required for new users");
            }

            // Check if email already exists in this organization
            var emailExists = await db.Users
                .AnyAsync(u => u.OrganizationId == orgId && u.Email == request.Email);

            if (emailExists)
            {
                return Results.Conflict("A user with this email already exists in this organization");
            }

            existingUser = new User(organization)
            {
                Id = userId,
                OrganizationId = orgId,
                Email = request.Email,
                PasswordHash = HashPassword(request.Password),
                CreatedAt = DateTime.UtcNow
            };

            db.Users.Add(existingUser);
        }
        else
        {
            // Update existing user
            if (!string.IsNullOrEmpty(request.Email) && request.Email != existingUser.Email)
            {
                // Check if new email already exists
                var emailExists = await db.Users
                    .AnyAsync(u => u.OrganizationId == orgId && u.Email == request.Email && u.Id != userId);

                if (emailExists)
                {
                    return Results.Conflict("A user with this email already exists in this organization");
                }

                existingUser.Email = request.Email;
            }

            if (!string.IsNullOrEmpty(request.Password))
            {
                existingUser.PasswordHash = HashPassword(request.Password);
            }
        }

        // Handle roles if specified
        if (request.Roles != null)
        {
            // Remove existing roles
            db.UserRoles.RemoveRange(existingUser.UserRoles);

            // Add new roles
            foreach (var roleName in request.Roles)
            {
                var role = organization.Roles.FirstOrDefault(r => r.Name == roleName);
                if (role == null)
                {
                    return Results.BadRequest($"Role '{roleName}' not found in this organization");
                }

                db.UserRoles.Add(new UserRole
                {
                    UserId = existingUser.Id,
                    RoleId = role.Id,
                    AssignedAt = DateTime.UtcNow,
                    User = existingUser,
                    Role = role
                });
            }
        }
        else if (isNew)
        {
            // Assign default Member role for new users
            var memberRole = organization.Roles.FirstOrDefault(r => r.Name == "Member");
            if (memberRole != null)
            {
                db.UserRoles.Add(new UserRole
                {
                    UserId = existingUser.Id,
                    RoleId = memberRole.Id,
                    AssignedAt = DateTime.UtcNow,
                    User = existingUser,
                    Role = memberRole
                });
            }
        }

        await db.SaveChangesAsync();

        // Reload roles for response
        var roleNames = await db.UserRoles
            .Where(ur => ur.UserId == existingUser.Id)
            .Select(ur => ur.Role.Name)
            .ToListAsync();

        var response = new UserResponse(
            existingUser.Id,
            existingUser.OrganizationId,
            existingUser.Email,
            roleNames,
            existingUser.CreatedAt);

        return isNew
            ? Results.Created($"/organizations/{orgId}/users/{existingUser.Id}", response)
            : Results.Ok(response);
    }

    private static async Task<IResult> DeleteUser(Guid orgId, Guid userId, AuthyDbContext db)
    {
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.OrganizationId == orgId && u.Id == userId);

        if (user == null)
        {
            return Results.NotFound();
        }

        db.Users.Remove(user);
        await db.SaveChangesAsync();

        return Results.NoContent();
    }

    private static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100000, HashAlgorithmName.SHA256, 32);
        return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }
}

public record PatchUserRequest(string? Email, string? Password, List<string>? Roles);

public record UserResponse(
    Guid Id,
    Guid OrganizationId,
    string Email,
    List<string> Roles,
    DateTime CreatedAt);
