using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Milk_manager.Models;
using Milk_manager.Services;

namespace Milk_manager.ViewModels;

public class ClientsViewModel : INotifyPropertyChanged
{
    public ObservableCollection<Client> Clients { get; } = new();

    public ICommand LoadCommand { get; }
    public ICommand AddCommand { get; }

    public ClientsViewModel()
    {
        LoadCommand = new Command(async () => await LoadAsync());
        AddCommand = new Command(async () => await AddSampleClientAsync());
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

    private async Task AddSampleClientAsync()
    {
        var client = new Client { FullName = "Новый клиент", VillageId = 0, DefaultPrice = 4.50m };
        await DatabaseService.Instance.AddClientAsync(client);
        await LoadAsync();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
