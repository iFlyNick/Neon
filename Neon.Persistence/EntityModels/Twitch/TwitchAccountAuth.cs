using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Neon.Persistence.EntityModels.Twitch;

public class TwitchAccountAuth : BaseModel
{
    public Guid? TwitchAccountAuthId { get; set; }
    public Guid? TwitchAccountId { get; set; }
    public string? AccessToken { get; set; } //encrypted
    public string? AccessTokenIv { get; set; }
    public string? RefreshToken { get; set; } //encrypted
    public string? RefreshTokenIv { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastRefreshDate { get; set; }
    public DateTime? LastValidationDate { get; set; }
    
    public TwitchAccount? TwitchAccount { get; set; }
}

public class TwitchAccountAuthConfiguration : IEntityTypeConfiguration<TwitchAccountAuth>
{
    public void Configure(EntityTypeBuilder<TwitchAccountAuth> builder)
    {
        //schema
        builder.ToTable("twitch_account_auth", "twitch");
        
        //pk
        builder.HasKey(s => s.TwitchAccountAuthId);

        //indexes
        builder.HasIndex(s => s.TwitchAccountId);

        //relationships
        builder.HasOne(s => s.TwitchAccount).WithOne(s => s.TwitchAccountAuth)
            .HasForeignKey<TwitchAccountAuth>(s => s.TwitchAccountId);
        
        //base model
        builder.Property(s => s.CreatedDate).HasColumnOrder(2).IsRequired();
        builder.Property(s => s.CreatedBy).HasColumnOrder(3).IsRequired().HasMaxLength(50);
        builder.Property(s => s.ModifiedDate).HasColumnOrder(4);
        builder.Property(s => s.ModifiedBy).HasColumnOrder(5).HasMaxLength(50);

        //columns
        builder.Property(s => s.TwitchAccountAuthId).HasColumnOrder(1).ValueGeneratedOnAdd();
        builder.Property(s => s.TwitchAccountId).IsRequired();
        builder.Property(s => s.AccessToken).IsRequired().HasMaxLength(255);
        builder.Property(s => s.AccessTokenIv).IsRequired().HasMaxLength(255);
        builder.Property(s => s.RefreshToken).IsRequired().HasMaxLength(255);
        builder.Property(s => s.RefreshTokenIv).IsRequired().HasMaxLength(255);
        builder.Property(s => s.ExpiresAt);
        builder.Property(s => s.LastRefreshDate);
        builder.Property(s => s.LastValidationDate);
    }
}