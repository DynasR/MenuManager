using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MenuManager.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddMealSlotItemOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Order",
                table: "MealSlotItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Order",
                table: "MealSlotItems");
        }
    }
}
