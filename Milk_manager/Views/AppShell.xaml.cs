using Milk_manager.Views;
namespace Milk_manager;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Регистрируем текстовые пути (маршруты) к каждому экрану приложения.
        // Теперь на них можно переходить по имени из любой точки кода.
        Routing.RegisterRoute(nameof(ClientsPage), typeof(ClientsPage));
        Routing.RegisterRoute(nameof(FactoriesPage), typeof(FactoriesPage));
        Routing.RegisterRoute(nameof(PurchasePage), typeof(PurchasePage));
        Routing.RegisterRoute(nameof(DeliveryPage), typeof(DeliveryPage));
        Routing.RegisterRoute(nameof(PaymentsPage), typeof(PaymentsPage));
        Routing.RegisterRoute(nameof(WriteOffPage), typeof(WriteOffPage));
        Routing.RegisterRoute(nameof(ReportsPage), typeof(ReportsPage));
    }
}