using Android.App;
using Android.Runtime;
using Android.Content;
using FrpAndroid.Services;

namespace FrpAndroid
{
    [Application]
    public class MainApplication : MauiApplication
    {
        public MainApplication(IntPtr handle, JniHandleOwnership ownership)
            : base(handle, ownership)
        {
        }

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }

    [BroadcastReceiver(Enabled = true, Exported = true, Name = "com.qiuhaotc.frpandroid.BootReceiver")]
    [IntentFilter(new[] { Intent.ActionBootCompleted, Intent.ActionLockedBootCompleted })]
    public class BootReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context? context, Intent? intent)
        {
            if (context == null) return;
            if (intent?.Action != Intent.ActionBootCompleted && intent?.Action != Intent.ActionLockedBootCompleted) return;

            try
            {
                var prefPath = Path.Combine(FileSystem.AppDataDirectory, "frp_boot_opts.json");
                if (!File.Exists(prefPath)) return;
                var json = File.ReadAllText(prefPath);
                var dto = System.Text.Json.JsonSerializer.Deserialize<BootOptions>(json);
                if (dto == null) return;
                if (!(dto.Frpc || dto.Frps)) return;

                var svcIntent = new Intent(context, typeof(FrpAndroid.Services.FrpForegroundService));
                svcIntent.SetAction(FrpAndroid.Services.FrpForegroundService.ActionStart);
                svcIntent.PutExtra(FrpAndroid.Services.FrpForegroundService.ExtraStartFrpc, dto.Frpc);
                svcIntent.PutExtra(FrpAndroid.Services.FrpForegroundService.ExtraStartFrps, dto.Frps);
                if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
                    context.StartForegroundService(svcIntent);
                else
                    context.StartService(svcIntent);
            }
            catch { }
        }

        private class BootOptions { public bool Frpc { get; set; } public bool Frps { get; set; } }
    }
}
