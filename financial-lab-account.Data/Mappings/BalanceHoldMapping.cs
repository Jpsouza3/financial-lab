using financial_lab_account.Business.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace financial_lab_account.Data.Mappings;

public class BalanceHoldMapping : IEntityTypeConfiguration<BalanceHold>
{
    public void Configure(EntityTypeBuilder<BalanceHold> builder)
    {
        builder.ToTable("balance_holds");

        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).ValueGeneratedNever();

        builder.Property(h => h.Reference).HasMaxLength(BalanceHold.ReferenceMaxLength).IsRequired();
        builder.Property(h => h.Amount).HasPrecision(18, 2);
        builder.Property(h => h.Reason).HasMaxLength(BalanceHold.ReasonMaxLength).IsRequired();
        builder.Property(h => h.Status).HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(h => h.CreatedAt).IsRequired();

        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(h => h.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(h => new { h.AccountId, h.Reference }).IsUnique();
        builder.HasIndex(h => new { h.Status, h.ExpiresAt });
    }
}
