using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Neon.Persistence.EntityModels.Twitch;

public class TwitchAccountLoyalty : BaseModel
{
    public Guid? TwitchAccountLoyaltyId { get; set; }
    public Guid? TwitchAccountId { get; set; }
    public string? LoyaltyName { get; set; }
    public bool? IsLoyaltyEnabled { get; set; }
    public int? PointIntervalMinutes { get; set; }
    public int? BasePointsPerInterval { get; set; }
    public double? BasePointModifier { get; set; }
    public double? SubscriberPointModifier { get; set; }
    public int? FollowerBonusPoints { get; set; }
    public int? SubscriberBonusPoints { get; set; }
    public bool? EnabledForViewersOnly { get; set; }
    
    public TwitchAccount? TwitchAccount { get; set; }
    public ICollection<LoyaltyTracking>? LoyaltyTracking { get; set; }
}

public class TwitchAccountLoyaltyConfiguration : IEntityTypeConfiguration<TwitchAccountLoyalty>
{
    public void Configure(EntityTypeBuilder<TwitchAccountLoyalty> builder)
    {
        //schema
        builder.ToTable("twitch_account_loyalty", "twitch");

        //pk
        builder.HasKey(s => s.TwitchAccountLoyaltyId);

        //indexes
        builder.HasIndex(s => s.TwitchAccountId);

        //relationships
        builder.HasOne(s => s.TwitchAccount).WithOne(s => s.TwitchAccountLoyalty);
        builder.HasMany(s => s.LoyaltyTracking).WithOne(s => s.TwitchAccountLoyalty).HasForeignKey(s => s.TwitchAccountLoyaltyId).OnDelete(DeleteBehavior.Cascade);

        //base model
        builder.Property(s => s.CreatedDate).HasColumnOrder(2).IsRequired();
        builder.Property(s => s.CreatedBy).HasColumnOrder(3).IsRequired().HasMaxLength(50);
        builder.Property(s => s.ModifiedDate).HasColumnOrder(4);
        builder.Property(s => s.ModifiedBy).HasColumnOrder(5).HasMaxLength(50);

        //columns
        builder.Property(s => s.TwitchAccountLoyaltyId).HasColumnOrder(1).ValueGeneratedOnAdd();
        builder.Property(s => s.TwitchAccountId).IsRequired();
        builder.Property(s => s.LoyaltyName).IsRequired().HasMaxLength(100);
        builder.Property(s => s.IsLoyaltyEnabled).IsRequired().HasDefaultValue(false);
        builder.Property(s => s.PointIntervalMinutes).IsRequired().HasDefaultValue(10);
        builder.Property(s => s.BasePointsPerInterval).IsRequired().HasDefaultValue(50);
        builder.Property(s => s.BasePointModifier).IsRequired().HasDefaultValue(1.0);
        builder.Property(s => s.SubscriberPointModifier).IsRequired().HasDefaultValue(2.0);
        builder.Property(s => s.FollowerBonusPoints).IsRequired().HasDefaultValue(300);
        builder.Property(s => s.SubscriberBonusPoints).IsRequired().HasDefaultValue(500);
        builder.Property(s => s.EnabledForViewersOnly).IsRequired().HasDefaultValue(true);
    }
}