using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Neon.Persistence.EntityModels;
using Neon.Persistence.EntityModels.Twitch;

namespace Neon.Persistence.NeonContext;

public class NeonDbContext(DbContextOptions<NeonDbContext> options) : DbContext(options)
{
    private const string DefaultDbUser = "NeonApiService";

    public DbSet<AppAccount> AppAccount { get; set; }
    public DbSet<SubscriptionType> SubscriptionType { get; set; }
    public DbSet<TwitchAccount> TwitchAccount { get; set; }
    public DbSet<TwitchAccountAuth> TwitchAccountAuth { get; set; }
    public DbSet<TwitchAccountScope> TwitchAccountScope { get; set; }

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
                    entry.Entity.CreatedBy = DefaultDbUser;
                    break;
                case EntityState.Modified or EntityState.Deleted:
                    entry.Entity.ModifiedDate = DateTime.UtcNow;
                    entry.Entity.ModifiedBy = DefaultDbUser;
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
        //TODO: solve this later so you can import from config or something?
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;User Id=postgres;Password=postgres;Database=Neon").UseSnakeCaseNamingConvention();

        return new NeonDbContext(optionsBuilder.Options);
    }
}