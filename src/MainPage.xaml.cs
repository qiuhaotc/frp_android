namespace FrpAndroid
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
#if ANDROID
            Loaded += OnLoaded;
#endif
        }

#if ANDROID
        private void OnLoaded(object? sender, EventArgs e)
        {
            var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
            if (activity?.Window?.DecorView is Android.Views.View decorView)
            {
                var insets = AndroidX.Core.View.ViewCompat.GetRootWindowInsets(decorView);
                if (insets != null)
                {
                    try
                    {

                        int statusBarPx = insets.GetInsets(AndroidX.Core.View.WindowInsetsCompat.Type.StatusBars())?.Top ?? 0;
                        if (statusBarPx > 0)
                        {
                            float density = activity.Resources?.DisplayMetrics?.Density ?? 1f;
                            Padding = new Thickness(0, statusBarPx / density, 0, 0);
                        }
                    }
                    catch (Exception)
                    {
                        // ignore
                    }
                }
            }
        }
#endif
    }
}
