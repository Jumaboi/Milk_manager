namespace Milk_manager.Models;

public class Client
{
    public int Id { get; set; }
    public int VillageId { get; set; } // К какому поселку привязан
    public string FullName { get; set; } // ФИО сдатчика
    public string Phone { get; set; }
    public decimal DefaultPrice { get; set; } = 4.50m; // Индивидуальная цена по умолчанию
}   