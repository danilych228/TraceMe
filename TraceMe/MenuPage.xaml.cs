namespace TraceMe;

public partial class MenuPage : ContentPage
{
	public MenuPage()
	{
		InitializeComponent();
        Shell.SetBackgroundColor(this, Colors.Black);
        Shell.SetForegroundColor(this, Colors.White);
        Shell.SetTitleColor(this, Colors.White);
    }

    private async void OnMenuClicked(object sender, EventArgs e) =>
    await Shell.Current.GoToAsync("//MenuPage");
    private async void OnHomeClicked(object sender, EventArgs e) =>
        await Shell.Current.GoToAsync("//MainPage");
    private async void OnChartClicked(object sender, EventArgs e) =>
        await Shell.Current.GoToAsync("//ActivityPage");
    private async void OnSettingsClicked(object sender, EventArgs e) =>
        await Shell.Current.GoToAsync("//SettingsPage");
}