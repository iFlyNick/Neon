using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Neon.Persistence.EntityModels;
using Neon.Persistence.EntityModels.Twitch;

namespace Neon.Persistence.NeonContext;

public class NeonDbContext(DbContextOptions<NeonDbContext> options) : DbContext(options)
{
    private const string defaultDbUser = "NeonApp";

    public DbSet<TwitchAccount>? TwitchAccount { get; set; }
    public DbSet<BotAccount>? BotAccount { get; set; }

    //abstracts the fluentapi calls to the configuration classes to keep this class relatively clean
    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);

    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        var entries = ChangeTracker.Entries<BaseModel>().ToList();
        foreach (var entry in entries)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedDate = DateTime.UtcNow;
                    entry.Entity.CreatedBy = defaultDbUser;
                    break;
                case EntityState.Modified:
                    entry.Entity.ModifiedDate = DateTime.UtcNow;
                    entry.Entity.ModifiedBy = defaultDbUser;
                    break;
                case EntityState.Deleted:
                    entry.Entity.ModifiedDate = DateTime.UtcNow;
                    entry.Entity.ModifiedBy = defaultDbUser;
                    break;
            }
        }
        return await base.SaveChangesAsync(ct);
    }
}

public class NeonDbContextDesignTimeFactory : IDesignTimeDbContextFactory<NeonDbContext>
{
    public NeonDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<NeonDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;User Id=postgres;Password=postgres;Database=Neon");

        return new NeonDbContext(optionsBuilder.Options);
    }
}