using financial_lab_account.Business.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace financial_lab_account.Data.Mappings;

public class ProcessedCommandMapping : IEntityTypeConfiguration<ProcessedCommand>
{
    public void Configure(EntityTypeBuilder<ProcessedCommand> builder)
    {
        builder.ToTable("processed_commands");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.AccountId).IsRequired();
        builder.Property(c => c.EndToEndId).HasMaxLength(LedgerEntry.EndToEndIdMaxLength).IsRequired();
        builder.Property(c => c.Type).HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(c => c.Amount).HasPrecision(18, 2);
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(c => c.RejectionReason).HasMaxLength(200);
        builder.Property(c => c.CreatedAt).IsRequired();

        builder.HasOne<LedgerEntry>()
            .WithOne()
            .HasForeignKey<ProcessedCommand>(c => c.LedgerEntryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(o => new { o.AccountId, o.EndToEndId }).IsUnique();
    }
}
