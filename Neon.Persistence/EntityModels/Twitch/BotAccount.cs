using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Neon.Persistence.EntityModels.Twitch;

public partial class BotAccount : BaseModel
{
    public Guid? BotAccountId { get; set; }
    public string? BotName { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? AccessToken { get; set; }
    public string? RedirectUri { get; set; }
    public string? TwitchBroadcasterId { get; set; }
}

public class BotAccountConfiguration : IEntityTypeConfiguration<BotAccount>
{
    public void Configure(EntityTypeBuilder<BotAccount> builder)
    {
        //schema
        builder.ToTable("bot_account", "twitch");
        
        //pk
        builder.HasKey(s => s.BotAccountId);

        //indexes

        //relationships

        //base model
        builder.Property(s => s.CreatedDate).HasColumnOrder(2).IsRequired();
        builder.Property(s => s.CreatedBy).HasColumnOrder(3).IsRequired().HasMaxLength(50);
        builder.Property(s => s.ModifiedDate).HasColumnOrder(4);
        builder.Property(s => s.ModifiedBy).HasColumnOrder(5).HasMaxLength(50);

        //columns
        builder.Property(s => s.BotAccountId).HasColumnOrder(1).ValueGeneratedOnAdd();
        builder.Property(s => s.BotName).IsRequired().HasMaxLength(50);
        builder.Property(s => s.ClientId).IsRequired();
        builder.Property(s => s.ClientSecret).IsRequired();
        builder.Property(s => s.AccessToken).IsRequired();
        builder.Property(s => s.RedirectUri).IsRequired();
        builder.Property(s => s.TwitchBroadcasterId).IsRequired();
    }
}
