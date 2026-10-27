using financial_lab_transfer.Business.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace financial_lab_transfer.Data.Mappings;

public class TransferMapping : IEntityTypeConfiguration<Transfer>
{
    public void Configure(EntityTypeBuilder<Transfer> builder)
    {
        builder.ToTable("transfers");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.SourceAccountId).IsRequired();
        builder.Property(t => t.DestinationAccountId).IsRequired();
        builder.Property(t => t.Amount).HasPrecision(18, 2);
        builder.Property(t => t.Description).HasMaxLength(Transfer.DescriptionMaxLength);
        builder.Property(t => t.EndToEndId).HasMaxLength(Transfer.EndToEndIdMaxLength).IsRequired();
        builder.Property(t => t.RefundEndToEndId).HasMaxLength(Transfer.EndToEndIdMaxLength);
        builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(t => t.FailureReason).HasMaxLength(Transfer.FailureReasonMaxLength);
        builder.Property(t => t.CreatedAt).IsRequired();
        builder.Property(t => t.UpdatedAt).IsRequired();

        builder.Property(t => t.Version).IsConcurrencyToken();

        builder.HasIndex(t => t.EndToEndId).IsUnique();
        builder.HasIndex(t => t.RefundEndToEndId).IsUnique();
        builder.HasIndex(t => new { t.SimulationId, t.Status });
    }
}
