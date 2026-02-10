using Microsoft.EntityFrameworkCore;

namespace Authy.Application.Data;

public class AuthyDbContext(DbContextOptions<AuthyDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<Scope> Scopes { get; set; }
    public DbSet<Organization> Organizations { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasOne<Organization>()
            .WithMany(o => o.Owners)
            .HasForeignKey(u => u.OrganizationId);

        modelBuilder.Entity<Role>()
            .HasMany(r => r.Scopes)
            .WithMany(s => s.Roles);

        modelBuilder.Entity<Scope>()
            .HasIndex(s => new { s.OrganizationId, s.Name })
            .IsUnique();
            
        modelBuilder.Entity<Role>()
            .HasIndex(r => new { r.OrganizationId, r.Name })
            .IsUnique();
    }
}


