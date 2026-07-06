namespace Milk_manager.Views;

public partial class PurchasePage : ContentPage
{
	public PurchasePage()
	{
		InitializeComponent();
		// Устанавливаем BindingContext с использованием singleton DatabaseService
		this.BindingContext = new ViewModels.PurchaseViewModel(Services.DatabaseService.Instance);
	}
}