namespace Milk_manager.Models;

public class DailyReportRow
{
    public DateTime Date { get; set; }
    public double ClientLiters { get; set; }
    public decimal ClientAmount { get; set; }
    public double FactoryLiters { get; set; }
    public decimal FactoryAmount { get; set; }
}
