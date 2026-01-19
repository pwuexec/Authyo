using System.Security.Cryptography;
using Authy.Presentation.Authorization;
using Authy.Presentation.Data;
using Authy.Presentation.Filters;
using Authy.Presentation.Models;
using Microsoft.EntityFrameworkCore;

namespace Authy.Presentation.Endpoints;

public static class OrganizationEndpoints
{
    public static void MapOrganizationEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/organizations");

        group.MapGet("/", ListOrganizations)
            .AddEndpointFilter<PlatformOwnerFilter>();

        group.MapGet("/{orgId:guid}", GetOrganization)
            .AuthorizeOrgAction(Scopes.OrganizationsRead);

        // Platform owner only - create/update organization
        group.MapPatch("/{orgId:guid}", PatchOrganization)
            .AddEndpointFilter<PlatformOwnerFilter>();

        group.MapDelete("/{orgId:guid}", DeleteOrganization)
            .AddEndpointFilter<PlatformOwnerFilter>();
    }

    private static async Task<IResult> ListOrganizations(AuthyDbContext db)
    {
        var organizations = await db.Organizations
            .Select(o => new OrganizationResponse(
                o.Id,
                o.Name,
                o.AllowSelfRegistration,
                o.CreatedAt))
            .ToListAsync();

        return Results.Ok(organizations);
    }

    private static async Task<IResult> GetOrganization(Guid orgId, AuthyDbContext db)
    {
        var organization = await db.Organizations.FindAsync(orgId);

        if (organization == null)
        {
            return Results.NotFound();
        }

        return Results.Ok(new OrganizationResponse(
            organization.Id,
            organization.Name,
            organization.AllowSelfRegistration,
            organization.CreatedAt));
    }

    private static async Task<IResult> PatchOrganization(
        Guid orgId,
        PatchOrganizationRequest request,
        AuthyDbContext db)
    {
        var existingOrg = await db.Organizations.FindAsync(orgId);

        bool isNew = existingOrg == null;

        if (isNew)
        {
            // Create new organization
            if (string.IsNullOrEmpty(request.Name))
            {
                return Results.BadRequest("Name is required for new organizations");
            }

            if (string.IsNullOrEmpty(request.AdminEmail) || string.IsNullOrEmpty(request.AdminPassword))
            {
                return Results.BadRequest("AdminEmail and AdminPassword are required for new organizations");
            }

            var adminUserId = Guid.NewGuid();

            existingOrg = new Organization
            {
                Id = orgId,
                Name = request.Name,
                AllowSelfRegistration = request.AllowSelfRegistration ?? false,
                CreatedByUserId = adminUserId,
                CreatedAt = DateTime.UtcNow
            };

            // Ensure all scopes exist in the database
            var scopeEntities = await EnsureScopesExist(db, existingOrg);

            // Create default roles for the organization
            var (defaultRoles, roleScopes) = DefaultRoles.CreateDefaultRoles(existingOrg, scopeEntities);
            var adminRole = defaultRoles.First(r => r.Name == "Admin");

            // Create initial admin user
            var adminUser = new User(existingOrg)
            {
                Id = adminUserId,
                OrganizationId = orgId,
                Email = request.AdminEmail,
                PasswordHash = HashPassword(request.AdminPassword),
                CreatedAt = DateTime.UtcNow
            };

            // Add admin as owner
            existingOrg.Owners.Add(adminUser);

            // Assign admin role to the user
            var userRole = new UserRole
            {
                UserId = adminUser.Id,
                RoleId = adminRole.Id,
                AssignedAt = DateTime.UtcNow,
                User = adminUser,
                Role = adminRole
            };

            db.Organizations.Add(existingOrg);
            db.Roles.AddRange(defaultRoles);
            db.RoleScopes.AddRange(roleScopes);
            db.Users.Add(adminUser);
            db.UserRoles.Add(userRole);
        }
        else
        {
            // Update existing organization
            if (!string.IsNullOrEmpty(request.Name))
            {
                existingOrg.Name = request.Name;
            }

            if (request.AllowSelfRegistration.HasValue)
            {
                existingOrg.AllowSelfRegistration = request.AllowSelfRegistration.Value;
            }
        }

        await db.SaveChangesAsync();

        var response = new OrganizationResponse(
            existingOrg.Id,
            existingOrg.Name,
            existingOrg.AllowSelfRegistration,
            existingOrg.CreatedAt);

        return isNew
            ? Results.Created($"/organizations/{existingOrg.Id}", response)
            : Results.Ok(response);
    }

    private static async Task<IResult> DeleteOrganization(Guid orgId, AuthyDbContext db)
    {
        var organization = await db.Organizations.FindAsync(orgId);

        if (organization == null)
        {
            return Results.NotFound();
        }

        db.Organizations.Remove(organization);
        await db.SaveChangesAsync();

        return Results.NoContent();
    }

    private static async Task<Dictionary<string, Scope>> EnsureScopesExist(AuthyDbContext db, Organization organization)
    {
        var existingScopes = await db.Scopes
            .Where(s => s.OrganizationId == organization.Id)
            .ToDictionaryAsync(s => s.Name);

        foreach (var scopeName in Scopes.All)
        {
            if (!existingScopes.ContainsKey(scopeName))
            {
                var scope = new Scope
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organization.Id,
                    Name = scopeName,
                    Organization = organization
                };
                db.Scopes.Add(scope);
                existingScopes[scopeName] = scope;
            }
        }

        await db.SaveChangesAsync();
        return existingScopes;
    }

    private static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100000, HashAlgorithmName.SHA256, 32);
        return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }
}

public record PatchOrganizationRequest(
    string? Name,
    bool? AllowSelfRegistration,
    string? AdminEmail,
    string? AdminPassword);

public record OrganizationResponse(
    Guid Id,
    string Name,
    bool AllowSelfRegistration,
    DateTime CreatedAt);
