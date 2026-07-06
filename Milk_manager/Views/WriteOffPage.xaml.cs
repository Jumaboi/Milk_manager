using System.Collections.ObjectModel;
using System.Globalization;
using Milk_manager.Models;
using Milk_manager.Services;

namespace Milk_manager.Views;

public partial class WriteOffPage : ContentPage
{
    private readonly ObservableCollection<WriteOffEntryRow> _writeOffs = new();

    public WriteOffPage()
    {
        InitializeComponent();
        WriteOffsCollection.ItemsSource = _writeOffs;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadWriteOffsAsync();
    }

    private async void OnCreateWriteOffClicked(object sender, EventArgs e)
    {
        if (!double.TryParse(LitersEntry.Text, NumberStyles.Float, CultureInfo.CurrentCulture, out var liters))
        {
            await DisplayAlertAsync("Ошибка", "Укажите корректное количество литров", "OK");
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
        await LoadWriteOffsAsync();
        await DisplayAlertAsync("Готово", "Списание создано", "OK");
    }

    private async Task LoadWriteOffsAsync()
    {
        _writeOffs.Clear();
        var rows = await DatabaseService.Instance.GetWriteOffEntriesAsync();
        foreach (var row in rows)
        {
            _writeOffs.Add(row);
        }
    }
}
