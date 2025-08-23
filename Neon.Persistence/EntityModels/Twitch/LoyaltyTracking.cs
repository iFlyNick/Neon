using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Neon.Persistence.EntityModels.Twitch;

public class LoyaltyTracking : BaseModel
{
    public Guid? LoyaltyTrackingId { get; set; }
    public Guid? TwitchAccountLoyaltyId { get; set; }
    public string? UserId { get; set; }
    public string? UserLogin { get; set; }
    public int? Points { get; set; }
    
    public TwitchAccountLoyalty? TwitchAccountLoyalty { get; set; }
}

public class LoyaltyTrackingConfiguration : IEntityTypeConfiguration<LoyaltyTracking>
{
    public void Configure(EntityTypeBuilder<LoyaltyTracking> builder)
    {
        //schema
        builder.ToTable("loyalty_tracking", "twitch");

        //pk
        builder.HasKey(s => s.LoyaltyTrackingId);

        //indexes
        builder.HasIndex(s => s.TwitchAccountLoyaltyId);
        builder.HasIndex(s => s.UserLogin);

        //relationships
        builder.HasOne(s => s.TwitchAccountLoyalty).WithMany(s => s.LoyaltyTracking)
            .HasForeignKey(s => s.TwitchAccountLoyaltyId);

        //base model
        builder.Property(s => s.CreatedDate).HasColumnOrder(2).IsRequired();
        builder.Property(s => s.CreatedBy).HasColumnOrder(3).IsRequired().HasMaxLength(50);
        builder.Property(s => s.ModifiedDate).HasColumnOrder(4);
        builder.Property(s => s.ModifiedBy).HasColumnOrder(5).HasMaxLength(50);

        //columns
        builder.Property(s => s.LoyaltyTrackingId).HasColumnOrder(1).ValueGeneratedOnAdd();
        builder.Property(s => s.TwitchAccountLoyaltyId).IsRequired();
        builder.Property(s => s.UserId).IsRequired().HasMaxLength(25);
        builder.Property(s => s.UserLogin).IsRequired().HasMaxLength(50);
        builder.Property(s => s.Points).IsRequired().HasDefaultValue(0);
    }
}