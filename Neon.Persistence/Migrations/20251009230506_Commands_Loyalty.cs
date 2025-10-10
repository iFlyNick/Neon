using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Neon.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Commands_Loyalty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "twitch_account_loyalty",
                schema: "twitch",
                columns: table => new
                {
                    twitch_account_loyalty_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    twitch_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    loyalty_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_loyalty_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    point_interval_minutes = table.Column<int>(type: "integer", nullable: false, defaultValue: 10),
                    base_points_per_interval = table.Column<int>(type: "integer", nullable: false, defaultValue: 50),
                    base_point_modifier = table.Column<double>(type: "double precision", nullable: false, defaultValue: 1.0),
                    subscriber_point_modifier = table.Column<double>(type: "double precision", nullable: false, defaultValue: 2.0),
                    follower_bonus_points = table.Column<int>(type: "integer", nullable: false, defaultValue: 300),
                    subscriber_bonus_points = table.Column<int>(type: "integer", nullable: false, defaultValue: 500),
                    enabled_for_viewers_only = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_twitch_account_loyalty", x => x.twitch_account_loyalty_id);
                    table.ForeignKey(
                        name: "fk_twitch_account_loyalty_twitch_account_twitch_account_id",
                        column: x => x.twitch_account_id,
                        principalSchema: "twitch",
                        principalTable: "twitch_account",
                        principalColumn: "twitch_account_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "twitch_channel_command",
                schema: "twitch",
                columns: table => new
                {
                    twitch_channel_command_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    twitch_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    command_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    command_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    command_response = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    cooldown_seconds = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_twitch_channel_command", x => x.twitch_channel_command_id);
                    table.ForeignKey(
                        name: "fk_twitch_channel_command_twitch_account_twitch_account_id",
                        column: x => x.twitch_account_id,
                        principalSchema: "twitch",
                        principalTable: "twitch_account",
                        principalColumn: "twitch_account_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "loyalty_tracking",
                schema: "twitch",
                columns: table => new
                {
                    loyalty_tracking_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    twitch_account_loyalty_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false),
                    user_login = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    points = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_loyalty_tracking", x => x.loyalty_tracking_id);
                    table.ForeignKey(
                        name: "fk_loyalty_tracking_twitch_account_loyalty_twitch_account_loya",
                        column: x => x.twitch_account_loyalty_id,
                        principalSchema: "twitch",
                        principalTable: "twitch_account_loyalty",
                        principalColumn: "twitch_account_loyalty_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_loyalty_tracking_twitch_account_loyalty_id",
                schema: "twitch",
                table: "loyalty_tracking",
                column: "twitch_account_loyalty_id");

            migrationBuilder.CreateIndex(
                name: "ix_loyalty_tracking_user_login",
                schema: "twitch",
                table: "loyalty_tracking",
                column: "user_login");

            migrationBuilder.CreateIndex(
                name: "ix_twitch_account_loyalty_twitch_account_id",
                schema: "twitch",
                table: "twitch_account_loyalty",
                column: "twitch_account_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_twitch_channel_command_twitch_account_id",
                schema: "twitch",
                table: "twitch_channel_command",
                column: "twitch_account_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "loyalty_tracking",
                schema: "twitch");

            migrationBuilder.DropTable(
                name: "twitch_channel_command",
                schema: "twitch");

            migrationBuilder.DropTable(
                name: "twitch_account_loyalty",
                schema: "twitch");
        }
    }
}
