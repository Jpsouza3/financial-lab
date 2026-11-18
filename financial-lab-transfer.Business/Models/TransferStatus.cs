namespace financial_lab_transfer.Business.Models;

public enum TransferStatus
{
    Screening,
    FraudRejected,
    Debiting,
    Crediting,
    Completed,
    Failed,
    Refunding,
    Refunded,
    RefundFailed
}
