namespace Milk_manager.Views;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
    }

    // Переход на экран "Клиенты и Долги"
    private async void OnClientsButtonClicked(object sender, EventArgs e)
    {
        await NotifyNavigationAsync("Клиенты");
        await Shell.Current.GoToAsync(nameof(ClientsPage));
    }

    // Переход на экран "Заводы"
    private async void OnFactoriesButtonClicked(object sender, EventArgs e)
    {
        await NotifyNavigationAsync("Заводы");
        await Shell.Current.GoToAsync(nameof(FactoriesPage));
    }

    // Переход на экран "Прием молока" (Ввод литров от людей)
    private async void OnPurchaseButtonClicked(object sender, EventArgs e)
    {
        await NotifyNavigationAsync("Прием молока");
        await Shell.Current.GoToAsync(nameof(PurchasePage));
    }

    // Переход на экран "Сдача молока на завод"
    private async void OnDeliveryButtonClicked(object sender, EventArgs e)
    {
        await NotifyNavigationAsync("Сдача на завод");
        await Shell.Current.GoToAsync(nameof(DeliveryPage));
    }

    // Переход на экран "Выплаты/Касса"
    private async void OnPaymentsButtonClicked(object sender, EventArgs e)
    {
        await NotifyNavigationAsync("Выплаты");
        await Shell.Current.GoToAsync(nameof(PaymentsPage));
    }

    // Переход на экран "Списание/Порча молока"
    private async void OnWriteOffButtonClicked(object sender, EventArgs e)
    {
        await NotifyNavigationAsync("Списание");
        await Shell.Current.GoToAsync(nameof(WriteOffPage));
    }

    // Переход на экран "Отчеты и экспорт в Excel"
    private async void OnReportsButtonClicked(object sender, EventArgs e)
    {
        await NotifyNavigationAsync("Отчеты");
        await Shell.Current.GoToAsync(nameof(ReportsPage));
    }

    private static Task NotifyNavigationAsync(string pageName)
    {
        return Shell.Current.DisplayAlertAsync("Переход", $"Открывается раздел: {pageName}", "OK");
    }
}
