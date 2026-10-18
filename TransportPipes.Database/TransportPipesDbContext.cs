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
    public DbSet<DbContract> Contracts => Set<DbContract>();
    public DbSet<DbCorporation> Corporations => Set<DbCorporation>();
    public DbSet<DbChatMessage> ChatMessages => Set<DbChatMessage>();
    public DbSet<DbInboxMessage> InboxMessages => Set<DbInboxMessage>();
    public DbSet<DbGuestbookNote> GuestbookNotes => Set<DbGuestbookNote>();
    public DbSet<DbBlock> Blocks => Set<DbBlock>();

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
            user.Property(t => t.LegacyTitles).HasColumnType("jsonb").HasDefaultValueSql("'[]'::jsonb");
        });

        modelBuilder.Entity<DbWorld>(world =>
        {
            world.ToTable("World");
            world.Property(t => t.Id).ValueGeneratedNever();
            world.Property(t => t.Markets).HasColumnType("jsonb").HasDefaultValueSql("'[]'::jsonb");
            world.Property(t => t.Exchange).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
            world.Property(t => t.Events).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
            world.Property(t => t.Wonders).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
            world.Property(t => t.Outposts).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
            world.Property(t => t.Season).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
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

        modelBuilder.Entity<DbContract>(contract =>
        {
            contract.ToTable("Contracts");
            contract.Property(t => t.Id).ValueGeneratedNever();
            contract.Property(t => t.Resource).HasMaxLength(40);
            contract.HasIndex(t => t.State);
        });

        modelBuilder.Entity<DbCorporation>(corporation =>
        {
            corporation.ToTable("Corporations");
            corporation.Property(t => t.Id).ValueGeneratedNever();
            corporation.Property(t => t.Name).HasMaxLength(40);
            corporation.Property(t => t.Tag).HasMaxLength(8);
            corporation.Property(t => t.Color).HasMaxLength(16);
            corporation.Property(t => t.PerkResource).HasMaxLength(40);
            corporation.Property(t => t.Description).HasMaxLength(400);
            corporation.Property(t => t.Data).HasColumnType("jsonb");
            corporation.HasIndex(t => t.Tag).IsUnique();
        });

        modelBuilder.Entity<DbChatMessage>(message =>
        {
            message.ToTable("ChatMessages");
            message.Property(t => t.Channel).HasMaxLength(64);
            message.Property(t => t.SenderName).HasMaxLength(40);
            message.Property(t => t.Text).HasMaxLength(500);
            message.HasIndex(t => new { t.Channel, t.Id });
        });

        modelBuilder.Entity<DbInboxMessage>(message =>
        {
            message.ToTable("InboxMessages");
            message.Property(t => t.Kind).HasMaxLength(40);
            message.Property(t => t.Text).HasMaxLength(500);
            message.HasIndex(t => new { t.Company, t.Id });
        });

        modelBuilder.Entity<DbGuestbookNote>(note =>
        {
            note.ToTable("GuestbookNotes");
            note.Property(t => t.AuthorName).HasMaxLength(40);
            note.Property(t => t.Text).HasMaxLength(300);
            note.HasIndex(t => new { t.Owner, t.Id });
        });

        modelBuilder.Entity<DbBlock>(block =>
        {
            block.ToTable("Blocks");
            block.HasKey(t => new { t.Company, t.Blocked });
        });

        modelBuilder.Entity<DbTerrainOverride>(terrain =>
        {
            terrain.ToTable("TerrainOverrides");
            terrain.HasKey(t => new { t.X, t.Y });
        });
    }
}
