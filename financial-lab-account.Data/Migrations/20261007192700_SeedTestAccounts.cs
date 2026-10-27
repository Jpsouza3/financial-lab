using financial_lab_account.Data.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace financial_lab_account.Data.Migrations
{
    [DbContext(typeof(AccountDbContext))]
    [Migration("20261007192700_SeedTestAccounts")]
    public partial class SeedTestAccounts : Migration
    {
        private const decimal InitialBalance = 30.00m;
        private static readonly DateTime SeededAt = new(2026, 10, 7, 0, 0, 0, DateTimeKind.Utc);

        private static readonly (Guid AccountId, Guid EntryId, Guid CommandId, string Number, string Document)[] Accounts =
        [
            (new("11111111-1111-1111-1111-111111111111"), new("11111111-0000-0000-0000-000000000001"),
                new("11111111-0000-0000-0000-000000000002"), "00001", "00000000001"),
            (new("22222222-2222-2222-2222-222222222222"), new("22222222-0000-0000-0000-000000000001"),
                new("22222222-0000-0000-0000-000000000002"), "00002", "00000000002"),
            (new("33333333-3333-3333-3333-333333333333"), new("33333333-0000-0000-0000-000000000001"),
                new("33333333-0000-0000-0000-000000000002"), "00003", "00000000003")
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            for (var i = 0; i < Accounts.Length; i++)
            {
                var (accountId, entryId, commandId, number, document) = Accounts[i];
                var endToEndId = $"SEED{number}";

                migrationBuilder.InsertData(
                    table: "accounts",
                    columns: ["Id", "Name", "Document", "Branch", "Number", "Status", "Balance", "BlockedBalance", "Version"],
                    columnTypes: ["uuid", "character varying(150)", "character varying(14)", "character varying(4)",
                        "character varying(20)", "character varying(20)", "numeric(18,2)", "numeric(18,2)", "bigint"],
                    values: [accountId, $"Conta Teste {i + 1}", document, "0001", number, "Active", InitialBalance, 0m, 1L]);

                migrationBuilder.InsertData(
                    table: "ledger_entries",
                    columns: ["Id", "AccountId", "Type", "Amount", "BalanceAfter", "EndToEndId", "Description", "CreatedAt"],
                    columnTypes: ["uuid", "uuid", "character varying(10)", "numeric(18,2)", "numeric(18,2)",
                        "character varying(32)", "character varying(140)", "timestamp with time zone"],
                    values: [entryId, accountId, "Credit", InitialBalance, InitialBalance, endToEndId, "Saldo inicial (seed)", SeededAt]);

                migrationBuilder.InsertData(
                    table: "processed_commands",
                    columns: ["Id", "AccountId", "EndToEndId", "Type", "Amount", "Status", "LedgerEntryId", "RejectionReason", "CreatedAt"],
                    columnTypes: ["uuid", "uuid", "character varying(32)", "character varying(10)", "numeric(18,2)",
                        "character varying(10)", "uuid", "character varying(200)", "timestamp with time zone"],
                    values: [commandId, accountId, endToEndId, "Credit", InitialBalance, "Applied", entryId, null, SeededAt]);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var (accountId, entryId, commandId, _, _) in Accounts)
            {
                migrationBuilder.DeleteData("processed_commands", "Id", "uuid", commandId);
                migrationBuilder.DeleteData("ledger_entries", "Id", "uuid", entryId);
                migrationBuilder.DeleteData("accounts", "Id", "uuid", accountId);
            }
        }
    }
}
