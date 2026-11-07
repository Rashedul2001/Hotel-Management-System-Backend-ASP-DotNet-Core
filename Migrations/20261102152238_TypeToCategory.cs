using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hotel_Management_System_Backend_dotNet.Migrations
{
    /// <inheritdoc />
    public partial class TypeToCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Type",
                table: "Rooms",
                newName: "Category");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Category",
                table: "Rooms",
                newName: "Type");
        }
    }
}
