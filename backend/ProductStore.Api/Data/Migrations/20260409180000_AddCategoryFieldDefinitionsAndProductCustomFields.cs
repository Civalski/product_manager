using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProductStore.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryFieldDefinitionsAndProductCustomFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CategoryFieldDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CategoryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    NormalizedName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoryFieldDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CategoryFieldDefinitions_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CategoryFieldDefinitions_CategoryId_NormalizedName",
                table: "CategoryFieldDefinitions",
                columns: new[] { "CategoryId", "NormalizedName" },
                unique: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomFieldValuesJson",
                table: "Products",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CategoryFieldDefinitions");

            migrationBuilder.DropColumn(
                name: "CustomFieldValuesJson",
                table: "Products");
        }
    }
}
