using financial_lab_account.Business.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace financial_lab_account.Data.Mappings;

public class AccountMapping : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("accounts");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.Name).HasMaxLength(150).IsRequired();
        builder.Property(a => a.Document).HasMaxLength(14).IsRequired();
        builder.Property(a => a.Branch).HasMaxLength(4).IsRequired();
        builder.Property(a => a.Number).HasMaxLength(20).IsRequired();

        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Property(a => a.Balance).HasPrecision(18, 2);
        builder.Property(a => a.BlockedBalance).HasPrecision(18, 2);
        builder.Ignore(a => a.AvailableBalance);

        builder.Property(a => a.Version).IsConcurrencyToken();

        builder.HasIndex(a => new { a.Branch, a.Number }).IsUnique();
        builder.HasIndex(a => a.Document);
    }
}
