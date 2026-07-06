namespace Milk_manager.Models;

public class MilkPurchase
{
    public int Id { get; set; }
    public int ClientId { get; set; } // Кто сдал
    public DateTime Date { get; set; } // Дата и время
    public double Liters { get; set; } // Литры
    public decimal PricePerLiter { get; set; } // Цена за литр в этот день

    // В LiteDB поле вычисляется при чтении, не нужно атрибутов
    public decimal TotalSum => (decimal)Liters * PricePerLiter;
}