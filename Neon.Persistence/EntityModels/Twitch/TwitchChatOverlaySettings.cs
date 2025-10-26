using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Neon.Persistence.EntityModels.Twitch;

public class TwitchChatOverlaySettings : BaseModel
{
    public Guid? TwitchChatOverlaySettingsId { get; set; }
    public Guid? TwitchAccountId { get; set; }
    public string? OverlayName { get; set; }
    public string? OverlayUrl { get; set; }
    public string? ChatStyle { get; set; }
    public bool? IgnoreBotMessages { get; set; }
    public bool? IgnoreCommandMessages { get; set; }
    public bool? UseTwitchBadges { get; set; }
    public bool? UseBetterTtvEmotes { get; set; }
    public bool? UseSevenTvEmotes { get; set; }
    public bool? UseFfzEmotes { get; set; }
    public int? ChatDelayMilliseconds { get; set; }
    public bool? AlwaysKeepMessages { get; set; }
    public int? ChatMessageRemoveDelayMilliseconds { get; set; }
    public string? FontFamily { get; set; }
    public int? FontSize { get; set; }
    
    public TwitchAccount? TwitchAccount { get; set; }
}

public class TwitchChatOverlaySettingsConfiguration : IEntityTypeConfiguration<TwitchChatOverlaySettings>
{
    public void Configure(EntityTypeBuilder<TwitchChatOverlaySettings> builder)
    {
        //schema
        builder.ToTable("twitch_chat_overlay_settings", "twitch");
        
        //pk
        builder.HasKey(s => s.TwitchChatOverlaySettingsId);

        //indexes
        builder.HasIndex(s => s.TwitchAccountId);

        //relationships
        builder.HasOne(s => s.TwitchAccount).WithMany(s => s.TwitchChatOverlaySettings).HasForeignKey(s => s.TwitchAccountId);
        
        //base model
        builder.Property(s => s.CreatedDate).HasColumnOrder(2).IsRequired();
        builder.Property(s => s.CreatedBy).HasColumnOrder(3).IsRequired().HasMaxLength(50);
        builder.Property(s => s.ModifiedDate).HasColumnOrder(4);
        builder.Property(s => s.ModifiedBy).HasColumnOrder(5).HasMaxLength(50);

        //columns
        builder.Property(s => s.TwitchChatOverlaySettingsId).HasColumnOrder(1).ValueGeneratedOnAdd();
        builder.Property(s => s.TwitchAccountId).IsRequired();
        builder.Property(s => s.OverlayName).IsRequired().HasMaxLength(300);
        builder.Property(s => s.OverlayUrl).IsRequired().HasMaxLength(1000);
        builder.Property(s => s.ChatStyle).IsRequired().HasMaxLength(100);
        builder.Property(s => s.IgnoreBotMessages).IsRequired().HasDefaultValue(false);
        builder.Property(s => s.IgnoreCommandMessages).IsRequired().HasDefaultValue(false);
        builder.Property(s => s.UseTwitchBadges).IsRequired().HasDefaultValue(true);
        builder.Property(s => s.UseBetterTtvEmotes).IsRequired().HasDefaultValue(true);
        builder.Property(s => s.UseSevenTvEmotes).IsRequired().HasDefaultValue(true);
        builder.Property(s => s.UseFfzEmotes).IsRequired().HasDefaultValue(true);
        builder.Property(s => s.ChatDelayMilliseconds).IsRequired().HasDefaultValue(0);
        builder.Property(s => s.AlwaysKeepMessages).IsRequired().HasDefaultValue(false);
        builder.Property(s => s.ChatMessageRemoveDelayMilliseconds).IsRequired().HasDefaultValue(300000);
        builder.Property(s => s.FontFamily).IsRequired().HasMaxLength(100);
        builder.Property(s => s.FontSize).IsRequired().HasDefaultValue(16);
    }
}