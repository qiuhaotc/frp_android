namespace FrpAndroid.Services;

public interface IFrpForegroundController
{
    void EnsureServiceRunning(bool startFrpc, bool startFrps);
}
