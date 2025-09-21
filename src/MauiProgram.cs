﻿using Microsoft.Extensions.Logging;
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
