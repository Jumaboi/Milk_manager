using System.Collections.ObjectModel;
using System.Globalization;
using Milk_manager.Models;
using Milk_manager.Services;

namespace Milk_manager.Views;

public partial class DeliveryPage : ContentPage
{
    private readonly ObservableCollection<DeliveryEntryRow> _currentDeliveries = new();
    public DeliveryPage()
    {
        InitializeComponent();
        DeliveriesCollection.ItemsSource = _currentDeliveries;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadFactoriesAsync();
        await LoadCurrentDeliveriesAsync();
    }

    private async void OnCreateDeliveryClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(FactoryNameEntry.Text) ||
            !double.TryParse(LitersEntry.Text, NumberStyles.Float, CultureInfo.CurrentCulture, out var liters) ||
            !decimal.TryParse(PriceEntry.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var price))
        {
            await DisplayAlertAsync("Ошибка", "Заполните завод, литры и цену корректно", "OK");
            return;
        }

        var factoryName = FactoryNameEntry.Text.Trim();
        await EnsureFactoryExistsAsync(factoryName);
        await DatabaseService.Instance.AddDeliveryAsync(new FactoryDelivery
        {
            FactoryName = factoryName,
            Date = DateTime.Now,
            Liters = liters,
            PricePerLiter = price
        });

        LitersEntry.Text = string.Empty;
        PriceEntry.Text = string.Empty;
        UpdateTotalLabel();
        await LoadFactoriesAsync();
        await LoadCurrentDeliveriesAsync();
        await DisplayAlertAsync("Готово", $"Сдача на {factoryName}: {liters:F1} л., сумма {((decimal)liters * price):F2}", "OK");
    }

    private async Task LoadCurrentDeliveriesAsync()
    {
        _currentDeliveries.Clear();
        var rows = await DatabaseService.Instance.GetDeliveryEntriesForDayAsync(DateTime.Today);
        foreach (var row in rows)
        {
            _currentDeliveries.Add(row);
        }
    }

    private async Task LoadFactoriesAsync()
    {
        FactoriesCollection.ItemsSource = await DatabaseService.Instance.GetFactoriesAsync();
    }

    private async Task EnsureFactoryExistsAsync(string factoryName)
    {
        var factories = await DatabaseService.Instance.GetFactoriesAsync();
        if (factories.Any(factory => string.Equals(factory.Name, factoryName, StringComparison.CurrentCultureIgnoreCase)))
        {
            return;
        }

        await DatabaseService.Instance.AddFactoryAsync(new Factory { Name = factoryName });
    }

    private void OnFactorySelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Factory factory)
        {
            FactoryNameEntry.Text = factory.Name;
        }
    }

    private void OnAmountTextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateTotalLabel();
    }

    private void UpdateTotalLabel()
    {
        if (double.TryParse(LitersEntry.Text, NumberStyles.Float, CultureInfo.CurrentCulture, out var liters) &&
            decimal.TryParse(PriceEntry.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var price))
        {
            TotalLabel.Text = $"Общая сумма: {((decimal)liters * price):F2}";
            return;
        }

        TotalLabel.Text = "Общая сумма: 0,00";
    }
}
