using Android.App;
using Android.Content.PM;
using Android.OS;
using Android;
using Android.Content;
using System.Text.Json;
using FrpAndroid.Services;

namespace FrpAndroid
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        const int RequestNotificationId = 0x9910;
        readonly string _bootPrefFile = Path.Combine(FileSystem.AppDataDirectory, "frp_boot_opts.json");

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            TryRequestNotificationPermission();
            TryAutoStartSelectedServices();
        }

        void TryRequestNotificationPermission()
        {
#if ANDROID
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
            {
                if (CheckSelfPermission(Manifest.Permission.PostNotifications) != Permission.Granted)
                {
                    RequestPermissions(new[] { Manifest.Permission.PostNotifications }, RequestNotificationId);
                }
            }
#endif
        }

        void TryAutoStartSelectedServices()
        {
            try
            {
                if (FrpForegroundService.IsRunning) return; // already
                if (!File.Exists(_bootPrefFile)) return;
                var json = File.ReadAllText(_bootPrefFile);
                var opts = JsonSerializer.Deserialize<BootOptions>(json);
                if (opts == null) return;
                if (!(opts.Frpc || opts.Frps)) return;

                var ctx = global::Android.App.Application.Context;
                var intent = new Intent(ctx, typeof(FrpForegroundService));
                intent.SetAction(FrpForegroundService.ActionStart);
                intent.PutExtra(FrpForegroundService.ExtraStartFrpc, opts.Frpc);
                intent.PutExtra(FrpForegroundService.ExtraStartFrps, opts.Frps);
                if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                    ctx.StartForegroundService(intent);
                else
                    ctx.StartService(intent);
            }
            catch { }
        }

        class BootOptions { public bool Frpc { get; set; } public bool Frps { get; set; } }
    }
}
