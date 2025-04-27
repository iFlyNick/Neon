using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Neon.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Twitch");

            migrationBuilder.CreateTable(
                name: "BotAccount",
                schema: "Twitch",
                columns: table => new
                {
                    BotAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    BotName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ClientId = table.Column<string>(type: "text", nullable: false),
                    ClientSecret = table.Column<string>(type: "text", nullable: false),
                    AccessToken = table.Column<string>(type: "text", nullable: false),
                    RedirectUri = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BotAccount", x => x.BotAccountId);
                });

            migrationBuilder.CreateTable(
                name: "TwitchAccount",
                schema: "Twitch",
                columns: table => new
                {
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TwitchAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    BroadcasterId = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false),
                    LoginName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Type = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: true),
                    BroadcasterType = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false),
                    ProfileImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    OfflineImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AccountCreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NeonAuthorizationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NeonAuthorizationRevokeDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsAuthorizationRevoked = table.Column<bool>(type: "boolean", nullable: true),
                    AuthorizationCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    AccessToken = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    RefreshToken = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    AccessTokenRefreshDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AuthorizationScopes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    WebSocketChatUrl = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TwitchAccount", x => x.TwitchAccountId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BotAccount",
                schema: "Twitch");

            migrationBuilder.DropTable(
                name: "TwitchAccount",
                schema: "Twitch");
        }
    }
}
