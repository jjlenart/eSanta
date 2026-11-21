using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eSanta.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWishlistReservations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReservedById",
                table: "WishlistItems",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WishlistItems_ReservedById",
                table: "WishlistItems",
                column: "ReservedById");

            migrationBuilder.AddForeignKey(
                name: "FK_WishlistItems_AspNetUsers_ReservedById",
                table: "WishlistItems",
                column: "ReservedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WishlistItems_AspNetUsers_ReservedById",
                table: "WishlistItems");

            migrationBuilder.DropIndex(
                name: "IX_WishlistItems_ReservedById",
                table: "WishlistItems");

            migrationBuilder.DropColumn(
                name: "ReservedById",
                table: "WishlistItems");
        }
    }
}
