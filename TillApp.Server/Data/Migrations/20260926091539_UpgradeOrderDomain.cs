using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TillApp.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpgradeOrderDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_IsPaid",
                table: "Orders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OrderItems_Price",
                table: "OrderItems");

            migrationBuilder.RenameColumn(
                name: "Price",
                table: "OrderItems",
                newName: "UnitPrice");

            migrationBuilder.RenameColumn(
                name: "ItemName",
                table: "OrderItems",
                newName: "ProductName");

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Orders",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<DateTime>(
                name: "PaidAt",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "Status",
                table: "Orders",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<int>(
                name: "ProductID",
                table: "OrderItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "OrderItems",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.Sql(
                "UPDATE [Orders] " +
                "SET [Status] = CASE WHEN [IsPaid] = CAST(1 AS bit) THEN CAST(1 AS tinyint) ELSE CAST(0 AS tinyint) END, " +
                "[PaidAt] = CASE WHEN [IsPaid] = CAST(1 AS bit) THEN [CreatedAt] ELSE NULL END;");

            migrationBuilder.DropColumn(
                name: "IsPaid",
                table: "Orders");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Status_CreatedAt",
                table: "Orders",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Orders_Status",
                table: "Orders",
                sql: "[Status] IN (0, 1, 2)");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_ProductID",
                table: "OrderItems",
                column: "ProductID");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OrderItems_Quantity",
                table: "OrderItems",
                sql: "[Quantity] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OrderItems_UnitPrice",
                table: "OrderItems",
                sql: "[UnitPrice] >= 0.0001 AND [UnitPrice] <= 922337203685477.5807");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_Products_ProductID",
                table: "OrderItems",
                column: "ProductID",
                principalTable: "Products",
                principalColumn: "ProductID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_Products_ProductID",
                table: "OrderItems");

            migrationBuilder.DropIndex(
                name: "IX_Orders_Status_CreatedAt",
                table: "Orders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Orders_Status",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_OrderItems_ProductID",
                table: "OrderItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OrderItems_Quantity",
                table: "OrderItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OrderItems_UnitPrice",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PaidAt",
                table: "Orders");

            migrationBuilder.AddColumn<bool>(
                name: "IsPaid",
                table: "Orders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(
                "UPDATE [Orders] SET [IsPaid] = CASE WHEN [Status] = CAST(1 AS tinyint) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END;");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ProductID",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "OrderItems");

            migrationBuilder.RenameColumn(
                name: "UnitPrice",
                table: "OrderItems",
                newName: "Price");

            migrationBuilder.RenameColumn(
                name: "ProductName",
                table: "OrderItems",
                newName: "ItemName");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_IsPaid",
                table: "Orders",
                column: "IsPaid");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OrderItems_Price",
                table: "OrderItems",
                sql: "[Price] >= 0.0001 AND [Price] <= 922337203685477.5807");
        }
    }
}
