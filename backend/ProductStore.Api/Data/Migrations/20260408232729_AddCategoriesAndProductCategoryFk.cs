using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProductStore.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoriesAndProductCategoryFk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.AddColumn<Guid>(
                name: "CategoryId",
                table: "Products",
                type: "TEXT",
                nullable: true);

            // Uma linha por nome de categoria distinto (ignora maiúsculas), com UUID texto compatível com Guid do .NET
            // SUBSTR(..., 3, 3) em HEX de 4 caracteres devolve só 2 chars (índice 1-based no SQLite) — GUID inválido.
            // '4' + SUBSTR(HEX, 2) = 1 + 3 = 4 hex no 3.º segmento; idem para o 4.º com o nibble de variante.
            migrationBuilder.Sql("""
                INSERT INTO "Categories" ("Id", "Name")
                SELECT
                  LOWER(HEX(RANDOMBLOB(4))) || '-' ||
                  LOWER(HEX(RANDOMBLOB(2))) || '-' || '4' ||
                  SUBSTR(LOWER(HEX(RANDOMBLOB(2))), 2) || '-' ||
                  SUBSTR('89ab', (ABS(RANDOM()) % 4) + 1, 1) ||
                  SUBSTR(LOWER(HEX(RANDOMBLOB(2))), 2) || '-' ||
                  LOWER(HEX(RANDOMBLOB(6))),
                  MIN("Category")
                FROM "Products"
                GROUP BY LOWER(TRIM("Category"));
                """);

            migrationBuilder.Sql("""
                UPDATE "Products"
                SET "CategoryId" = (
                  SELECT "c"."Id" FROM "Categories" AS "c"
                  WHERE LOWER(TRIM("c"."Name")) = LOWER(TRIM("Products"."Category"))
                  LIMIT 1
                );
                """);

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Products");

            migrationBuilder.AlterColumn<Guid>(
                name: "CategoryId",
                table: "Products",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_CategoryId",
                table: "Products",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Categories_CategoryId",
                table: "Products",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_Categories_CategoryId",
                table: "Products");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Products_CategoryId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Products");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Products",
                type: "TEXT",
                maxLength: 128,
                nullable: false,
                defaultValue: "");
        }
    }
}
