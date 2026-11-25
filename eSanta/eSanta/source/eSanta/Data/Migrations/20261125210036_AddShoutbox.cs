using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace eSanta.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddShoutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ShoutboxMessages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FamilyId = table.Column<int>(type: "integer", nullable: false),
                    AuthorId = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SentAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShoutboxMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShoutboxMessages_AspNetUsers_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShoutboxMessages_Families_FamilyId",
                        column: x => x.FamilyId,
                        principalTable: "Families",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShoutboxMessages_AuthorId",
                table: "ShoutboxMessages",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_ShoutboxMessages_FamilyId_Id",
                table: "ShoutboxMessages",
                columns: new[] { "FamilyId", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShoutboxMessages");
        }
    }
}
