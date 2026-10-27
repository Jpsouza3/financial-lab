namespace financial_lab_transfer.Business.Models;

public enum TransferStatus
{
    Debiting,
    Crediting,
    Completed,
    Failed,
    Refunding,
    Refunded,
    RefundFailed
}
