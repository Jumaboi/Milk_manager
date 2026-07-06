using System.Globalization;
using Milk_manager.Models;
using Milk_manager.Services;

namespace Milk_manager.Views;

public partial class WriteOffPage : ContentPage
{
    public WriteOffPage()
    {
        InitializeComponent();
    }

    private async void OnCreateWriteOffClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Списание", "Создание списания молока", "OK");

        if (!double.TryParse(LitersEntry.Text, NumberStyles.Float, CultureInfo.CurrentCulture, out var liters))
        {
            await DisplayAlert("Ошибка", "Укажите корректное количество литров", "OK");
            return;
        }

        await DatabaseService.Instance.AddWriteOffAsync(new MilkWriteOff
        {
            Date = DateTime.Now,
            Liters = liters,
            Reason = ReasonEntry.Text?.Trim()
        });

        LitersEntry.Text = string.Empty;
        ReasonEntry.Text = string.Empty;
        await DisplayAlert("Готово", "Списание создано", "OK");
    }
}
