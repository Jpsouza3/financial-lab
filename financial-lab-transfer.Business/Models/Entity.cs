namespace financial_lab_transfer.Business.Models;

public class Entity
{
    public Guid Id { get; protected init; }

    public Entity()
    {
        Id = Guid.NewGuid();
    }
}
