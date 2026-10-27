using financial_lab_account.Business.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace financial_lab_account.Data.Mappings;

public class LedgerEntryMapping : IEntityTypeConfiguration<LedgerEntry>
{
    public void Configure(EntityTypeBuilder<LedgerEntry> builder)
    {
        builder.ToTable("ledger_entries");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.Type).HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(e => e.Amount).HasPrecision(18, 2);
        builder.Property(e => e.BalanceAfter).HasPrecision(18, 2);
        builder.Property(e => e.EndToEndId).HasMaxLength(LedgerEntry.EndToEndIdMaxLength).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(LedgerEntry.DescriptionMaxLength);
        builder.Property(e => e.CreatedAt).IsRequired();

        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(e => e.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.AccountId, e.EndToEndId }).IsUnique();

        builder.HasIndex(e => new { e.AccountId, e.CreatedAt });
    }
}
