using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace eSanta.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddShoppingList : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ShoppingListEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OwnerId = table.Column<string>(type: "text", nullable: false),
                    RecipientUserId = table.Column<string>(type: "text", nullable: true),
                    AttendeeId = table.Column<int>(type: "integer", nullable: true),
                    RecipientName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    GiftName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsBought = table.Column<bool>(type: "boolean", nullable: false),
                    Note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShoppingListEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShoppingListEntries_AspNetUsers_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShoppingListEntries_AspNetUsers_RecipientUserId",
                        column: x => x.RecipientUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ShoppingListEntries_Attendees_AttendeeId",
                        column: x => x.AttendeeId,
                        principalTable: "Attendees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingListEntries_AttendeeId",
                table: "ShoppingListEntries",
                column: "AttendeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingListEntries_OwnerId_AttendeeId",
                table: "ShoppingListEntries",
                columns: new[] { "OwnerId", "AttendeeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingListEntries_OwnerId_RecipientUserId",
                table: "ShoppingListEntries",
                columns: new[] { "OwnerId", "RecipientUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingListEntries_RecipientUserId",
                table: "ShoppingListEntries",
                column: "RecipientUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShoppingListEntries");
        }
    }
}
