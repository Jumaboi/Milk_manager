using System.Globalization;
using System.Text;
using Milk_manager.Models;

namespace Milk_manager.Services;

public class ExportService
{
    public async Task<string> ExportReportCsvAsync(ReportData reportData)
    {
        var fileName = $"milk_report_{reportData.FromDate:yyyyMMdd}_{reportData.ToDate:yyyyMMdd}.csv";
        var filePath = Path.Combine(FileSystem.AppDataDirectory, fileName);
        var csv = new StringBuilder();

        AppendLine(csv, "Период", reportData.FromDate.ToString("d", CultureInfo.CurrentCulture), reportData.ToDate.ToString("d", CultureInfo.CurrentCulture));
        csv.AppendLine();

        AppendLine(csv, "Итоги");
        AppendLine(csv, "Принято у клиентов, л", FormatDouble(reportData.Totals.ClientLiters));
        AppendLine(csv, "Начислено клиентам", FormatDecimal(reportData.Totals.ClientAmount));
        AppendLine(csv, "Оплачено клиентам", FormatDecimal(reportData.Totals.ClientPaid));
        AppendLine(csv, "Долг клиентам", FormatDecimal(reportData.Totals.ClientDebt));
        AppendLine(csv, "Сдано на заводы, л", FormatDouble(reportData.Totals.FactoryLiters));
        AppendLine(csv, "Сумма по заводам", FormatDecimal(reportData.Totals.FactoryAmount));
        csv.AppendLine();

        AppendLine(csv, "По дням");
        AppendLine(csv, "Дата", "Принято у клиентов, л", "Начислено клиентам", "Сдано на заводы, л", "Сумма по заводам");
        foreach (var day in reportData.Days)
        {
            AppendLine(
                csv,
                day.Date.ToString("d", CultureInfo.CurrentCulture),
                FormatDouble(day.ClientLiters),
                FormatDecimal(day.ClientAmount),
                FormatDouble(day.FactoryLiters),
                FormatDecimal(day.FactoryAmount));
        }
        csv.AppendLine();

        AppendLine(csv, "Клиенты");
        AppendLine(csv, "Поселок", "Клиент", "Взял литров", "За сколько", "Оплатил", "Должен");
        foreach (var client in reportData.Clients)
        {
            AppendLine(
                csv,
                client.VillageName,
                client.ClientName,
                FormatDouble(client.LitersTaken),
                FormatDecimal(client.TotalAmount),
                FormatDecimal(client.PaidAmount),
                FormatDecimal(client.DebtAmount));
        }
        csv.AppendLine();

        AppendLine(csv, "Заводы");
        AppendLine(csv, "Завод", "Сдач", "Литры", "Сумма");
        foreach (var factory in reportData.Factories)
        {
            AppendLine(
                csv,
                factory.FactoryName,
                factory.DeliveriesCount.ToString(CultureInfo.CurrentCulture),
                FormatDouble(factory.LitersDelivered),
                FormatDecimal(factory.TotalAmount));
        }

        await File.WriteAllTextAsync(filePath, csv.ToString(), Encoding.UTF8);
        return filePath;
    }

    private static void AppendLine(StringBuilder builder, params string[] cells)
    {
        builder.AppendLine(string.Join(";", cells.Select(EscapeCell)));
    }

    private static string EscapeCell(string cell)
    {
        if (!cell.Contains(';') && !cell.Contains('"') && !cell.Contains('\n'))
        {
            return cell;
        }

        return $"\"{cell.Replace("\"", "\"\"")}\"";
    }

    private static string FormatDouble(double value) => value.ToString("F1", CultureInfo.CurrentCulture);

    private static string FormatDecimal(decimal value) => value.ToString("F2", CultureInfo.CurrentCulture);
}
