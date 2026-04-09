using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProductStore.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryNormalizedNameIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NormalizedName",
                table: "Categories",
                type: "TEXT",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            // Preenchimento mínimo para satisfazer o índice único antes do backfill em C# (acentos: CategoryNormalizedNameSync.AfterMigrate).
            migrationBuilder.Sql("UPDATE Categories SET NormalizedName = LOWER(TRIM(Name)) WHERE NormalizedName = ''");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_NormalizedName",
                table: "Categories",
                column: "NormalizedName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Categories_NormalizedName",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "NormalizedName",
                table: "Categories");
        }
    }
}
