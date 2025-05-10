using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Neon.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class authrewrite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "access_token",
                schema: "twitch",
                table: "twitch_account");

            migrationBuilder.DropColumn(
                name: "access_token_refresh_date",
                schema: "twitch",
                table: "twitch_account");

            migrationBuilder.DropColumn(
                name: "authorization_code",
                schema: "twitch",
                table: "twitch_account");

            migrationBuilder.DropColumn(
                name: "authorization_scopes",
                schema: "twitch",
                table: "twitch_account");

            migrationBuilder.DropColumn(
                name: "refresh_token",
                schema: "twitch",
                table: "twitch_account");

            migrationBuilder.DropColumn(
                name: "web_socket_chat_url",
                schema: "twitch",
                table: "twitch_account");

            migrationBuilder.AlterColumn<bool>(
                name: "is_authorization_revoked",
                schema: "twitch",
                table: "twitch_account",
                type: "boolean",
                nullable: true,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "authorization_scope",
                schema: "twitch",
                columns: table => new
                {
                    authorization_scope_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_authorization_scope", x => x.authorization_scope_id);
                });

            migrationBuilder.CreateTable(
                name: "subscription_type",
                schema: "twitch",
                columns: table => new
                {
                    subscription_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    version = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_subscription_type", x => x.subscription_type_id);
                });

            migrationBuilder.CreateTable(
                name: "twitch_account_auth",
                schema: "twitch",
                columns: table => new
                {
                    twitch_account_auth_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    twitch_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    authorization_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    access_token = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    refresh_token = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_refresh_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_validation_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_twitch_account_auth", x => x.twitch_account_auth_id);
                    table.ForeignKey(
                        name: "fk_twitch_account_auth_twitch_account_twitch_account_id",
                        column: x => x.twitch_account_id,
                        principalSchema: "twitch",
                        principalTable: "twitch_account",
                        principalColumn: "twitch_account_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "twitch_account_scope",
                schema: "twitch",
                columns: table => new
                {
                    twitch_account_scope_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    twitch_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    authorization_scope_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_twitch_account_scope", x => x.twitch_account_scope_id);
                    table.ForeignKey(
                        name: "fk_twitch_account_scope_authorization_scope_authorization_scop",
                        column: x => x.authorization_scope_id,
                        principalSchema: "twitch",
                        principalTable: "authorization_scope",
                        principalColumn: "authorization_scope_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_twitch_account_scope_twitch_account_twitch_account_id",
                        column: x => x.twitch_account_id,
                        principalSchema: "twitch",
                        principalTable: "twitch_account",
                        principalColumn: "twitch_account_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "authorization_scope_subscription_type",
                schema: "twitch",
                columns: table => new
                {
                    authorization_scope_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subscription_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_authorization_scope_subscription_type", x => new { x.authorization_scope_id, x.subscription_type_id });
                    table.ForeignKey(
                        name: "fk_authorization_scope_subscription_type_authorization_scope_a",
                        column: x => x.authorization_scope_id,
                        principalSchema: "twitch",
                        principalTable: "authorization_scope",
                        principalColumn: "authorization_scope_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_authorization_scope_subscription_type_subscription_type_sub",
                        column: x => x.subscription_type_id,
                        principalSchema: "twitch",
                        principalTable: "subscription_type",
                        principalColumn: "subscription_type_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_twitch_account_broadcaster_id",
                schema: "twitch",
                table: "twitch_account",
                column: "broadcaster_id");

            migrationBuilder.CreateIndex(
                name: "ix_twitch_account_login_name",
                schema: "twitch",
                table: "twitch_account",
                column: "login_name");

            migrationBuilder.CreateIndex(
                name: "ix_authorization_scope_name",
                schema: "twitch",
                table: "authorization_scope",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_authorization_scope_subscription_type_subscription_type_id",
                schema: "twitch",
                table: "authorization_scope_subscription_type",
                column: "subscription_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_subscription_type_name",
                schema: "twitch",
                table: "subscription_type",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_twitch_account_auth_twitch_account_id",
                schema: "twitch",
                table: "twitch_account_auth",
                column: "twitch_account_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_twitch_account_scope_authorization_scope_id",
                schema: "twitch",
                table: "twitch_account_scope",
                column: "authorization_scope_id");

            migrationBuilder.CreateIndex(
                name: "ix_twitch_account_scope_twitch_account_id",
                schema: "twitch",
                table: "twitch_account_scope",
                column: "twitch_account_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "authorization_scope_subscription_type",
                schema: "twitch");

            migrationBuilder.DropTable(
                name: "twitch_account_auth",
                schema: "twitch");

            migrationBuilder.DropTable(
                name: "twitch_account_scope",
                schema: "twitch");

            migrationBuilder.DropTable(
                name: "subscription_type",
                schema: "twitch");

            migrationBuilder.DropTable(
                name: "authorization_scope",
                schema: "twitch");

            migrationBuilder.DropIndex(
                name: "ix_twitch_account_broadcaster_id",
                schema: "twitch",
                table: "twitch_account");

            migrationBuilder.DropIndex(
                name: "ix_twitch_account_login_name",
                schema: "twitch",
                table: "twitch_account");

            migrationBuilder.AlterColumn<bool>(
                name: "is_authorization_revoked",
                schema: "twitch",
                table: "twitch_account",
                type: "boolean",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldNullable: true,
                oldDefaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "access_token",
                schema: "twitch",
                table: "twitch_account",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "access_token_refresh_date",
                schema: "twitch",
                table: "twitch_account",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "authorization_code",
                schema: "twitch",
                table: "twitch_account",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "authorization_scopes",
                schema: "twitch",
                table: "twitch_account",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "refresh_token",
                schema: "twitch",
                table: "twitch_account",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "web_socket_chat_url",
                schema: "twitch",
                table: "twitch_account",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }
    }
}
