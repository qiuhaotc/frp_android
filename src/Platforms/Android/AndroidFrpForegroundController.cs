using Android.Content;
using FrpAndroid.Services;

namespace FrpAndroid.Platforms.Android;

public class AndroidFrpForegroundController : IFrpForegroundController
{
    public void EnsureServiceRunning(bool startFrpc, bool startFrps)
    {
        var ctx = global::Android.App.Application.Context;
        var intent = new Intent(ctx, typeof(FrpForegroundService));
        intent.SetAction(FrpForegroundService.ActionStart);
        intent.PutExtra(FrpForegroundService.ExtraStartFrpc, startFrpc);
        intent.PutExtra(FrpForegroundService.ExtraStartFrps, startFrps);
        if (global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.O)
            ctx.StartForegroundService(intent);
        else
            ctx.StartService(intent);
    }
}
