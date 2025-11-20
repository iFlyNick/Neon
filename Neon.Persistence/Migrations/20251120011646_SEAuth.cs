using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Neon.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SEAuth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "streamelements_auth",
                schema: "twitch",
                columns: table => new
                {
                    stream_elements_auth_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    twitch_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stream_elements_channel = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    jwt_token = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    jwt_token_iv = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_streamelements_auth", x => x.stream_elements_auth_id);
                    table.ForeignKey(
                        name: "fk_streamelements_auth_twitch_account_twitch_account_id",
                        column: x => x.twitch_account_id,
                        principalSchema: "twitch",
                        principalTable: "twitch_account",
                        principalColumn: "twitch_account_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_streamelements_auth_twitch_account_id",
                schema: "twitch",
                table: "streamelements_auth",
                column: "twitch_account_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "streamelements_auth",
                schema: "twitch");
        }
    }
}
