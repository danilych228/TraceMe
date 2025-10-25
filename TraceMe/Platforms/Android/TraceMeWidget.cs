using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using System.IO;
using System.Linq;

namespace TraceMe.Platforms.Android;

[BroadcastReceiver(Label = "TraceMe", Exported =true)]
[IntentFilter(new[] { "android.appwidget.action.APPWIDGET_UPDATE" })]
[MetaData("android.appwidget.provider", Resource = "@xml/trace_widget_info")]
public class TraceMeWidget : AppWidgetProvider
{
    public override void OnUpdate(Context context, AppWidgetManager appWidgetManager, int[] appWidgetIds)
    {
        foreach (int appWidgetId in appWidgetIds)
        {
            UpdateWidget(context, appWidgetManager, appWidgetId);
        }
    }

    private void UpdateWidget(Context context, AppWidgetManager appWidgetManager, int appWidgetId)
    {
        var widgetView = new RemoteViews(context.PackageName, Resource.Layout.widget_layout);

        // Чтение данных из файла
        string sensorData = GetLatestSensorData(context);
        var (heartRate, acceleration) = ParseSensorData(sensorData);

        // Обновление текста
        widgetView.SetTextViewText(Resource.Id.widget_heart_rate, $"Пульс: {heartRate}");
        widgetView.SetTextViewText(Resource.Id.widget_acceleration, $"Ускорение: {acceleration}");

        // Настройка кнопки обновления
        var intent = new Intent(context, typeof(WidgetUpdateReceiver));
        intent.SetAction("WIDGET_UPDATE_ACTION");
        intent.PutExtra(AppWidgetManager.ExtraAppwidgetId, appWidgetId);
        var pendingIntent = PendingIntent.GetBroadcast(
            context,
            appWidgetId,
            intent,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable
        );
        widgetView.SetOnClickPendingIntent(Resource.Id.widget_update_btn, pendingIntent);

        appWidgetManager.UpdateAppWidget(appWidgetId, widgetView);
    }

    private string GetLatestSensorData(Context context)
    {
        try
        {
            string filePath = Path.Combine(context.FilesDir.AbsolutePath, "data.txt");
            if (!File.Exists(filePath)) return "Нет данных";

            string[] lines = File.ReadAllLines(filePath);
            return lines.Length > 0 ? lines.Last().Trim() : "Нет данных";
        }
        catch
        {
            return "Ошибка чтения";
        }
    }

    private (string heartRate, string acceleration) ParseSensorData(string data)
    {
        if (string.IsNullOrEmpty(data) || data == "Нет данных" || data == "Ошибка чтения")
        {
            return ("--", "--");
        }

        try
        {
            string[] pairs = data.Split(';', StringSplitOptions.RemoveEmptyEntries);
            var dataDict = pairs.ToDictionary(
                p => p.Split(':')[0].Trim(),
                p => p.Split(':').Length > 1 ? p.Split(':')[1].Trim() : "0"
            );

            string heartRate = dataDict.ContainsKey("red") ?
                $"RED: {dataDict["red"]}" : "--";

            string acceleration = (dataDict.ContainsKey("ax") && dataDict.ContainsKey("ay") && dataDict.ContainsKey("az")) ?
                $"{dataDict["ax"]}, {dataDict["ay"]}, {dataDict["az"]}" : "--";

            return (heartRate, acceleration);
        }
        catch
        {
            return ("--", "--");
        }
    }
}