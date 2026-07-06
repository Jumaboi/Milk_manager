using Milk_manager.Services;

namespace Milk_manager.Views;

public partial class SettingsPage : ContentPage
{
    public SettingsPage()
    {
        InitializeComponent();
        DeveloperLogPathLabel.Text = DeveloperActionLogService.LogFilePath;
    }

    private async void OnShowDeveloperLogPathClicked(object sender, EventArgs e)
    {
        await DisplayAlertAsync("Лог разработчика", DeveloperActionLogService.LogFilePath, "OK");
    }
}
