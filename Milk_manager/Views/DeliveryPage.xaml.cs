using System.Globalization;
using Milk_manager.Models;
using Milk_manager.Services;

namespace Milk_manager.Views;

public partial class DeliveryPage : ContentPage
{
    public DeliveryPage()
    {
        InitializeComponent();
    }

    private async void OnCreateDeliveryClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Сдача", "Создание сдачи на завод", "OK");

        if (string.IsNullOrWhiteSpace(FactoryNameEntry.Text) ||
            !double.TryParse(LitersEntry.Text, NumberStyles.Float, CultureInfo.CurrentCulture, out var liters) ||
            !decimal.TryParse(PriceEntry.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var price))
        {
            await DisplayAlert("Ошибка", "Заполните завод, литры и цену корректно", "OK");
            return;
        }

        await DatabaseService.Instance.AddDeliveryAsync(new FactoryDelivery
        {
            FactoryName = FactoryNameEntry.Text.Trim(),
            Date = DateTime.Now,
            Liters = liters,
            PricePerLiter = price
        });

        FactoryNameEntry.Text = string.Empty;
        LitersEntry.Text = string.Empty;
        PriceEntry.Text = string.Empty;
        await DisplayAlert("Готово", "Сдача на завод создана", "OK");
    }
}
