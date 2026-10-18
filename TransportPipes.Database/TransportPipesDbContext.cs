using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TransportPipes.Database.Entities;
using TransportPipes.Database.Helpers;

namespace TransportPipes.Database;

public class TransportPipesDbContext : DbContext, IDataProtectionKeyContext
{
    public TransportPipesDbContext(DbContextOptions<TransportPipesDbContext> options)
        : base(options)
    {
    }

    public DbSet<DbUser> Users => Set<DbUser>();
    public DbSet<DbWorld> Worlds => Set<DbWorld>();
    public DbSet<DbCompany> Companies => Set<DbCompany>();
    public DbSet<DbEstate> Estates => Set<DbEstate>();
    public DbSet<DbBuilding> Buildings => Set<DbBuilding>();
    public DbSet<DbConductor> Conductors => Set<DbConductor>();
    public DbSet<DbTerrainOverride> TerrainOverrides => Set<DbTerrainOverride>();

    /// <summary>The keys that protect the login cookies, so logins survive a restart</summary>
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbUser>(user =>
        {
            user.ToTable("Users");
            user.Property(t => t.UserName).HasMaxLength(100);
            user.Property(t => t.NormalizedUserName).HasMaxLength(100);
            user.Property(t => t.Email).HasMaxLength(256);
            user.HasIndex(t => t.NormalizedUserName).IsUnique();
        });

        modelBuilder.Entity<DbWorld>(world =>
        {
            world.ToTable("World");
            world.Property(t => t.Id).ValueGeneratedNever();
            world.Property(t => t.Markets).HasColumnType("jsonb").HasDefaultValueSql("'[]'::jsonb");
        });

        modelBuilder.Entity<DbCompany>(company =>
        {
            company.ToTable("Companies");
            company.Property(t => t.Id).ValueGeneratedNever();
            company.Property(t => t.Name).HasMaxLength(40);
            company.Property(t => t.Color).HasMaxLength(16);
            company.Property(t => t.Settings).HasColumnType("jsonb");
            company.Property(t => t.Report).HasColumnType("jsonb");
            company.HasIndex(t => t.UserId).IsUnique();
            company.HasIndex(t => t.Name).IsUnique();
        });

        modelBuilder.Entity<DbEstate>(estate =>
        {
            estate.ToTable("Estates");
            estate.HasKey(t => new { t.DistrictX, t.DistrictY, t.Slot });
            estate.Property(t => t.GatePolicies).HasColumnType("jsonb");
        });

        modelBuilder.Entity<DbBuilding>(building =>
        {
            building.ToTable("Buildings");
            building.Property(t => t.Id).ValueGeneratedNever();
            building.Property(t => t.Type).HasMaxLength(40);
            building.Property(t => t.Contents).HasColumnType("jsonb");
            building.Property(t => t.Imports).HasColumnType("jsonb");
            building.Property(t => t.Filter).HasColumnType("jsonb").HasDefaultValueSql("'[]'::jsonb");
            building.HasIndex(t => t.OwnerId);
        });

        modelBuilder.Entity<DbConductor>(conductor =>
        {
            conductor.ToTable("Conductors");
            conductor.HasKey(t => new { t.X, t.Y });
        });

        modelBuilder.Entity<DbTerrainOverride>(terrain =>
        {
            terrain.ToTable("TerrainOverrides");
            terrain.HasKey(t => new { t.X, t.Y });
        });
    }
}
