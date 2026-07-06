namespace Milk_manager.Models;

public class FactoryBalanceReportRow
{
    public string FactoryName { get; set; } = string.Empty;
    public double LitersDelivered { get; set; }
    public decimal TotalAmount { get; set; }
    public int DeliveriesCount { get; set; }
}
