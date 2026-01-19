using Authy.Presentation.Data;
using Authy.Presentation.Filters;
using Authy.Presentation.Models;
using Microsoft.EntityFrameworkCore;

namespace Authy.Presentation.Endpoints;

public static class ScopeEndpoints
{
    public static void MapScopeEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/organizations/{orgId:guid}/scopes");

        group.MapGet("/", ListScopes)
            .AuthorizeOrgAction(Authorization.Scopes.ScopesRead);
        group.MapPatch("/{scopeId:guid}", PatchScope)
            .AuthorizeOrgAction(Authorization.Scopes.ScopesEdit);
        group.MapDelete("/{scopeId:guid}", DeleteScope)
            .AuthorizeOrgAction(Authorization.Scopes.ScopesDelete);
    }

    private static async Task<IResult> ListScopes(Guid orgId, AuthyDbContext db)
    {
        var organization = await db.Organizations.FindAsync(orgId);

        if (organization == null)
        {
            return Results.NotFound("Organization not found");
        }

        var scopes = await db.Scopes
            .Where(s => s.OrganizationId == orgId)
            .Select(s => new ScopeResponse(
                s.Id,
                s.OrganizationId,
                s.Name,
                s.Description))
            .ToListAsync();

        return Results.Ok(scopes);
    }

    private static async Task<IResult> PatchScope(
        Guid orgId,
        Guid scopeId,
        PatchScopeRequest request,
        AuthyDbContext db)
    {
        var organization = await db.Organizations.FindAsync(orgId);

        if (organization == null)
        {
            return Results.NotFound("Organization not found");
        }

        var existingScope = await db.Scopes
            .FirstOrDefaultAsync(s => s.OrganizationId == orgId && s.Id == scopeId);

        bool isNew = existingScope == null;

        if (isNew)
        {
            if (string.IsNullOrEmpty(request.Name))
            {
                return Results.BadRequest("Name is required for new scopes");
            }

            // Check if name already exists in this organization
            var nameExists = await db.Scopes
                .AnyAsync(s => s.OrganizationId == orgId && s.Name == request.Name);

            if (nameExists)
            {
                return Results.Conflict("A scope with this name already exists in this organization");
            }

            existingScope = new Scope
            {
                Id = scopeId,
                OrganizationId = orgId,
                Name = request.Name,
                Description = request.Description,
                Organization = organization
            };

            db.Scopes.Add(existingScope);
        }
        else
        {
            if (!string.IsNullOrEmpty(request.Name) && request.Name != existingScope.Name)
            {
                // Check if new name already exists
                var nameExists = await db.Scopes
                    .AnyAsync(s => s.OrganizationId == orgId && s.Name == request.Name && s.Id != scopeId);

                if (nameExists)
                {
                    return Results.Conflict("A scope with this name already exists in this organization");
                }

                existingScope.Name = request.Name;
            }

            if (request.Description != null)
            {
                existingScope.Description = request.Description;
            }
        }

        await db.SaveChangesAsync();

        var response = new ScopeResponse(
            existingScope.Id,
            existingScope.OrganizationId,
            existingScope.Name,
            existingScope.Description);

        return isNew
            ? Results.Created($"/organizations/{orgId}/scopes/{existingScope.Id}", response)
            : Results.Ok(response);
    }

    private static async Task<IResult> DeleteScope(Guid orgId, Guid scopeId, AuthyDbContext db)
    {
        var scope = await db.Scopes
            .FirstOrDefaultAsync(s => s.OrganizationId == orgId && s.Id == scopeId);

        if (scope == null)
        {
            return Results.NotFound();
        }

        db.Scopes.Remove(scope);
        await db.SaveChangesAsync();

        return Results.NoContent();
    }
}

public record PatchScopeRequest(string? Name, string? Description);

public record ScopeResponse(
    Guid Id,
    Guid OrganizationId,
    string Name,
    string? Description);
