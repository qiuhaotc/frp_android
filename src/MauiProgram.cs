using Microsoft.Extensions.Logging;
using FrpAndroid.Services;
using FrpAndroid.Platforms.Android;

namespace FrpAndroid
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    // 如果添加了中文字体文件（例如 Resources/Fonts/NotoSansSC-Regular.otf），解除下面注释
                    // fonts.AddFont("NotoSansSC-Regular.otf", "NotoSansSC");
                });

            builder.Services.AddMauiBlazorWebView();
            builder.Services.AddSingleton<FrpManager>();
#if ANDROID
            builder.Services.AddSingleton<IFrpForegroundController, AndroidFrpForegroundController>();
#endif
#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif
            return builder.Build();
        }
    }
}
