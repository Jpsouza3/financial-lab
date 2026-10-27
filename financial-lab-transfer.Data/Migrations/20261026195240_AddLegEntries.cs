using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace financial_lab_transfer.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLegEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CreditEntryId",
                table: "transfers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreditedAt",
                table: "transfers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DebitEntryId",
                table: "transfers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DebitedAt",
                table: "transfers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RefundEntryId",
                table: "transfers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RefundedAt",
                table: "transfers",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreditEntryId",
                table: "transfers");

            migrationBuilder.DropColumn(
                name: "CreditedAt",
                table: "transfers");

            migrationBuilder.DropColumn(
                name: "DebitEntryId",
                table: "transfers");

            migrationBuilder.DropColumn(
                name: "DebitedAt",
                table: "transfers");

            migrationBuilder.DropColumn(
                name: "RefundEntryId",
                table: "transfers");

            migrationBuilder.DropColumn(
                name: "RefundedAt",
                table: "transfers");
        }
    }
}
