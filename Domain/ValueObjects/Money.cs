namespace Domain.ValueObjects;

public record Money
{
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "RUB";

    public static Money operator +(Money a, Money b)
    {
        if (a.Currency != b.Currency)
            throw new InvalidOperationException("Cannot add money with different currencies");
        return new Money { Amount = a.Amount + b.Amount, Currency = a.Currency };
    }

    public static Money operator *(Money money, decimal multiplier)
        => new Money { Amount = money.Amount * multiplier, Currency = money.Currency };
}
