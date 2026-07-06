using System.Globalization;
using Milk_manager.Models;
using Milk_manager.Services;

namespace Milk_manager.Views;

public partial class PaymentsPage : ContentPage
{
    public PaymentsPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        ClientPicker.ItemsSource = await DatabaseService.Instance.GetAllClientsAsync();
    }

    private async void OnCreatePaymentClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Выплаты", "Создание выплаты", "OK");

        if (ClientPicker.SelectedItem is not Client client ||
            !decimal.TryParse(AmountEntry.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var amount))
        {
            await DisplayAlert("Ошибка", "Выберите клиента и укажите корректную сумму", "OK");
            return;
        }

        await DatabaseService.Instance.AddPaymentAsync(new ClientPayment
        {
            ClientId = client.Id,
            Date = DateTime.Now,
            Amount = amount,
            Comment = CommentEntry.Text?.Trim()
        });

        AmountEntry.Text = string.Empty;
        CommentEntry.Text = string.Empty;
        await DisplayAlert("Готово", $"Выплата для {client.FullName} создана", "OK");
    }
}
