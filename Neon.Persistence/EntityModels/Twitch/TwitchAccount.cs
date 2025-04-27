﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Neon.Persistence.EntityModels.Twitch;

public class TwitchAccount : BaseModel
{
    public Guid? TwitchAccountId { get; set; }
    public string? BroadcasterId { get; set; }
    public string? LoginName { get; set; }
    public string? DisplayName { get; set; }
    public string? Type { get; set; }
    public string? BroadcasterType { get; set; }
    public string? ProfileImageUrl { get; set; }
    public string? OfflineImageUrl { get; set; }
    public DateTime? AccountCreatedDate { get; set; }
    public DateTime? NeonAuthorizationDate { get; set; }
    public DateTime? NeonAuthorizationRevokeDate { get; set; }
    public bool? IsAuthorizationRevoked { get; set; }
    public string? AuthorizationCode { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? AccessTokenRefreshDate { get; set; }
    public string? AuthorizationScopes { get; set; }
    public string? WebSocketChatUrl { get; set; }
}

public class TwitchAccountConfiguration : IEntityTypeConfiguration<TwitchAccount>
{
    public void Configure(EntityTypeBuilder<TwitchAccount> builder)
    {
        //schema
        builder.ToTable("twitch_account", "twitch");
        
        //pk
        builder.HasKey(s => s.TwitchAccountId);

        //indexes


        //relationships

        //base model
        builder.Property(s => s.CreatedDate).HasColumnOrder(2).IsRequired();
        builder.Property(s => s.CreatedBy).HasColumnOrder(3).IsRequired().HasMaxLength(50);
        builder.Property(s => s.ModifiedDate).HasColumnOrder(4);
        builder.Property(s => s.ModifiedBy).HasColumnOrder(5).HasMaxLength(50);

        //columns
        builder.Property(s => s.TwitchAccountId).HasColumnOrder(1).ValueGeneratedOnAdd();
        builder.Property(s => s.BroadcasterId).IsRequired().HasMaxLength(25);
        builder.Property(s => s.LoginName).IsRequired().HasMaxLength(50);
        builder.Property(s => s.DisplayName).IsRequired().HasMaxLength(50);
        builder.Property(s => s.Type).HasMaxLength(25);
        builder.Property(s => s.BroadcasterType).IsRequired().HasMaxLength(25);
        builder.Property(s => s.ProfileImageUrl).HasMaxLength(500);
        builder.Property(s => s.OfflineImageUrl).HasMaxLength(500);
        builder.Property(s => s.AccountCreatedDate).IsRequired();
        builder.Property(s => s.NeonAuthorizationDate);
        builder.Property(s => s.AuthorizationCode).HasMaxLength(50);
        builder.Property(s => s.AccessToken).HasMaxLength(50);
        builder.Property(s => s.RefreshToken).HasMaxLength(50);
        builder.Property(s => s.AccessTokenRefreshDate);
        builder.Property(s => s.AuthorizationScopes).HasMaxLength(500);
        builder.Property(s => s.WebSocketChatUrl).HasMaxLength(200);
    }
}