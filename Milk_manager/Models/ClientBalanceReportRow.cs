namespace Milk_manager.Models;

public class ClientBalanceReportRow
{
    public string ClientName { get; set; } = string.Empty;
    public string VillageName { get; set; } = string.Empty;
    public double LitersTaken { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal DebtAmount { get; set; }
}
