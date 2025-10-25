using Android.App;
using Android.Appwidget;
using Android.Content;

namespace TraceMe.Platforms.Android;

[BroadcastReceiver(Enabled = true, Exported = true)]
[IntentFilter(new[] { "WIDGET_UPDATE_ACTION" })]
public class WidgetUpdateReceiver : BroadcastReceiver
{
    public override void OnReceive(Context context, Intent intent)
    {
        if (intent?.Action == "WIDGET_UPDATE_ACTION")
        {
            int appWidgetId = intent.GetIntExtra(AppWidgetManager.ExtraAppwidgetId, -1);
            if (appWidgetId != -1)
            {
                var widget = new TraceMeWidget();
                var appWidgetManager = AppWidgetManager.GetInstance(context);
                widget.OnUpdate(context, appWidgetManager, new int[] { appWidgetId });
            }
        }
    }
}