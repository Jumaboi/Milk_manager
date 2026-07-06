using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Milk_manager.Models;
using Milk_manager.Services;

namespace Milk_manager.ViewModels;

public class PurchaseViewModel : INotifyPropertyChanged
{
    private readonly DatabaseService _dbService;

    // Списки, которые динамически обновляются на экране телефона
    public ObservableCollection<Village> Villages { get; set; } = new();
    public ObservableCollection<Client> Clients { get; set; } = new();

    private Village _selectedVillage;
    public Village SelectedVillage
    {
        get => _selectedVillage;
        set
        {
            _selectedVillage = value;
            OnPropertyChanged();
            LoadClientsAsync(); // Перезагрузить клиентов при смене поселка
        }
    }

    private Client _selectedClient;
    public Client SelectedClient
    {
        get => _selectedClient;
        set
        {
            _selectedClient = value;
            OnPropertyChanged();
            if (_selectedClient != null)
            {
                // Подставляем персональную цену клиента по умолчанию
                Price = _selectedClient.DefaultPrice.ToString("F2");
            }
        }
    }

    private string _liters;
    public string Liters
    {
        get => _liters;
        set { _liters = value; OnPropertyChanged(); }
    }

    private string _price;
    public string Price
    {
        get => _price;
        set { _price = value; OnPropertyChanged(); }
    }

    public ICommand SavePurchaseCommand { get; }

    public PurchaseViewModel(DatabaseService dbService)
    {
        _dbService = dbService;
        SavePurchaseCommand = new Command(async () => await SavePurchaseAsync());
        LoadVillagesAsync();
    }

    private async void LoadVillagesAsync()
    {
        var list = await _dbService.GetVillagesAsync();
        Villages.Clear();
        foreach (var v in list) Villages.Add(v);
    }

    private async void LoadClientsAsync()
    {
        Clients.Clear();
        if (SelectedVillage == null) return;

        var list = await _dbService.GetClientsByVillageAsync(SelectedVillage.Id);
        foreach (var c in list) Clients.Add(c);
    }

    private async Task SavePurchaseAsync()
    {
        // Проверка на пустые поля перед записью
        if (SelectedClient == null || string.IsNullOrWhiteSpace(Liters) || string.IsNullOrWhiteSpace(Price))
        {
            await Shell.Current.DisplayAlert("Внимание", "Пожалуйста, заполните все поля ввода!", "OK");
            return;
        }

        if (!double.TryParse(Liters, out double litersValue) || !decimal.TryParse(Price, out decimal priceValue))
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

        // Сохраняем в локальную SQLite
        await _dbService.AddPurchaseAsync(purchase);

        // Очищаем поле литров для следующего ввода
        Liters = string.Empty;

        await Shell.Current.DisplayAlert("Успешно", $"Принято {litersValue} л. от {SelectedClient.FullName}", "OK");
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}