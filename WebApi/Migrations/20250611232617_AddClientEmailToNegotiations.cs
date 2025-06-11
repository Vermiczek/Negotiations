using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Negotiations.Migrations
{
    /// <inheritdoc />
    public partial class AddClientEmailToNegotiations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClientEmail",
                table: "Negotiations",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ClientName",
                table: "Negotiations",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClientEmail",
                table: "Negotiations");

            migrationBuilder.DropColumn(
                name: "ClientName",
                table: "Negotiations");
        }
    }
}
