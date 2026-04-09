using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProductStore.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixMalformedCategoryGuids : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Corrige Categories/CategoryId gerados pela expressão antiga (segmentos com 3 hex em vez de 4).
            // Fora da transação da migração: PRAGMA foreign_keys não desliga FKs dentro de transação ativa.
            const string fixSql = """
                CREATE TABLE "__CatGuidMap" (
                  "OldId" TEXT NOT NULL PRIMARY KEY,
                  "NewId" TEXT NOT NULL
                );

                INSERT INTO "__CatGuidMap" ("OldId", "NewId")
                SELECT
                  "c"."Id",
                  LOWER(HEX(RANDOMBLOB(4))) || '-' ||
                  LOWER(HEX(RANDOMBLOB(2))) || '-' || '4' ||
                  SUBSTR(LOWER(HEX(RANDOMBLOB(2))), 2) || '-' ||
                  SUBSTR('89ab', (ABS(RANDOM()) % 4) + 1, 1) ||
                  SUBSTR(LOWER(HEX(RANDOMBLOB(2))), 2) || '-' ||
                  LOWER(HEX(RANDOMBLOB(6)))
                FROM "Categories" AS "c"
                WHERE LENGTH("c"."Id") <> 36;

                UPDATE "Products"
                SET "CategoryId" = (
                  SELECT "m"."NewId" FROM "__CatGuidMap" AS "m"
                  WHERE "m"."OldId" = "Products"."CategoryId"
                )
                WHERE "CategoryId" IN (SELECT "OldId" FROM "__CatGuidMap");

                UPDATE "Categories"
                SET "Id" = (
                  SELECT "m"."NewId" FROM "__CatGuidMap" AS "m"
                  WHERE "m"."OldId" = "Categories"."Id"
                )
                WHERE "Id" IN (SELECT "OldId" FROM "__CatGuidMap");

                DROP TABLE "__CatGuidMap";
                """;

            migrationBuilder.Sql("PRAGMA foreign_keys = 0;", suppressTransaction: true);
            migrationBuilder.Sql(fixSql, suppressTransaction: true);
            migrationBuilder.Sql("PRAGMA foreign_keys = 1;", suppressTransaction: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
