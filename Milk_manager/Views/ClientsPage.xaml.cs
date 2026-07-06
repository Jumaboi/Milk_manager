namespace Milk_manager.Views;

public partial class ClientsPage : ContentPage
{
	public ClientsPage()
	{
		InitializeComponent();
		this.BindingContext = new ViewModels.ClientsViewModel();
	}
}