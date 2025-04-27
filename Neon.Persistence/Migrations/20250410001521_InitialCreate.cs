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
                name: "twitch");

            migrationBuilder.CreateTable(
                name: "bot_account",
                schema: "twitch",
                columns: table => new
                {
                    bot_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    bot_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    client_id = table.Column<string>(type: "text", nullable: false),
                    client_secret = table.Column<string>(type: "text", nullable: false),
                    access_token = table.Column<string>(type: "text", nullable: false),
                    redirect_uri = table.Column<string>(type: "text", nullable: false),
                    twitch_broadcaster_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bot_account", x => x.bot_account_id);
                });

            migrationBuilder.CreateTable(
                name: "twitch_account",
                schema: "twitch",
                columns: table => new
                {
                    twitch_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    broadcaster_id = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false),
                    login_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    display_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    type = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: true),
                    broadcaster_type = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false),
                    profile_image_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    offline_image_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    account_created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    neon_authorization_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    neon_authorization_revoke_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_authorization_revoked = table.Column<bool>(type: "boolean", nullable: true),
                    authorization_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    access_token = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    refresh_token = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    access_token_refresh_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    authorization_scopes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    web_socket_chat_url = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_twitch_account", x => x.twitch_account_id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bot_account",
                schema: "twitch");

            migrationBuilder.DropTable(
                name: "twitch_account",
                schema: "twitch");
        }
    }
}
