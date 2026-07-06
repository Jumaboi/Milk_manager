namespace Milk_manager.Models;

public record PurchaseEntryRow(string ClientName, string Phone, double Liters, decimal PricePerLiter)
{
    public decimal TotalSum => (decimal)Liters * PricePerLiter;
    public string PhoneDisplay => string.IsNullOrWhiteSpace(Phone) ? "Без телефона" : Phone;
}

public record PaymentEntryRow(string ClientName, string Phone, decimal Amount, string Comment, double Liters, decimal PricePerLiter, decimal MilkTotal)
{
    public string PhoneDisplay => string.IsNullOrWhiteSpace(Phone) ? "Без телефона" : Phone;
    public string CommentDisplay => string.IsNullOrWhiteSpace(Comment) ? "Без комментария" : Comment;
}

public record DeliveryEntryRow(string FactoryName, double Liters, decimal PricePerLiter)
{
    public decimal TotalSum => (decimal)Liters * PricePerLiter;
}

public record WriteOffEntryRow(DateTime Date, double Liters, string Reason)
{
    public string ReasonDisplay => string.IsNullOrWhiteSpace(Reason) ? "Без причины" : Reason;
}
