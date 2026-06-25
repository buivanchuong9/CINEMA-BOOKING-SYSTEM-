using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BE.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCinemaIdToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CinemaId",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_CinemaId",
                table: "AspNetUsers",
                column: "CinemaId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Cinemas_CinemaId",
                table: "AspNetUsers",
                column: "CinemaId",
                principalTable: "Cinemas",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Cinemas_CinemaId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_CinemaId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "CinemaId",
                table: "AspNetUsers");
        }
    }
}
