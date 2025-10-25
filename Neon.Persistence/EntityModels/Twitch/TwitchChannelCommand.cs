using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Neon.Persistence.EntityModels.Twitch;

public class TwitchChannelCommand : BaseModel
{
    public Guid? TwitchChannelCommandId { get; set; }
    public Guid? TwitchAccountId { get; set; }
    public string? CommandType { get; set; }
    public string? CommandName { get; set; }
    public string? CommandResponse { get; set; }
    public bool? IsEnabled { get; set; }
    public int? CooldownSeconds { get; set; }
    
    public TwitchAccount? TwitchAccount { get; set; }
}

public class TwitchChannelCommandConfiguration : IEntityTypeConfiguration<TwitchChannelCommand>
{
    public void Configure(EntityTypeBuilder<TwitchChannelCommand> builder)
    {
        //schema
        builder.ToTable("twitch_channel_command", "twitch");
        
        //pk
        builder.HasKey(s => s.TwitchChannelCommandId);
        
        //indexes
        builder.HasIndex(s => s.TwitchAccountId);
        
        //relationships
        builder.HasOne(s => s.TwitchAccount).WithMany(s => s.TwitchChannelCommands).HasForeignKey(s => s.TwitchAccountId);

        //base model
        builder.Property(s => s.CreatedDate).HasColumnOrder(2).IsRequired();
        builder.Property(s => s.CreatedBy).HasColumnOrder(3).IsRequired().HasMaxLength(50);
        builder.Property(s => s.ModifiedDate).HasColumnOrder(4);
        builder.Property(s => s.ModifiedBy).HasColumnOrder(5).HasMaxLength(50);

        //columns
        builder.Property(s => s.TwitchChannelCommandId).HasColumnOrder(1).ValueGeneratedOnAdd();
        builder.Property(s => s.TwitchAccountId).IsRequired();
        builder.Property(s => s.CommandType).IsRequired().HasMaxLength(100);
        builder.Property(s => s.CommandName).IsRequired().HasMaxLength(100);
        builder.Property(s => s.CommandResponse).IsRequired().HasMaxLength(1000);
        builder.Property(s => s.IsEnabled).IsRequired().HasDefaultValue(true);
        builder.Property(s => s.CooldownSeconds).IsRequired().HasDefaultValue(0);
    }
}