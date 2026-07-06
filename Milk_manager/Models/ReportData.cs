namespace Milk_manager.Models;

public class ReportData
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public List<ClientBalanceReportRow> Clients { get; set; } = new();
    public List<FactoryBalanceReportRow> Factories { get; set; } = new();
    public List<DailyReportRow> Days { get; set; } = new();
    public ReportTotals Totals { get; set; } = new();
}
