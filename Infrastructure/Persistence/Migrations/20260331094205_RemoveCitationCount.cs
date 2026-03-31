using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ResearchPublications.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCitationCount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CitationCount",
                table: "Publications");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CitationCount",
                table: "Publications",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
