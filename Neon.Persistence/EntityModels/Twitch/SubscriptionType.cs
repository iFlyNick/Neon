using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Neon.Persistence.EntityModels.Twitch;

public class SubscriptionType : BaseModel
{
    public Guid? SubscriptionTypeId { get; set; }
    public string? Name { get; set; }
    public string? Version { get; set; }
    public string? Description { get; set; }
    
    public ICollection<TwitchAccountScope>? TwitchAccountScopes { get; set; }
}

public class SubscriptionTypeConfiguration : IEntityTypeConfiguration<SubscriptionType>
{
    public void Configure(EntityTypeBuilder<SubscriptionType> builder)
    {
        //schema
        builder.ToTable("subscription_type", "twitch");
        
        //pk
        builder.HasKey(s => s.SubscriptionTypeId);

        //indexes
        builder.HasIndex(s => s.Name);

        //relationships
        builder.HasMany(s => s.TwitchAccountScopes).WithOne(s => s.SubscriptionType).HasForeignKey(s => s.SubscriptionTypeId);

        //base model
        builder.Property(s => s.CreatedDate).HasColumnOrder(2).IsRequired();
        builder.Property(s => s.CreatedBy).HasColumnOrder(3).IsRequired().HasMaxLength(50);
        builder.Property(s => s.ModifiedDate).HasColumnOrder(4);
        builder.Property(s => s.ModifiedBy).HasColumnOrder(5).HasMaxLength(50);

        //columns
        builder.Property(s => s.SubscriptionTypeId).HasColumnOrder(1).ValueGeneratedOnAdd();
        builder.Property(s => s.Name).IsRequired().HasMaxLength(100);
        builder.Property(s => s.Version).IsRequired().HasMaxLength(10);
        builder.Property(s => s.Description).IsRequired().HasMaxLength(500);
    }
}