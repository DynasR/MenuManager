using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MenuManager.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddMealTypesToCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AllowedMealTypes",
                table: "Categories",
                type: "integer",
                nullable: false,
                defaultValue: 15);

            // Set specific values for seeded categories
            migrationBuilder.Sql("""
                UPDATE "Categories" SET "AllowedMealTypes" = 9  WHERE "Id" = 1;
                UPDATE "Categories" SET "AllowedMealTypes" = 14 WHERE "Id" = 2;
                UPDATE "Categories" SET "AllowedMealTypes" = 7  WHERE "Id" = 3;
                UPDATE "Categories" SET "AllowedMealTypes" = 6  WHERE "Id" = 4;
                UPDATE "Categories" SET "AllowedMealTypes" = 15 WHERE "Id" = 5;
                UPDATE "Categories" SET "AllowedMealTypes" = 15 WHERE "Id" = 6;
                UPDATE "Categories" SET "AllowedMealTypes" = 6  WHERE "Id" = 7;
                UPDATE "Categories" SET "AllowedMealTypes" = 6  WHERE "Id" = 8;
                UPDATE "Categories" SET "AllowedMealTypes" = 15 WHERE "Id" = 9;
                UPDATE "Categories" SET "AllowedMealTypes" = 9  WHERE "Id" = 10;
                UPDATE "Categories" SET "AllowedMealTypes" = 6  WHERE "Id" = 11;
                UPDATE "Categories" SET "AllowedMealTypes" = 0  WHERE "Id" = 12;
                UPDATE "Categories" SET "AllowedMealTypes" = 9  WHERE "Id" = 13;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowedMealTypes",
                table: "Categories");
        }
    }
}
