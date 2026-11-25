using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eSanta.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendeeFamily : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FamilyId",
                table: "Attendees",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Attendees_FamilyId",
                table: "Attendees",
                column: "FamilyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Attendees_Families_FamilyId",
                table: "Attendees",
                column: "FamilyId",
                principalTable: "Families",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attendees_Families_FamilyId",
                table: "Attendees");

            migrationBuilder.DropIndex(
                name: "IX_Attendees_FamilyId",
                table: "Attendees");

            migrationBuilder.DropColumn(
                name: "FamilyId",
                table: "Attendees");
        }
    }
}
