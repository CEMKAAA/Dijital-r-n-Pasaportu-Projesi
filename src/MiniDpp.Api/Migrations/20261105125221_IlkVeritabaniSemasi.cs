using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniDpp.Api.Migrations
{
    /// <inheritdoc />
    public partial class IlkVeritabaniSemasi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductModels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductModels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductBatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BatchNumber = table.Column<string>(type: "TEXT", nullable: false),
                    ProductModelId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductBatches_ProductModels_ProductModelId",
                        column: x => x.ProductModelId,
                        principalTable: "ProductModels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Gtin = table.Column<string>(type: "TEXT", nullable: false),
                    CottonPercentage = table.Column<decimal>(type: "TEXT", nullable: false),
                    ElastanePercentage = table.Column<decimal>(type: "TEXT", nullable: false),
                    ProductionLocation = table.Column<string>(type: "TEXT", nullable: false),
                    CarbonKgCo2e = table.Column<decimal>(type: "TEXT", nullable: false),
                    ProductBatchId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Products_ProductBatches_ProductBatchId",
                        column: x => x.ProductBatchId,
                        principalTable: "ProductBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductBatches_ProductModelId_BatchNumber",
                table: "ProductBatches",
                columns: new[] { "ProductModelId", "BatchNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductModels_Name",
                table: "ProductModels",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_Gtin",
                table: "Products",
                column: "Gtin",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_ProductBatchId",
                table: "Products",
                column: "ProductBatchId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "ProductBatches");

            migrationBuilder.DropTable(
                name: "ProductModels");
        }
    }
}
