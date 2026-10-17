using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using TransportPipes.Database.Entities;
using TransportPipes.Database.Entities.ActionDetails;
using TransportPipes.Database.Entities.ClientUpdateDetails;
using TransportPipes.Database.Helpers;

namespace TransportPipes.Database;

public class TransportPipesDbContext : DbContext
{
    public TransportPipesDbContext(DbContextOptions<TransportPipesDbContext> options)
        : base(options)
    {
    }

    public DbSet<DbUser> Users => Set<DbUser>();
    public DbSet<DbGlobalConnection> GlobalConnections => Set<DbGlobalConnection>();
    public DbSet<DbGameObject> GameObjects => Set<DbGameObject>();
    public DbSet<DbConsumerConnection> ConsumerConnections => Set<DbConsumerConnection>();
    public DbSet<DbProviderConnection> ProviderConnections => Set<DbProviderConnection>();
    public DbSet<DbAction> Actions => Set<DbAction>();
    public DbSet<DbClientUpdate> ClientUpdates => Set<DbClientUpdate>();

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

        modelBuilder.Entity<DbGlobalConnection>(globalConnection =>
        {
            globalConnection.ToTable("GlobalConnections");
            globalConnection.HasIndex(t => t.GlobalConnectionId);
        });

        modelBuilder.Entity<DbGameObject>(gameObject =>
        {
            gameObject.ToTable("GameObjects");
            gameObject.HasIndex(t => new { t.ChunkX, t.ChunkY });
            gameObject.HasIndex(t => t.GlobalConnectionId);
            // There can only be one GameObject on every tile
            gameObject.HasIndex(t => new { t.X, t.Y }).IsUnique();
            gameObject.HasOne(t => t.Owner).WithMany().HasForeignKey(t => t.DbOwnerId).OnDelete(DeleteBehavior.Restrict);
            gameObject.HasOne(t => t.DbGlobalConnection).WithMany().HasForeignKey(t => t.DbGlobalConnectionId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<DbConsumerConnection>(consumerConnection =>
        {
            consumerConnection.ToTable("ConsumerConnections");
            consumerConnection.HasIndex(t => t.ConnectionId);
            consumerConnection.HasIndex(t => t.GlobalConnectionId);
            consumerConnection.HasOne(t => t.Destination).WithMany().HasForeignKey(t => t.DestinationId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DbProviderConnection>(providerConnection =>
        {
            providerConnection.ToTable("ProviderConnections");
            providerConnection.HasIndex(t => t.ConnectionId);
            providerConnection.HasIndex(t => t.GlobalConnectionId);
            providerConnection.HasOne(t => t.Source).WithMany().HasForeignKey(t => t.SourceId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DbAction>(action =>
        {
            action.ToTable("Actions");
            action.HasIndex(t => t.Executed);
            action.HasIndex(t => t.DateToBeExecuted);
            // Only one action can be queued per tile, this used to be enforced by a serializable transaction
            action.HasIndex(t => new { t.X, t.Y }).IsUnique().HasFilter("NOT \"Executed\"");
            action.HasOne(t => t.ExecutedBy).WithMany().HasForeignKey(t => t.ExecutedById).OnDelete(DeleteBehavior.Restrict);
            action.Property(t => t.ActionDetails)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => DetailsJsonSerializer.Serialize(v),
                    v => DetailsJsonSerializer.DeserializeActionDetails(v),
                    new ValueComparer<ActionDetailsBase?>(
                        (a, b) => DetailsJsonSerializer.Serialize(a) == DetailsJsonSerializer.Serialize(b),
                        v => DetailsJsonSerializer.Serialize(v).GetHashCode(),
                        v => DetailsJsonSerializer.DeserializeActionDetails(DetailsJsonSerializer.Serialize(v))));
        });

        modelBuilder.Entity<DbClientUpdate>(clientUpdate =>
        {
            clientUpdate.ToTable("ClientUpdates");
            clientUpdate.HasOne(t => t.ExecutedBy).WithMany().HasForeignKey(t => t.ExecutedById).OnDelete(DeleteBehavior.SetNull);
            clientUpdate.Property(t => t.ClientUpdateDetails)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => DetailsJsonSerializer.Serialize(v),
                    v => DetailsJsonSerializer.DeserializeClientUpdateDetails(v),
                    new ValueComparer<ClientUpdateDetailsBase?>(
                        (a, b) => DetailsJsonSerializer.Serialize(a) == DetailsJsonSerializer.Serialize(b),
                        v => DetailsJsonSerializer.Serialize(v).GetHashCode(),
                        v => DetailsJsonSerializer.DeserializeClientUpdateDetails(DetailsJsonSerializer.Serialize(v))));
        });
    }
}
