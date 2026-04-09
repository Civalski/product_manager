using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProductStore.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCosmosRealSkuColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CosmosAvgPrice",
                table: "Products",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CosmosBrandName",
                table: "Products",
                type: "TEXT",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CosmosBrandPictureUrl",
                table: "Products",
                type: "TEXT",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CosmosCommercialDescription",
                table: "Products",
                type: "TEXT",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CosmosGpcCode",
                table: "Products",
                type: "TEXT",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CosmosGpcDescription",
                table: "Products",
                type: "TEXT",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "CosmosGrossWeightGrams",
                table: "Products",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CosmosGtin",
                table: "Products",
                type: "TEXT",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "CosmosHeight",
                table: "Products",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "CosmosLength",
                table: "Products",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CosmosMaxPrice",
                table: "Products",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CosmosMinPrice",
                table: "Products",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CosmosNcmCode",
                table: "Products",
                type: "TEXT",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CosmosNcmDescription",
                table: "Products",
                type: "TEXT",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "CosmosNetWeightGrams",
                table: "Products",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CosmosPriceLabel",
                table: "Products",
                type: "TEXT",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CosmosThumbnailUrl",
                table: "Products",
                type: "TEXT",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "CosmosWidth",
                table: "Products",
                type: "REAL",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CosmosAvgPrice",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CosmosBrandName",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CosmosBrandPictureUrl",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CosmosCommercialDescription",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CosmosGpcCode",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CosmosGpcDescription",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CosmosGrossWeightGrams",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CosmosGtin",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CosmosHeight",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CosmosLength",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CosmosMaxPrice",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CosmosMinPrice",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CosmosNcmCode",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CosmosNcmDescription",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CosmosNetWeightGrams",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CosmosPriceLabel",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CosmosThumbnailUrl",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CosmosWidth",
                table: "Products");
        }
    }
}
