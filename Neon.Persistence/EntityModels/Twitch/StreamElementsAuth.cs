using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Neon.Persistence.EntityModels.Twitch;

public class StreamElementsAuth : BaseModel
{
    public Guid? StreamElementsAuthId { get; set; }
    public Guid? TwitchAccountId { get; set; }
    public string? StreamElementsChannel { get; set; }
    public string? JwtToken { get; set; }
    public string? JwtTokenIv { get; set; }
    
    public TwitchAccount? TwitchAccount { get; set; }
}

public class StreamElementsAuthConfiguration : IEntityTypeConfiguration<StreamElementsAuth>
{
    public void Configure(EntityTypeBuilder<StreamElementsAuth> builder)
    {
                //schema
        builder.ToTable("streamelements_auth", "twitch");
        
        //pk
        builder.HasKey(s => s.StreamElementsAuthId);

        //indexes
        builder.HasIndex(s => s.TwitchAccountId).IsUnique();

        //relationships
        builder.HasOne(s => s.TwitchAccount).WithOne(s => s.StreamElementsAuth).HasForeignKey<TwitchAccount>(s => s.TwitchAccountId);
        
        //base model
        builder.Property(s => s.CreatedDate).HasColumnOrder(2).IsRequired();
        builder.Property(s => s.CreatedBy).HasColumnOrder(3).IsRequired().HasMaxLength(50);
        builder.Property(s => s.ModifiedDate).HasColumnOrder(4);
        builder.Property(s => s.ModifiedBy).HasColumnOrder(5).HasMaxLength(50);

        //columns
        builder.Property(s => s.StreamElementsAuthId).HasColumnOrder(1).ValueGeneratedOnAdd();
        builder.Property(s => s.TwitchAccountId).IsRequired();
        builder.Property(s => s.StreamElementsChannel).IsRequired().HasMaxLength(100);
        builder.Property(s => s.JwtToken).IsRequired().HasMaxLength(2000);
        builder.Property(s => s.JwtTokenIv).IsRequired().HasMaxLength(200);
    }
}