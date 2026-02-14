using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FUNewsManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddCreatedByIDToCategoriesAndTags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<short>(
                name: "CreatedByID",
                table: "Tag",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "CreatedByID",
                table: "Category",
                type: "smallint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedByID",
                table: "Tag");

            migrationBuilder.DropColumn(
                name: "CreatedByID",
                table: "Category");
        }
    }
}
