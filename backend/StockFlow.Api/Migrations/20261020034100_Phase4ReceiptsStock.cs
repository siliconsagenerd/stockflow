using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockFlow.Api.Migrations
{
    /// <inheritdoc />
    public partial class Phase4ReceiptsStock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GoodsReceipts_IdempotencyKey",
                table: "GoodsReceipts");

            migrationBuilder.Sql("DELETE FROM \"StockMovements\";");
            migrationBuilder.AddColumn<Guid>(
                name: "WorkspaceId",
                table: "StockMovements",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "WorkspaceId",
                table: "GoodsReceipts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "StockBalances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockBalances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockBalances_DemoWorkspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "DemoWorkspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockBalances_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockBalances_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_WorkspaceId",
                table: "StockMovements",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceipts_WorkspaceId_IdempotencyKey",
                table: "GoodsReceipts",
                columns: new[] { "WorkspaceId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockBalances_ProductId",
                table: "StockBalances",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_StockBalances_WarehouseId",
                table: "StockBalances",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_StockBalances_WorkspaceId_ProductId_WarehouseId",
                table: "StockBalances",
                columns: new[] { "WorkspaceId", "ProductId", "WarehouseId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_GoodsReceipts_DemoWorkspaces_WorkspaceId",
                table: "GoodsReceipts",
                column: "WorkspaceId",
                principalTable: "DemoWorkspaces",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_DemoWorkspaces_WorkspaceId",
                table: "StockMovements",
                column: "WorkspaceId",
                principalTable: "DemoWorkspaces",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GoodsReceipts_DemoWorkspaces_WorkspaceId",
                table: "GoodsReceipts");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_DemoWorkspaces_WorkspaceId",
                table: "StockMovements");

            migrationBuilder.DropTable(
                name: "StockBalances");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_WorkspaceId",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_GoodsReceipts_WorkspaceId_IdempotencyKey",
                table: "GoodsReceipts");

            migrationBuilder.DropColumn(
                name: "WorkspaceId",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "WorkspaceId",
                table: "GoodsReceipts");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceipts_IdempotencyKey",
                table: "GoodsReceipts",
                column: "IdempotencyKey",
                unique: true);
        }
    }
}
