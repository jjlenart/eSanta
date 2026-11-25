using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eSanta.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddShoutboxMentions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ShoutboxMentions",
                columns: table => new
                {
                    MessageId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShoutboxMentions", x => new { x.MessageId, x.UserId });
                    table.ForeignKey(
                        name: "FK_ShoutboxMentions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShoutboxMentions_ShoutboxMessages_MessageId",
                        column: x => x.MessageId,
                        principalTable: "ShoutboxMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShoutboxMentions_UserId",
                table: "ShoutboxMentions",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShoutboxMentions");
        }
    }
}
