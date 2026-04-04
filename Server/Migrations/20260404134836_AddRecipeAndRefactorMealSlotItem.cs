using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MenuManager.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddRecipeAndRefactorMealSlotItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MealSlotItems_Items_ItemId",
                table: "MealSlotItems");

            migrationBuilder.AlterColumn<int>(
                name: "ItemId",
                table: "MealSlotItems",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "RecipeId",
                table: "MealSlotItems",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Servings",
                table: "MealSlotItems",
                type: "numeric(10,3)",
                precision: 10,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

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

            migrationBuilder.CreateTable(
                name: "Recipes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    BaseServings = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recipes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RecipeIngredients",
                columns: table => new
                {
                    RecipeId = table.Column<int>(type: "integer", nullable: false),
                    ItemId = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecipeIngredients", x => new { x.RecipeId, x.ItemId });
                    table.ForeignKey(
                        name: "FK_RecipeIngredients_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RecipeIngredients_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "Recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MealSlotItems_RecipeId",
                table: "MealSlotItems",
                column: "RecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeIngredients_ItemId",
                table: "RecipeIngredients",
                column: "ItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_MealSlotItems_Items_ItemId",
                table: "MealSlotItems",
                column: "ItemId",
                principalTable: "Items",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MealSlotItems_Recipes_RecipeId",
                table: "MealSlotItems",
                column: "RecipeId",
                principalTable: "Recipes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MealSlotItems_Items_ItemId",
                table: "MealSlotItems");

            migrationBuilder.DropForeignKey(
                name: "FK_MealSlotItems_Recipes_RecipeId",
                table: "MealSlotItems");

            migrationBuilder.DropTable(
                name: "RecipeIngredients");

            migrationBuilder.DropTable(
                name: "Recipes");

            migrationBuilder.DropIndex(
                name: "IX_MealSlotItems_RecipeId",
                table: "MealSlotItems");

            migrationBuilder.DropColumn(
                name: "RecipeId",
                table: "MealSlotItems");

            migrationBuilder.DropColumn(
                name: "Servings",
                table: "MealSlotItems");

            migrationBuilder.DropColumn(
                name: "IsStaple",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "MonthlyEstimate",
                table: "Items");

            migrationBuilder.AlterColumn<int>(
                name: "ItemId",
                table: "MealSlotItems",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_MealSlotItems_Items_ItemId",
                table: "MealSlotItems",
                column: "ItemId",
                principalTable: "Items",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
