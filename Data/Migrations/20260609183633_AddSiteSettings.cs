using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BE.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSiteSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SiteSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SiteName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SiteSlogan = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LogoIcon = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LogoUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContactAddress = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContactPhone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContactEmail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PrimaryColor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SecondaryColor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BgPrimaryColor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BgSecondaryColor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HomeMoviesCount = table.Column<int>(type: "int", nullable: false),
                    MovieDisplayMode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MovieGridColumns = table.Column<int>(type: "int", nullable: false),
                    ShowComingSoon = table.Column<bool>(type: "bit", nullable: false),
                    ComingSoonCount = table.Column<int>(type: "int", nullable: false),
                    ShowMovieRating = table.Column<bool>(type: "bit", nullable: false),
                    ShowMovieGenre = table.Column<bool>(type: "bit", nullable: false),
                    ShowMovieDuration = table.Column<bool>(type: "bit", nullable: false),
                    HeroSliderHeight = table.Column<int>(type: "int", nullable: false),
                    EnableHeroSlider = table.Column<bool>(type: "bit", nullable: false),
                    FontFamily = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteSettings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SiteSettings");
        }
    }
}
