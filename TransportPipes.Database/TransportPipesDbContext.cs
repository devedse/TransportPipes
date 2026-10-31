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
    public DbSet<DbChatMessage> ChatMessages => Set<DbChatMessage>();
    public DbSet<DbInboxMessage> InboxMessages => Set<DbInboxMessage>();
    public DbSet<DbGuestbookNote> GuestbookNotes => Set<DbGuestbookNote>();
    public DbSet<DbBlock> Blocks => Set<DbBlock>();
    public DbSet<DbStatSample> StatSamples => Set<DbStatSample>();
    public DbSet<DbMoment> Moments => Set<DbMoment>();

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
            world.Property(t => t.Exchange).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
            world.Property(t => t.Events).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
            world.Property(t => t.Wonders).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
            world.Property(t => t.Outposts).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
            world.Property(t => t.Blueprints).HasColumnType("jsonb").HasDefaultValueSql("'[]'::jsonb");
            world.Property(t => t.Crews).HasColumnType("jsonb").HasDefaultValueSql("'[]'::jsonb");
            world.Property(t => t.Links).HasColumnType("jsonb").HasDefaultValueSql("'[]'::jsonb");
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
        });

        modelBuilder.Entity<DbBuilding>(building =>
        {
            building.ToTable("Buildings");
            building.Property(t => t.Id).ValueGeneratedNever();
            building.Property(t => t.Type).HasMaxLength(40);
            building.Property(t => t.Contents).HasColumnType("jsonb");
            building.Property(t => t.Imports).HasColumnType("jsonb");
            building.Property(t => t.Filter).HasColumnType("jsonb").HasDefaultValueSql("'[]'::jsonb");
            building.Property(t => t.Keeps).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
            building.HasIndex(t => t.OwnerId);
        });

        modelBuilder.Entity<DbConductor>(conductor =>
        {
            conductor.ToTable("Conductors");
            conductor.HasKey(t => new { t.X, t.Y });
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

        modelBuilder.Entity<DbStatSample>(sample =>
        {
            sample.ToTable("StatSamples");
            sample.HasKey(t => new { t.Kind, t.Subject, t.At });
        });

        modelBuilder.Entity<DbMoment>(moment =>
        {
            moment.ToTable("Moments");
            moment.Property(t => t.Kind).HasMaxLength(20);
            moment.Property(t => t.Text).HasMaxLength(200);
            moment.Property(t => t.Ref).HasMaxLength(40);
            moment.HasIndex(t => new { t.Company, t.At });
        });
    }
}
