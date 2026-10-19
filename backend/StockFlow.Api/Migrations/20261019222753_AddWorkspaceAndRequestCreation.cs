using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockFlow.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkspaceAndRequestCreation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Justification",
                table: "PurchaseRequests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WorkspaceId",
                table: "PurchaseRequests",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "DemoWorkspaces",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastAccessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemoWorkspaces", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequests_WorkspaceId",
                table: "PurchaseRequests",
                column: "WorkspaceId");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseRequests_DemoWorkspaces_WorkspaceId",
                table: "PurchaseRequests",
                column: "WorkspaceId",
                principalTable: "DemoWorkspaces",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseRequests_DemoWorkspaces_WorkspaceId",
                table: "PurchaseRequests");

            migrationBuilder.DropTable(
                name: "DemoWorkspaces");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseRequests_WorkspaceId",
                table: "PurchaseRequests");

            migrationBuilder.DropColumn(
                name: "Justification",
                table: "PurchaseRequests");

            migrationBuilder.DropColumn(
                name: "WorkspaceId",
                table: "PurchaseRequests");
        }
    }
}
