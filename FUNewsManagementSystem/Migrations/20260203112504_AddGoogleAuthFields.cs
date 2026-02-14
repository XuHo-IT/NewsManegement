using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FUNewsManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddGoogleAuthFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add GoogleId column to SystemAccount table
            migrationBuilder.AddColumn<string>(
                name: "GoogleId",
                table: "SystemAccount",
                type: "nvarchar(max)",
                nullable: true);

            // Add AvatarUrl column to SystemAccount table
            migrationBuilder.AddColumn<string>(
                name: "AvatarUrl",
                table: "SystemAccount",
                type: "nvarchar(max)",
                nullable: true);

            // Add ImageUrl column to NewsArticle table
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "NewsArticle",
                type: "nvarchar(max)",
                nullable: true);

            // Add ImageUrl column to Category table
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Category",
                type: "nvarchar(max)",
                nullable: true);

            // Add ImageUrl column to Tag table
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Tag",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GoogleId",
                table: "SystemAccount");

            migrationBuilder.DropColumn(
                name: "AvatarUrl",
                table: "SystemAccount");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "NewsArticle");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Category");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Tag");
        }
    }
}
