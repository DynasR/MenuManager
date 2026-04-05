using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MenuManager.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddUnitAndOrderToRecipeIngredient : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // RecipeIngredients — add Unit (default 0, then set = Item.PurchaseUnit)
            migrationBuilder.AddColumn<int>(
                name: "Unit",
                table: "RecipeIngredients",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(@"
                UPDATE ""RecipeIngredients"" ri
                SET ""Unit"" = i.""PurchaseUnit""
                FROM ""Items"" i
                WHERE i.""Id"" = ri.""ItemId""
            ");

            migrationBuilder.AlterColumn<int>(
                name: "Unit",
                table: "RecipeIngredients",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0);

            // RecipeIngredients — add Order (default 0, then set = ItemId for stable ordering)
            migrationBuilder.AddColumn<int>(
                name: "Order",
                table: "RecipeIngredients",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(@"UPDATE ""RecipeIngredients"" SET ""Order"" = ""ItemId""");

            migrationBuilder.AlterColumn<int>(
                name: "Order",
                table: "RecipeIngredients",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0);

            // MealItems — add Unit (default 0, then set = Item.PurchaseUnit for item-based rows)
            migrationBuilder.AddColumn<int>(
                name: "Unit",
                table: "MealItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(@"
                UPDATE ""MealItems"" mi
                SET ""Unit"" = i.""PurchaseUnit""
                FROM ""Items"" i
                WHERE i.""Id"" = mi.""ItemId""
            ");

            migrationBuilder.AlterColumn<int>(
                name: "Unit",
                table: "MealItems",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Unit",  table: "RecipeIngredients");
            migrationBuilder.DropColumn(name: "Order", table: "RecipeIngredients");
            migrationBuilder.DropColumn(name: "Unit",  table: "MealItems");
        }
    }
}
