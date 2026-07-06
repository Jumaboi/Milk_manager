using System.Globalization;
using Milk_manager.Models;
using Milk_manager.Services;

namespace Milk_manager.Views;

public partial class ReportsPage : ContentPage
{
    private readonly ExportService _exportService = new();
    private ReportData? _currentReport;

    public ReportsPage()
    {
        InitializeComponent();
        SetCurrentMonthRange();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadReportsAsync();
    }

    private async void OnRefreshClicked(object sender, EventArgs e)
    {
        await LoadReportsAsync();
    }

    private async void OnCurrentMonthClicked(object sender, EventArgs e)
    {
        SetCurrentMonthRange();
        await LoadReportsAsync();
    }

    private async void OnTodayClicked(object sender, EventArgs e)
    {
        FromDatePicker.Date = DateTime.Today;
        ToDatePicker.Date = DateTime.Today;
        await LoadReportsAsync();
    }

    private async void OnExportClicked(object sender, EventArgs e)
    {
        if (_currentReport is null)
        {
            await LoadReportsAsync();
        }

        if (_currentReport is null)
        {
            await DisplayAlertAsync("Экспорт", "Нет данных для экспорта", "OK");
            return;
        }

        var filePath = await _exportService.ExportReportCsvAsync(_currentReport);
        await DisplayAlertAsync("Экспорт готов", $"CSV отчет сохранен:\n{filePath}", "OK");
    }

    private async Task LoadReportsAsync()
    {
        var fromDate = FromDatePicker.Date.GetValueOrDefault(DateTime.Today);
        var toDate = ToDatePicker.Date.GetValueOrDefault(DateTime.Today);
        if (fromDate > toDate)
        {
            await DisplayAlertAsync("Период", "Дата начала не может быть позже даты окончания", "OK");
            return;
        }

        _currentReport = await DatabaseService.Instance.GetReportDataAsync(fromDate, toDate);
        DailyReportView.ItemsSource = _currentReport.Days;
        ClientReportView.ItemsSource = _currentReport.Clients;
        FactoryReportView.ItemsSource = _currentReport.Factories;
        UpdateTotals(_currentReport);
    }

    private void SetCurrentMonthRange()
    {
        var today = DateTime.Today;
        FromDatePicker.Date = new DateTime(today.Year, today.Month, 1);
        ToDatePicker.Date = new DateTime(today.Year, today.Month, 1).AddMonths(1).AddDays(-1);
    }

    private void UpdateTotals(ReportData report)
    {
        PeriodLabel.Text = $"Период: {report.FromDate:d} — {report.ToDate:d}";
        ClientTotalsLabel.Text = string.Format(
            CultureInfo.CurrentCulture,
            "Клиенты: {0:F1} л. / начислено {1:F2} / оплачено {2:F2} / долг {3:F2}",
            report.Totals.ClientLiters,
            report.Totals.ClientAmount,
            report.Totals.ClientPaid,
            report.Totals.ClientDebt);
        FactoryTotalsLabel.Text = string.Format(
            CultureInfo.CurrentCulture,
            "Заводы: {0:F1} л. / сумма {1:F2}",
            report.Totals.FactoryLiters,
            report.Totals.FactoryAmount);
    }
}
