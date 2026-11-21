using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eSanta.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendeeUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Attendees",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Attendees_UserId",
                table: "Attendees",
                column: "UserId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Attendees_AspNetUsers_UserId",
                table: "Attendees",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attendees_AspNetUsers_UserId",
                table: "Attendees");

            migrationBuilder.DropIndex(
                name: "IX_Attendees_UserId",
                table: "Attendees");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Attendees");
        }
    }
}
