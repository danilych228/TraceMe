using System.IO;
using Microsoft.Maui.ApplicationModel;

namespace TraceMe;

public partial class SettingsPage : ContentPage
{
    private const string SETTINGS_FILE = "app_settings.txt";

    public SettingsPage()
    {
        InitializeComponent();
        Shell.SetBackgroundColor(this, Colors.Black);
        Shell.SetForegroundColor(this, Colors.White);
        Shell.SetTitleColor(this, Colors.White);

        LoadSettings();
        LoadAppVersion();
    }

    private void LoadAppVersion()
    {
        try
        {
            var version = AppInfo.VersionString;
            VersionLabel.Text = $"������: {version}";
        }
        catch { /* ignore */ }
    }

    private void LoadSettings()
    {
        // ������: ����� ��������� � Preferences ��� ����
        DarkThemeSwitch.IsToggled = Preferences.Get("DarkTheme", true);
        AutoSaveSwitch.IsToggled = Preferences.Get("AutoSave", true);
        UpdateIntervalEntry.Text = Preferences.Get("UpdateInterval", "200");
    }

    private void SaveSettings()
    {
        Preferences.Set("DarkTheme", DarkThemeSwitch.IsToggled);
        Preferences.Set("AutoSave", AutoSaveSwitch.IsToggled);
        Preferences.Set("UpdateInterval", UpdateIntervalEntry.Text);
    }

    private async void OnThemeToggled(object sender, ToggledEventArgs e)
    {
        // ����� ��������� ���� �����������
        SaveSettings();
    }

    private async void OnClearLogsClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("�������������", "�������� ��� ���������� ������?", "��", "���");
        if (confirm)
        {
            try
            {
                string filePath = Path.Combine(FileSystem.AppDataDirectory, "data.txt");
                if (File.Exists(filePath))
                    File.Delete(filePath);
                await DisplayAlert("�����", "���� �������", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("������", ex.Message, "OK");
            }
        }
    }

    // ���������
    private async void OnMenuClicked(object sender, EventArgs e) => await Shell.Current.GoToAsync("//MenuPage");
    private async void OnHomeClicked(object sender, EventArgs e) => await Shell.Current.GoToAsync("//MainPage");
    private async void OnChartClicked(object sender, EventArgs e) => await Shell.Current.GoToAsync("//ActivityPage");
}