using Milk_manager.Models;
using Milk_manager.Services;

namespace Milk_manager.Views;

public partial class FactoriesPage : ContentPage
{
    public FactoriesPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadFactoriesAsync();
    }

    private async void OnAddFactoryClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Заводы", "Создание завода", "OK");
        var name = await DisplayPromptAsync("Новый завод", "Название завода:");
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        var phone = await DisplayPromptAsync("Новый завод", "Телефон завода:", initialValue: "+7");
        await DatabaseService.Instance.AddFactoryAsync(new Factory { Name = name.Trim(), Phone = phone?.Trim() ?? string.Empty });
        await LoadFactoriesAsync();
        await DisplayAlert("Готово", "Завод создан", "OK");
    }

    private async void OnEditFactoryClicked(object sender, EventArgs e)
    {
        if (!TryGetSelectedFactory(out var factory))
        {
            await DisplayAlert("Заводы", "Выберите завод для изменения", "OK");
            return;
        }

        await DisplayAlert("Заводы", "Изменение завода", "OK");
        var name = await DisplayPromptAsync("Изменить завод", "Название завода:", initialValue: factory.Name);
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        var phone = await DisplayPromptAsync("Изменить завод", "Телефон завода:", initialValue: factory.Phone);
        factory.Name = name.Trim();
        factory.Phone = phone?.Trim() ?? string.Empty;
        await DatabaseService.Instance.UpdateFactoryAsync(factory);
        await LoadFactoriesAsync();
        await DisplayAlert("Готово", "Завод изменён", "OK");
    }

    private async void OnDeleteFactoryClicked(object sender, EventArgs e)
    {
        if (!TryGetSelectedFactory(out var factory))
        {
            await DisplayAlert("Заводы", "Выберите завод для удаления", "OK");
            return;
        }

        var confirm = await DisplayAlert("Подтверждение удаления", $"Удалить завод {factory.Name}?", "Удалить", "Отмена");
        if (!confirm)
        {
            await DisplayAlert("Отменено", "Удаление завода отменено", "OK");
            return;
        }

        await DatabaseService.Instance.DeleteFactoryAsync(factory.Id);
        await LoadFactoriesAsync();
        await DisplayAlert("Готово", "Завод удалён", "OK");
    }

    private async void OnRefreshFactoriesClicked(object sender, EventArgs e)
    {
        await LoadFactoriesAsync();
        await DisplayAlert("Заводы", "Список обновлён", "OK");
    }

    private async Task LoadFactoriesAsync()
    {
        FactoriesCollection.ItemsSource = await DatabaseService.Instance.GetFactoriesAsync();
    }

    private bool TryGetSelectedFactory(out Factory factory)
    {
        if (FactoriesCollection.SelectedItem is Factory selectedFactory)
        {
            factory = selectedFactory;
            return true;
        }

        factory = null!;
        return false;
    }
}
