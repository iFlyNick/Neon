using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Neon.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AppAccountChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bot_account",
                schema: "twitch");

            migrationBuilder.CreateTable(
                name: "app_account",
                schema: "twitch",
                columns: table => new
                {
                    app_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    app_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    client_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    client_secret = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    access_token = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    redirect_uri = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_app_account", x => x.app_account_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_app_account_app_name",
                schema: "twitch",
                table: "app_account",
                column: "app_name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "app_account",
                schema: "twitch");

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
                    access_token = table.Column<string>(type: "text", nullable: false),
                    bot_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    client_id = table.Column<string>(type: "text", nullable: false),
                    client_secret = table.Column<string>(type: "text", nullable: false),
                    redirect_uri = table.Column<string>(type: "text", nullable: false),
                    twitch_broadcaster_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bot_account", x => x.bot_account_id);
                });
        }
    }
}
