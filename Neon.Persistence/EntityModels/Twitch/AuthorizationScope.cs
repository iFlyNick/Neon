using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Neon.Persistence.EntityModels.Twitch;

public class AuthorizationScope : BaseModel
{
    public Guid? AuthorizationScopeId { get; set; }
    public string? Name { get; set; }
    
    public ICollection<TwitchAccountScope>? TwitchAccountScopes { get; set; }
    public ICollection<AuthorizationScopeSubscriptionType>? AuthorizationScopeSubscriptionTypes { get; set; }
}

public class AuthorizationScopeConfiguration : IEntityTypeConfiguration<AuthorizationScope>
{
    public void Configure(EntityTypeBuilder<AuthorizationScope> builder)
    {
        //schema
        builder.ToTable("authorization_scope", "twitch");
        
        //pk
        builder.HasKey(s => s.AuthorizationScopeId);

        //indexes
        builder.HasIndex(s => s.Name);

        //relationships
        builder.HasMany(s => s.TwitchAccountScopes).WithOne(s => s.AuthorizationScope).HasForeignKey(s => s.AuthorizationScopeId);
        builder.HasMany(s => s.AuthorizationScopeSubscriptionTypes)
            .WithOne(s => s.AuthorizationScope).HasForeignKey(s => s.AuthorizationScopeId);

        //base model
        builder.Property(s => s.CreatedDate).HasColumnOrder(2).IsRequired();
        builder.Property(s => s.CreatedBy).HasColumnOrder(3).IsRequired().HasMaxLength(50);
        builder.Property(s => s.ModifiedDate).HasColumnOrder(4);
        builder.Property(s => s.ModifiedBy).HasColumnOrder(5).HasMaxLength(50);

        //columns
        builder.Property(s => s.AuthorizationScopeId).HasColumnOrder(1).ValueGeneratedOnAdd();
        builder.Property(s => s.Name).IsRequired().HasMaxLength(100);
    }
}