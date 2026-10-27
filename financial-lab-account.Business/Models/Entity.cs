namespace financial_lab_account.Business.Models;

public class Entity
{
    public Guid Id { get; set; }

    public Entity()
    {
        Id = Guid.NewGuid();
    }
}