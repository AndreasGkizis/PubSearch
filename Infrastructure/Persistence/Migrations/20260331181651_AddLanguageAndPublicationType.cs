using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ResearchPublications.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLanguageAndPublicationType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Languages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Value = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Languages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PublicationTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Value = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicationTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PublicationLanguages",
                columns: table => new
                {
                    LanguagesId = table.Column<int>(type: "int", nullable: false),
                    PublicationsId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicationLanguages", x => new { x.LanguagesId, x.PublicationsId });
                    table.ForeignKey(
                        name: "FK_PublicationLanguages_Languages_LanguagesId",
                        column: x => x.LanguagesId,
                        principalTable: "Languages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PublicationLanguages_Publications_PublicationsId",
                        column: x => x.PublicationsId,
                        principalTable: "Publications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PublicationPublicationTypes",
                columns: table => new
                {
                    PublicationTypesId = table.Column<int>(type: "int", nullable: false),
                    PublicationsId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicationPublicationTypes", x => new { x.PublicationTypesId, x.PublicationsId });
                    table.ForeignKey(
                        name: "FK_PublicationPublicationTypes_PublicationTypes_PublicationTypesId",
                        column: x => x.PublicationTypesId,
                        principalTable: "PublicationTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PublicationPublicationTypes_Publications_PublicationsId",
                        column: x => x.PublicationsId,
                        principalTable: "Publications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Languages_Value",
                table: "Languages",
                column: "Value",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PublicationLanguages_PublicationsId",
                table: "PublicationLanguages",
                column: "PublicationsId");

            migrationBuilder.CreateIndex(
                name: "IX_PublicationPublicationTypes_PublicationsId",
                table: "PublicationPublicationTypes",
                column: "PublicationsId");

            migrationBuilder.CreateIndex(
                name: "IX_PublicationTypes_Value",
                table: "PublicationTypes",
                column: "Value",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PublicationLanguages");

            migrationBuilder.DropTable(
                name: "PublicationPublicationTypes");

            migrationBuilder.DropTable(
                name: "Languages");

            migrationBuilder.DropTable(
                name: "PublicationTypes");
        }
    }
}
