using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Milk_manager.Models;
using Milk_manager.Services;

namespace Milk_manager.ViewModels;

public class PurchaseViewModel : INotifyPropertyChanged
{
    private readonly DatabaseService _dbService;
    private Client? _selectedClient;
    private Village? _selectedVillage;
    private string _liters = string.Empty;
    private string _price = string.Empty;

    // Списки, которые динамически обновляются на экране телефона
    public ObservableCollection<Village> Villages { get; } = new();
    public ObservableCollection<Client> Clients { get; } = new();

    public Village? SelectedVillage
    {
        get => _selectedVillage;
        set
        {
            if (_selectedVillage == value)
            {
                return;
            }

            _selectedVillage = value;
            OnPropertyChanged();
            _ = LoadClientsAsync(); // Перезагрузить клиентов при смене поселка
        }
    }

    public Client? SelectedClient
    {
        get => _selectedClient;
        set
        {
            if (_selectedClient == value)
            {
                return;
            }

            _selectedClient = value;
            OnPropertyChanged();
            if (_selectedClient is not null)
            {
                // Подставляем персональную цену клиента по умолчанию
                Price = _selectedClient.DefaultPrice.ToString("F2", CultureInfo.CurrentCulture);
            }
        }
    }

    public string Liters
    {
        get => _liters;
        set { _liters = value; OnPropertyChanged(); }
    }

    public string Price
    {
        get => _price;
        set { _price = value; OnPropertyChanged(); }
    }

    public ICommand SavePurchaseCommand { get; }
    public ICommand AddClientCommand { get; }

    public PurchaseViewModel(DatabaseService dbService)
    {
        _dbService = dbService;
        SavePurchaseCommand = new Command(async () => await SavePurchaseAsync());
        AddClientCommand = new Command(async () => await AddClientAsync());
        _ = LoadVillagesAsync();
    }

    private async Task LoadVillagesAsync()
    {
        var list = await _dbService.GetVillagesAsync();
        Villages.Clear();
        foreach (var village in list)
        {
            Villages.Add(village);
        }
    }

    private async Task LoadClientsAsync()
    {
        Clients.Clear();
        if (SelectedVillage is null)
        {
            return;
        }

        var list = await _dbService.GetClientsByVillageAsync(SelectedVillage.Id);
        foreach (var client in list)
        {
            Clients.Add(client);
        }
    }

    private async Task AddClientAsync()
    {
        var fullName = await Shell.Current.DisplayPromptAsync("Новый клиент", "Введите ФИО клиента:");
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return;
        }

        var villageInitial = SelectedVillage?.Name ?? string.Empty;
        var villageName = await Shell.Current.DisplayPromptAsync(
            "Новый клиент",
            "Введите поселок. Если не указать, будет создан 'Без поселка':",
            initialValue: villageInitial);
        var phone = await Shell.Current.DisplayPromptAsync("Новый клиент", "Введите телефон клиента:", initialValue: "+7");
        var priceText = await Shell.Current.DisplayPromptAsync(
            "Новый клиент",
            "Введите цену за литр:",
            initialValue: string.IsNullOrWhiteSpace(Price) ? "4,50" : Price,
            keyboard: Keyboard.Numeric);

        if (!decimal.TryParse(priceText, NumberStyles.Number, CultureInfo.CurrentCulture, out var price))
        {
            price = 4.50m;
        }

        var village = await _dbService.GetOrCreateVillageAsync(villageName ?? string.Empty);
        var client = new Client
        {
            FullName = fullName.Trim(),
            VillageId = village.Id,
            Phone = phone?.Trim() ?? string.Empty,
            DefaultPrice = price
        };

        await _dbService.AddClientAsync(client);
        await LoadVillagesAsync();
        SelectedVillage = Villages.FirstOrDefault(item => item.Id == village.Id);
        await LoadClientsAsync();
        SelectedClient = Clients.FirstOrDefault(item => item.Id == client.Id);
        await Shell.Current.DisplayAlert("Готово", $"Клиент {client.FullName} добавлен и выбран", "OK");
    }

    private async Task SavePurchaseAsync()
    {
        // Проверка на пустые поля перед записью
        if (SelectedClient is null || string.IsNullOrWhiteSpace(Liters) || string.IsNullOrWhiteSpace(Price))
        {
            await Shell.Current.DisplayAlert("Внимание", "Пожалуйста, заполните все поля ввода!", "OK");
            return;
        }

        if (!double.TryParse(Liters, NumberStyles.Float, CultureInfo.CurrentCulture, out var litersValue) ||
            !decimal.TryParse(Price, NumberStyles.Number, CultureInfo.CurrentCulture, out var priceValue))
        {
            await Shell.Current.DisplayAlert("Ошибка", "Неверный формат чисел в литрах или цене!", "OK");
            return;
        }

        var purchase = new MilkPurchase
        {
            ClientId = SelectedClient.Id,
            Date = DateTime.Now, // Точное локальное время телефона
            Liters = litersValue,
            PricePerLiter = priceValue
        };

        // Сохраняем в локальную LiteDB
        await _dbService.AddPurchaseAsync(purchase);

        // Очищаем поле литров для следующего ввода
        Liters = string.Empty;

        await Shell.Current.DisplayAlert("Успешно", $"Принято {litersValue} л. от {SelectedClient.FullName}", "OK");
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
