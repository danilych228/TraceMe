using System.Collections.ObjectModel;
using System.IO;
using System.Globalization;

namespace TraceMe;

public partial class ActivityPage : ContentPage
{
    public ObservableCollection<ActivityItem> ActivityItems { get; set; }
    public ActivityPage()
    {
        InitializeComponent();
        ActivityItems = new ObservableCollection<ActivityItem>();
        ActivityListView.ItemsSource = ActivityItems;
        Shell.SetBackgroundColor(this, Colors.Black);
        Shell.SetForegroundColor(this, Colors.White);
        Shell.SetTitleColor(this, Colors.White);
    }
    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadSensorData();
    }
    private void LoadSensorData()
    {
        try
        {
            ActivityItems.Clear();
            string filePath = Path.Combine(FileSystem.AppDataDirectory, "data.txt");
            if (!File.Exists(filePath))
            {
                ActivityItems.Add(new ActivityItem { Title = "Нет данных", Value = "Файл не найден" });
                return;
            }
            string[] lines = File.ReadAllLines(filePath);
            if (lines.Length == 0)
            {
                ActivityItems.Add(new ActivityItem { Title = "Нет данных", Value = "Файл пуст" });
                return;
            }
            string lastLine = lines[lines.Length - 1].Trim();
            if (string.IsNullOrEmpty(lastLine))
            {
                ActivityItems.Add(new ActivityItem { Title = "Нет данных", Value = "Последняя строка пуста" });
                return;
            }
            string[] pairs = lastLine.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (string pair in pairs)
            {
                int separatorIndex = pair.IndexOf(':');
                if (separatorIndex <= 0 || separatorIndex == pair.Length - 1) continue;
                string key = pair.Substring(0, separatorIndex).Trim();
                string valueStr = pair.Substring(separatorIndex + 1).Trim();
                string formattedValue = key switch
                {
                    "ax" or "ay" or "az" =>
                        double.TryParse(valueStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double acc)
                            ? $"{acc:F4} g" : valueStr,
                    "gx" or "gy" or "gz" =>
                        double.TryParse(valueStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double gyro)
                            ? $"{gyro:F2} °/с" : valueStr,
                    "red" =>
                        ulong.TryParse(valueStr, out ulong red)
                            ? red.ToString() : valueStr,
                    _ => valueStr
                };
                string title = key switch
                {
                    "ax" => "Ускорение X",
                    "ay" => "Ускорение Y",
                    "az" => "Ускорение Z",
                    "gx" => "Гироскоп X",
                    "gy" => "Гироскоп Y",
                    "gz" => "Гироскоп Z",
                    "red" => "RED (пульсоксиметр)",
                    _ => key
                };
                ActivityItems.Add(new ActivityItem
                {
                    Title = title,
                    Value = formattedValue
                });
            }
            if (ActivityItems.Count == 0)
            {
                ActivityItems.Add(new ActivityItem { Title = "Ошибка", Value = "Неверный формат данных" });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Ошибка загрузки: {ex.Message}");
            ActivityItems.Clear();
            ActivityItems.Add(new ActivityItem { Title = "Ошибка", Value = ex.Message });
        }
    }
    private async void OnItemTapped(object sender, ItemTappedEventArgs e)
    {
        if (e.Item is ActivityItem item)
        {
            await DisplayAlert("Данные", $"{item.Title}: {item.Value}", "OK");
            ActivityListView.SelectedItem = null;
        }
    }
    private async void OnMenuClicked(object sender, EventArgs e) => await Shell.Current.GoToAsync("//MenuPage");
    private async void OnHomeClicked(object sender, EventArgs e) => await Shell.Current.GoToAsync("//MainPage");
    private async void OnChartClicked(object sender, EventArgs e) => await Shell.Current.GoToAsync("//ActivityPage");
    private async void OnSettingsClicked(object sender, EventArgs e) => await Shell.Current.GoToAsync("//SettingsPage");
}
public class ActivityItem
{
    public string Title { get; set; }
    public string Value { get; set; }
}