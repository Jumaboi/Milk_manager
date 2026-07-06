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
        await Shell.Current.GoToAsync(nameof(ClientsPage));
    }

    // Переход на экран "Заводы"
    private async void OnFactoriesButtonClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(FactoriesPage));
    }

    // Переход на экран "Прием молока" (Ввод литров от людей)
    private async void OnPurchaseButtonClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(PurchasePage));
    }

    // Переход на экран "Сдача молока на завод"
    private async void OnDeliveryButtonClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(DeliveryPage));
    }

    // Переход на экран "Выплаты/Касса"
    private async void OnPaymentsButtonClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(PaymentsPage));
    }

    // Переход на экран "Списание/Порча молока"
    private async void OnWriteOffButtonClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(WriteOffPage));
    }

    // Переход на экран "Отчеты и экспорт в Excel"
    private async void OnReportsButtonClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(ReportsPage));
    }
}