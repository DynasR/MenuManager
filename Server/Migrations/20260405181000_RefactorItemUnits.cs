using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MenuManager.Server.Migrations
{
    /// <inheritdoc />
    public partial class RefactorItemUnits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Items — rename Unit → PurchaseUnit, PackageSize → ContentQuantity
            migrationBuilder.RenameColumn(
                name: "Unit",
                table: "Items",
                newName: "PurchaseUnit");

            migrationBuilder.RenameColumn(
                name: "PackageSize",
                table: "Items",
                newName: "ContentQuantity");

            // Items — add ContentUnit (default 0 during migration, then set = PurchaseUnit)
            migrationBuilder.AddColumn<int>(
                name: "ContentUnit",
                table: "Items",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("UPDATE \"Items\" SET \"ContentUnit\" = \"PurchaseUnit\"");

            migrationBuilder.AlterColumn<int>(
                name: "ContentUnit",
                table: "Items",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0);

            // Items — drop IsStaple, MonthlyEstimate
            migrationBuilder.DropColumn(
                name: "IsStaple",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "MonthlyEstimate",
                table: "Items");

            // MealItems — drop Servings
            migrationBuilder.DropColumn(
                name: "Servings",
                table: "MealItems");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // MealItems — restore Servings
            migrationBuilder.AddColumn<decimal>(
                name: "Servings",
                table: "MealItems",
                type: "numeric(10,3)",
                precision: 10,
                scale: 3,
                nullable: false,
                defaultValue: 1m);

            // Items — restore IsStaple, MonthlyEstimate
            migrationBuilder.AddColumn<bool>(
                name: "IsStaple",
                table: "Items",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "MonthlyEstimate",
                table: "Items",
                type: "numeric(10,3)",
                precision: 10,
                scale: 3,
                nullable: true);

            // Items — drop ContentUnit, rename back
            migrationBuilder.DropColumn(
                name: "ContentUnit",
                table: "Items");

            migrationBuilder.RenameColumn(
                name: "PurchaseUnit",
                table: "Items",
                newName: "Unit");

            migrationBuilder.RenameColumn(
                name: "ContentQuantity",
                table: "Items",
                newName: "PackageSize");
        }
    }
}
