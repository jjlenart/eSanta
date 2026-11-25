using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eSanta.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddShoutboxAnonymousMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAnonymous",
                table: "ShoutboxMessages",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAnonymous",
                table: "ShoutboxMessages");
        }
    }
}
