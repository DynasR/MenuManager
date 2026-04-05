using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MenuManager.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentTypeCheckConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_Supplier_PaymentType",
                table: "Suppliers",
                sql: "\"PaymentType\" IN (0, 1)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Customer_PaymentType",
                table: "Customers",
                sql: "\"PaymentType\" IN (0, 1)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Supplier_PaymentType",
                table: "Suppliers");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Customer_PaymentType",
                table: "Customers");
        }
    }
}
