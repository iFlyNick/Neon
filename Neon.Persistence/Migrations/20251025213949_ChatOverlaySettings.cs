using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Neon.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ChatOverlaySettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "twitch_chat_overlay_settings",
                schema: "twitch",
                columns: table => new
                {
                    twitch_chat_overlay_settings_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    twitch_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    overlay_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    overlay_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    chat_style = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ignore_bot_messages = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ignore_command_messages = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    use_twitch_badges = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    use_better_ttv_emotes = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    use_seven_tv_emotes = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    use_ffz_emotes = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    chat_delay_milliseconds = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    always_keep_messages = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    chat_message_remove_delay_milliseconds = table.Column<int>(type: "integer", nullable: false, defaultValue: 300000),
                    font_family = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    font_size = table.Column<int>(type: "integer", nullable: false, defaultValue: 16)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_twitch_chat_overlay_settings", x => x.twitch_chat_overlay_settings_id);
                    table.ForeignKey(
                        name: "fk_twitch_chat_overlay_settings_twitch_account_twitch_account_",
                        column: x => x.twitch_account_id,
                        principalSchema: "twitch",
                        principalTable: "twitch_account",
                        principalColumn: "twitch_account_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_twitch_chat_overlay_settings_twitch_account_id",
                schema: "twitch",
                table: "twitch_chat_overlay_settings",
                column: "twitch_account_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "twitch_chat_overlay_settings",
                schema: "twitch");
        }
    }
}
