using Authy.Presentation.Filters;
using Authy.Presentation.Authorization;
using Authy.Presentation.Data;
using Authy.Presentation.Models;
using Microsoft.EntityFrameworkCore;

namespace Authy.Presentation.Endpoints;

public static class RoleEndpoints
{
    public static void MapRoleEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/organizations/{orgId:guid}/roles");

        group.MapGet("/", ListRoles)
            .AuthorizeOrgAction(Scopes.RolesRead);
        group.MapGet("/{roleId:guid}", GetRole)
            .AuthorizeOrgAction(Scopes.RolesRead);
        group.MapPatch("/{roleId:guid}", PatchRole)
            .AuthorizeOrgAction(Scopes.RolesEdit);
        group.MapDelete("/{roleId:guid}", DeleteRole)
            .AuthorizeOrgAction(Scopes.RolesDelete);
    }

    private static async Task<IResult> ListRoles(Guid orgId, AuthyDbContext db)
    {
        var organization = await db.Organizations.FindAsync(orgId);

        if (organization == null)
        {
            return Results.NotFound("Organization not found");
        }

        var roles = await db.Roles
            .Where(r => r.OrganizationId == orgId)
            .Include(r => r.RoleScopes)
            .ThenInclude(rs => rs.Scope)
            .Select(r => new RoleResponse(
                r.Id,
                r.OrganizationId,
                r.Name,
                r.Description,
                r.RoleScopes.Select(rs => rs.Scope.Name).ToList(),
                r.CreatedAt))
            .ToListAsync();

        return Results.Ok(roles);
    }

    private static async Task<IResult> GetRole(Guid orgId, Guid roleId, AuthyDbContext db)
    {
        var role = await db.Roles
            .Include(r => r.RoleScopes)
            .ThenInclude(rs => rs.Scope)
            .FirstOrDefaultAsync(r => r.OrganizationId == orgId && r.Id == roleId);

        if (role == null)
        {
            return Results.NotFound();
        }

        var scopeNames = role.RoleScopes.Select(rs => rs.Scope.Name).ToList();

        return Results.Ok(new RoleResponse(
            role.Id,
            role.OrganizationId,
            role.Name,
            role.Description,
            scopeNames,
            role.CreatedAt));
    }

    private static async Task<IResult> PatchRole(
        Guid orgId,
        Guid roleId,
        PatchRoleRequest request,
        AuthyDbContext db)
    {
        var organization = await db.Organizations.FindAsync(orgId);

        if (organization == null)
        {
            return Results.NotFound("Organization not found");
        }

        var existingRole = await db.Roles
            .Include(r => r.RoleScopes)
            .FirstOrDefaultAsync(r => r.OrganizationId == orgId && r.Id == roleId);

        bool isNew = existingRole == null;

        if (isNew)
        {
            // Create new role
            if (string.IsNullOrEmpty(request.Name))
            {
                return Results.BadRequest("Name is required for new roles");
            }

            // Check if name already exists
            var nameExists = await db.Roles
                .AnyAsync(r => r.OrganizationId == orgId && r.Name == request.Name);

            if (nameExists)
            {
                return Results.Conflict("A role with this name already exists in this organization");
            }

            existingRole = new Role(organization)
            {
                Id = roleId,
                OrganizationId = orgId,
                Name = request.Name,
                Description = request.Description,
                CreatedAt = DateTime.UtcNow
            };

            db.Roles.Add(existingRole);
        }
        else
        {
            // Update existing role
            if (!string.IsNullOrEmpty(request.Name) && request.Name != existingRole.Name)
            {
                // Check if new name already exists
                var nameExists = await db.Roles
                    .AnyAsync(r => r.OrganizationId == orgId && r.Name == request.Name && r.Id != roleId);

                if (nameExists)
                {
                    return Results.Conflict("A role with this name already exists in this organization");
                }

                existingRole.Name = request.Name;
            }

            if (request.Description != null)
            {
                existingRole.Description = request.Description;
            }
        }

        // Handle scopes if specified
        if (request.Scopes != null)
        {
            // Validate scopes
            var invalidScopes = request.Scopes.Except(Scopes.All).ToList();
            if (invalidScopes.Count != 0)
            {
                return Results.BadRequest($"Invalid scopes: {string.Join(", ", invalidScopes)}");
            }

            // Get scope entities
            var scopeEntities = await db.Scopes
                .Where(s => request.Scopes.Contains(s.Name))
                .ToDictionaryAsync(s => s.Name);

            // Remove existing role scopes
            db.RoleScopes.RemoveRange(existingRole.RoleScopes);

            // Add new role scopes
            var newRoleScopes = request.Scopes
                .Where(scopeEntities.ContainsKey)
                .Select(scopeName => new RoleScope
                {
                    RoleId = existingRole.Id,
                    ScopeId = scopeEntities[scopeName].Id,
                    Role = existingRole,
                    Scope = scopeEntities[scopeName]
                })
                .ToList();

            db.RoleScopes.AddRange(newRoleScopes);
        }

        await db.SaveChangesAsync();

        // Reload scopes for response
        var scopeNames = await db.RoleScopes
            .Where(rs => rs.RoleId == existingRole.Id)
            .Select(rs => rs.Scope.Name)
            .ToListAsync();

        var response = new RoleResponse(
            existingRole.Id,
            existingRole.OrganizationId,
            existingRole.Name,
            existingRole.Description,
            scopeNames,
            existingRole.CreatedAt);

        return isNew
            ? Results.Created($"/organizations/{orgId}/roles/{existingRole.Id}", response)
            : Results.Ok(response);
    }

    private static async Task<IResult> DeleteRole(Guid orgId, Guid roleId, AuthyDbContext db)
    {
        var role = await db.Roles
            .FirstOrDefaultAsync(r => r.OrganizationId == orgId && r.Id == roleId);

        if (role == null)
        {
            return Results.NotFound();
        }

        db.Roles.Remove(role);
        await db.SaveChangesAsync();

        return Results.NoContent();
    }
}

public record PatchRoleRequest(string? Name, string? Description, List<string>? Scopes);

public record RoleResponse(
    Guid Id,
    Guid OrganizationId,
    string Name,
    string? Description,
    List<string> Scopes,
    DateTime CreatedAt);
