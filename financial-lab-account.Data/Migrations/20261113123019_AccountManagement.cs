using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace financial_lab_account.Data.Migrations
{
    /// <inheritdoc />
    public partial class AccountManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_accounts_Document",
                table: "accounts");

            migrationBuilder.AddColumn<DateTime>(
                name: "OpenedAt",
                table: "accounts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2026, 10, 7, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.AddColumn<DateTime>(
                name: "StatusChangedAt",
                table: "accounts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StatusReason",
                table: "accounts",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "balance_holds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reference = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReleasedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_balance_holds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_balance_holds_accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_accounts_Document",
                table: "accounts",
                column: "Document",
                unique: true,
                filter: "\"Status\" <> 'Closed'");

            migrationBuilder.CreateIndex(
                name: "IX_balance_holds_AccountId_Reference",
                table: "balance_holds",
                columns: new[] { "AccountId", "Reference" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_balance_holds_Status_ExpiresAt",
                table: "balance_holds",
                columns: new[] { "Status", "ExpiresAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "balance_holds");

            migrationBuilder.DropIndex(
                name: "IX_accounts_Document",
                table: "accounts");

            migrationBuilder.DropColumn(
                name: "OpenedAt",
                table: "accounts");

            migrationBuilder.DropColumn(
                name: "StatusChangedAt",
                table: "accounts");

            migrationBuilder.DropColumn(
                name: "StatusReason",
                table: "accounts");

            migrationBuilder.CreateIndex(
                name: "IX_accounts_Document",
                table: "accounts",
                column: "Document");
        }
    }
}
