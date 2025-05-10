using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Neon.Persistence.EntityModels.Twitch;

public class TwitchAccountScope : BaseModel
{
    public Guid? TwitchAccountScopeId { get; set; }
    public Guid? TwitchAccountId { get; set; }
    public Guid? SubscriptionTypeId { get; set; }
    
    public TwitchAccount? TwitchAccount { get; set; }
    public SubscriptionType? SubscriptionType { get; set; }
}

public class TwitchAccountScopeConfiguration : IEntityTypeConfiguration<TwitchAccountScope>
{
    public void Configure(EntityTypeBuilder<TwitchAccountScope> builder)
    {
        //schema
        builder.ToTable("twitch_account_scope", "twitch");
        
        //pk
        builder.HasKey(s => s.TwitchAccountScopeId);

        //indexes
        builder.HasIndex(s => s.TwitchAccountId);
        
        //relationships
        builder.HasOne(s => s.TwitchAccount).WithMany(s => s.TwitchAccountScopes).HasForeignKey(s => s.TwitchAccountId);
        builder.HasOne(s => s.SubscriptionType).WithMany(s => s.TwitchAccountScopes).HasForeignKey(s => s.SubscriptionTypeId);
        
        //base model
        builder.Property(s => s.CreatedDate).HasColumnOrder(2).IsRequired();
        builder.Property(s => s.CreatedBy).HasColumnOrder(3).IsRequired().HasMaxLength(50);
        builder.Property(s => s.ModifiedDate).HasColumnOrder(4);
        builder.Property(s => s.ModifiedBy).HasColumnOrder(5).HasMaxLength(50);

        //columns
        builder.Property(s => s.TwitchAccountScopeId).HasColumnOrder(1).ValueGeneratedOnAdd();
        builder.Property(s => s.TwitchAccountId).IsRequired();
        builder.Property(s => s.SubscriptionTypeId).IsRequired();
    }
}