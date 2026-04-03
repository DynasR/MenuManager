using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MenuManager.Server.Migrations
{
    /// <inheritdoc />
    public partial class RefactorItemUnitAndPackageSize : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Quantity",
                table: "Items",
                newName: "PackageSize");

            // PostgreSQL cannot auto-cast text → integer; use raw SQL with USING
            migrationBuilder.Sql(
                """
                ALTER TABLE "Items"
                ALTER COLUMN "Unit" TYPE integer
                USING 0;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PackageSize",
                table: "Items",
                newName: "Quantity");

            migrationBuilder.AlterColumn<string>(
                name: "Unit",
                table: "Items",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");
        }
    }
}
