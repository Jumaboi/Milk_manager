using System.Collections.ObjectModel;
using System.Globalization;
using Milk_manager.Models;
using Milk_manager.Services;

namespace Milk_manager.Views;

public partial class PaymentsPage : ContentPage
{
    private readonly ObservableCollection<PaymentEntryViewModel> _currentPayments = new();
    private Client? _selectedClient;

    public PaymentsPage()
    {
        InitializeComponent();
        PaymentsCollection.ItemsSource = _currentPayments;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        ClientsCollection.ItemsSource = await DatabaseService.Instance.GetAllClientsAsync();
    }

    private async void OnCreatePaymentClicked(object sender, EventArgs e)
    {
        if (_selectedClient is not Client client ||
            !decimal.TryParse(AmountEntry.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var amount))
        {
            await DisplayAlertAsync("Ошибка", "Выберите клиента и укажите корректную сумму", "OK");
            return;
        }

        var comment = CommentEntry.Text?.Trim() ?? string.Empty;
        await DatabaseService.Instance.AddPaymentAsync(new ClientPayment
        {
            ClientId = client.Id,
            Date = DateTime.Now,
            Amount = amount,
            Comment = comment
        });

        _currentPayments.Insert(0, new PaymentEntryViewModel(client.FullName, client.Phone, amount, comment));
        AmountEntry.Text = string.Empty;
        CommentEntry.Text = string.Empty;
        await DisplayAlertAsync("Готово", $"Выплата для {client.FullName}: {amount:F2}", "OK");
    }

    private void OnClientSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedClient = e.CurrentSelection.FirstOrDefault() as Client;
        SelectedClientLabel.Text = _selectedClient?.FullName ?? "Клиент не выбран";
        SelectedPhoneLabel.Text = $"Телефон: {(_selectedClient is null || string.IsNullOrWhiteSpace(_selectedClient.Phone) ? "Без телефона" : _selectedClient.Phone)}";
    }
}

public record PaymentEntryViewModel(string ClientName, string Phone, decimal Amount, string Comment)
{
    public string PhoneDisplay => string.IsNullOrWhiteSpace(Phone) ? "Без телефона" : Phone;

    public string CommentDisplay => string.IsNullOrWhiteSpace(Comment) ? "Без комментария" : Comment;
}
