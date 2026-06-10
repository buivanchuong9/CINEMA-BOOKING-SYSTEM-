using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BE.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRefundFieldsToBooking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RefundAccountName",
                table: "Bookings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefundAccountNumber",
                table: "Bookings",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefundBankName",
                table: "Bookings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefundProofUrl",
                table: "Bookings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefundStatus",
                table: "Bookings",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RefundAccountName",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "RefundAccountNumber",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "RefundBankName",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "RefundProofUrl",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "RefundStatus",
                table: "Bookings");
        }
    }
}
