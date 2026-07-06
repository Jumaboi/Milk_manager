using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.ApplicationModel.Communication;
using Milk_manager.Models;
using Milk_manager.Services;

namespace Milk_manager.ViewModels;

public class ClientsViewModel : INotifyPropertyChanged
{
    private Client? _selectedClient;

    public ObservableCollection<Client> Clients { get; } = new();

    public Client? SelectedClient
    {
        get => _selectedClient;
        set
        {
            _selectedClient = value;
            OnPropertyChanged();
        }
    }

    public ICommand LoadCommand { get; }
    public ICommand AddCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand CallCommand { get; }

    public ClientsViewModel()
    {
        LoadCommand = new Command(async () => await LoadAsync());
        AddCommand = new Command(async () => await AddClientAsync());
        EditCommand = new Command(async () => await EditClientAsync());
        DeleteCommand = new Command(async () => await DeleteClientAsync());
        CallCommand = new Command(async () => await CallClientAsync());
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        Clients.Clear();
        var list = await DatabaseService.Instance.GetAllClientsAsync();
        foreach (var client in list)
        {
            Clients.Add(client);
        }
    }

    private async Task AddClientAsync()
    {
        await Shell.Current.DisplayAlert("Клиенты", "Открыто создание клиента", "OK");

        var fullName = await Shell.Current.DisplayPromptAsync("Новый клиент", "Введите ФИО клиента:");
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return;
        }

        var phone = await Shell.Current.DisplayPromptAsync("Новый клиент", "Введите телефон клиента:", initialValue: "+7");
        var priceText = await Shell.Current.DisplayPromptAsync("Новый клиент", "Введите цену за литр:", initialValue: "4,50", keyboard: Keyboard.Numeric);
        if (!decimal.TryParse(priceText, NumberStyles.Number, CultureInfo.CurrentCulture, out var price))
        {
            price = 4.50m;
        }

        var client = new Client
        {
            FullName = fullName.Trim(),
            Phone = phone?.Trim() ?? string.Empty,
            VillageId = 0,
            DefaultPrice = price
        };

        await DatabaseService.Instance.AddClientAsync(client);
        await LoadAsync();
        await Shell.Current.DisplayAlert("Готово", $"Клиент {client.FullName} создан", "OK");
    }

    private async Task EditClientAsync()
    {
        if (!await RequireSelectedClientAsync("изменения"))
        {
            return;
        }

        await Shell.Current.DisplayAlert("Клиенты", "Открыто изменение клиента", "OK");

        var client = SelectedClient!;
        var fullName = await Shell.Current.DisplayPromptAsync("Изменить клиента", "ФИО:", initialValue: client.FullName);
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return;
        }

        var phone = await Shell.Current.DisplayPromptAsync("Изменить клиента", "Телефон:", initialValue: client.Phone);
        var priceText = await Shell.Current.DisplayPromptAsync(
            "Изменить клиента",
            "Цена за литр:",
            initialValue: client.DefaultPrice.ToString("F2", CultureInfo.CurrentCulture),
            keyboard: Keyboard.Numeric);

        if (!decimal.TryParse(priceText, NumberStyles.Number, CultureInfo.CurrentCulture, out var price))
        {
            await Shell.Current.DisplayAlert("Ошибка", "Цена указана неверно", "OK");
            return;
        }

        client.FullName = fullName.Trim();
        client.Phone = phone?.Trim() ?? string.Empty;
        client.DefaultPrice = price;

        await DatabaseService.Instance.UpdateClientAsync(client);
        await LoadAsync();
        SelectedClient = Clients.FirstOrDefault(item => item.Id == client.Id);
        await Shell.Current.DisplayAlert("Готово", "Клиент изменён", "OK");
    }

    private async Task DeleteClientAsync()
    {
        if (!await RequireSelectedClientAsync("удаления"))
        {
            return;
        }

        var client = SelectedClient!;
        var confirm = await Shell.Current.DisplayAlert(
            "Подтверждение удаления",
            $"Удалить клиента {client.FullName}? Связанные закупки и выплаты также будут удалены.",
            "Удалить",
            "Отмена");

        if (!confirm)
        {
            await Shell.Current.DisplayAlert("Отменено", "Удаление клиента отменено", "OK");
            return;
        }

        await DatabaseService.Instance.DeleteClientAsync(client.Id);
        SelectedClient = null;
        await LoadAsync();
        await Shell.Current.DisplayAlert("Готово", "Клиент удалён", "OK");
    }

    private async Task CallClientAsync()
    {
        if (!await RequireSelectedClientAsync("звонка"))
        {
            return;
        }

        var phone = SelectedClient!.Phone;
        if (string.IsNullOrWhiteSpace(phone))
        {
            await Shell.Current.DisplayAlert("Телефон", "У выбранного клиента не указан номер телефона", "OK");
            return;
        }

        var confirm = await Shell.Current.DisplayAlert("Позвонить", $"Набрать {phone}?", "Позвонить", "Отмена");
        if (!confirm)
        {
            return;
        }

        try
        {
            PhoneDialer.Default.Open(phone);
            await Shell.Current.DisplayAlert("Телефон", "Открыт набор номера", "OK");
        }
        catch (FeatureNotSupportedException)
        {
            await Shell.Current.DisplayAlert("Телефон", "Звонки не поддерживаются на этом устройстве", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Телефон", $"Не удалось открыть набор номера: {ex.Message}", "OK");
        }
    }

    private async Task<bool> RequireSelectedClientAsync(string actionName)
    {
        if (SelectedClient is not null)
        {
            return true;
        }

        await Shell.Current.DisplayAlert("Клиенты", $"Выберите клиента для {actionName}", "OK");
        return false;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
