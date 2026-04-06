using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MenuManager.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueDailyMenuDateConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remove orphaned meal items and meals from duplicate DailyMenus,
            // keeping only the lowest-Id row per (CustomerId, Date).
            migrationBuilder.Sql("""
                DELETE FROM "MealItems"
                WHERE "MealId" IN (
                    SELECT "Id" FROM "Meals"
                    WHERE "DailyMenuId" IN (
                        SELECT "Id" FROM "DailyMenus"
                        WHERE "Id" NOT IN (
                            SELECT MIN("Id") FROM "DailyMenus"
                            GROUP BY "CustomerId", "Date"
                        )
                    )
                );

                DELETE FROM "Meals"
                WHERE "DailyMenuId" IN (
                    SELECT "Id" FROM "DailyMenus"
                    WHERE "Id" NOT IN (
                        SELECT MIN("Id") FROM "DailyMenus"
                        GROUP BY "CustomerId", "Date"
                    )
                );

                DELETE FROM "DailyMenus"
                WHERE "Id" NOT IN (
                    SELECT MIN("Id") FROM "DailyMenus"
                    GROUP BY "CustomerId", "Date"
                );
                """);

            migrationBuilder.DropIndex(
                name: "IX_DailyMenus_CustomerId",
                table: "DailyMenus");

            migrationBuilder.CreateIndex(
                name: "IX_DailyMenus_CustomerId_Date",
                table: "DailyMenus",
                columns: new[] { "CustomerId", "Date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DailyMenus_CustomerId_Date",
                table: "DailyMenus");

            migrationBuilder.CreateIndex(
                name: "IX_DailyMenus_CustomerId",
                table: "DailyMenus",
                column: "CustomerId");
        }
    }
}
