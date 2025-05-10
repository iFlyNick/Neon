using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Neon.Persistence.EntityModels.Twitch;

public class AuthorizationScopeSubscriptionType : BaseModel
{
    public Guid? AuthorizationScopeId { get; set; }
    public Guid? SubscriptionTypeId { get; set; }
    
    public AuthorizationScope? AuthorizationScope { get; set; }
    public SubscriptionType? SubscriptionType { get; set; }
}

public class
    AuthorizationScopeSubscriptionTypeConfiguration : IEntityTypeConfiguration<AuthorizationScopeSubscriptionType>
{
    public void Configure(EntityTypeBuilder<AuthorizationScopeSubscriptionType> builder)
    {
        //schema
        builder.ToTable("authorization_scope_subscription_type", "twitch");

        //pk
        builder.HasKey(s => new { s.AuthorizationScopeId, s.SubscriptionTypeId });

        //indexes

        //relationships
        builder.HasOne(s => s.AuthorizationScope).WithMany(s => s.AuthorizationScopeSubscriptionTypes)
            .HasForeignKey(s => s.AuthorizationScopeId);
        builder.HasOne(s => s.SubscriptionType).WithMany(s => s.AuthorizationScopeSubscriptionTypes)
            .HasForeignKey(s => s.SubscriptionTypeId);

        //base model
        builder.Property(s => s.CreatedDate).HasColumnOrder(3).IsRequired();
        builder.Property(s => s.CreatedBy).HasColumnOrder(4).IsRequired().HasMaxLength(50);
        builder.Property(s => s.ModifiedDate).HasColumnOrder(5);
        builder.Property(s => s.ModifiedBy).HasColumnOrder(6).HasMaxLength(50);

        //columns
        builder.Property(s => s.AuthorizationScopeId).HasColumnOrder(1);
        builder.Property(s => s.SubscriptionTypeId).HasColumnOrder(2);
    }
}