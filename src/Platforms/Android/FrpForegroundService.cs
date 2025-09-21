using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;

namespace FrpAndroid.Services;

// Foreground service for running frp processes with event?driven status notification (without log tail).
[Service(Name = "com.qiuhaotc.frpandroid.FrpForegroundService", Exported = false, Enabled = true)]
public class FrpForegroundService : Service
{
    public const int NotificationId = 0x0F12;
    public const string ChannelId = "frp_run_channel";
    public const string ActionStart = "FRP_SERVICE_START";
    public const string ExtraStartFrpc = "start_frpc";
    public const string ExtraStartFrps = "start_frps";

    public static bool IsRunning { get; private set; } // 防重复

    FrpManager? _manager;
    bool _subscribed;
    bool _wantFrpc;
    bool _wantFrps;

    public override IBinder? OnBind(Intent? intent) => null;

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        if (IsRunning)
        {
            // 已运行直接刷新状态
            UpdateNotification();
            return StartCommandResult.Sticky;
        }
        IsRunning = true;

        _wantFrpc = intent?.GetBooleanExtra(ExtraStartFrpc, false) ?? false;
        _wantFrps = intent?.GetBooleanExtra(ExtraStartFrps, false) ?? false;

        CreateChannel();
        StartForeground(NotificationId, BuildNotification("初始化中..."));

        _manager = MauiApplication.Current.Services.GetService<FrpManager>();
        if (_manager != null && !_subscribed)
        {
            // 仅订阅状态事件（不再根据日志刷新）
            _manager.OnStateChanged += Manager_OnStateChanged;
            _subscribed = true;
        }

        Task.Run(async () =>
        {
            try
            {
                if (_manager != null)
                {
                    if (_wantFrpc && !_manager.IsRunning(FrpType.Frpc)) await _manager.StartFrpcAsync();
                    if (_wantFrps && !_manager.IsRunning(FrpType.Frps)) await _manager.StartFrpsAsync();
                }
                UpdateNotification("运行中");
            }
            catch (Exception ex)
            {
                UpdateNotification("启动异常: " + Trim(ex.Message, 28));
            }
        });

        return StartCommandResult.Sticky;
    }

    void Manager_OnStateChanged(object? sender, FrpStateEvent e)
    {
        // 仅在状态变化时刷新
        UpdateNotification();
    }

    void CreateChannel()
    {
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            var mgr = (NotificationManager?)GetSystemService(NotificationService);
            if (mgr?.GetNotificationChannel(ChannelId) == null)
            {
                var channel = new NotificationChannel(ChannelId, "FRP 运行", NotificationImportance.Low)
                { Description = "FRP 前台服务" };
                mgr?.CreateNotificationChannel(channel);
            }
        }
    }

    Notification BuildNotification(string? statusOverride = null)
    {
        var frpcState = _manager?.IsRunning(FrpType.Frpc) == true ? "on" : "off";
        var frpsState = _manager?.IsRunning(FrpType.Frps) == true ? "on" : "off";
        var state = statusOverride ?? $"frpc:{frpcState} frps:{frpsState}";

        var pendingIntent = PendingIntent.GetActivity(
            this,
            0,
            new Intent(this, typeof(MainActivity)).SetFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop),
            PendingIntentFlags.Immutable);

        return new NotificationCompat.Builder(this, ChannelId)
            .SetOngoing(true)
            .SetContentTitle("FRP 服务")
            .SetContentText(state)
            .SetSmallIcon(Android.Resource.Drawable.StatSysDataBluetooth)
            .SetStyle(new NotificationCompat.BigTextStyle().BigText(state))
            .SetContentIntent(pendingIntent)
            .Build();
    }

    static string Trim(string s, int max) => s.Length <= max ? s : s[..max];

    void UpdateNotification(string? statusOverride = null)
    {
        NotificationManagerCompat.From(this).Notify(NotificationId, BuildNotification(statusOverride));
    }

    public override void OnDestroy()
    {
        if (_manager != null && _subscribed)
        {
            _manager.OnStateChanged -= Manager_OnStateChanged;
            _subscribed = false;
        }
        IsRunning = false;
        base.OnDestroy();
    }
}
