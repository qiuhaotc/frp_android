using Android.App;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using FrpClient.Business;
using Google.Android.Material.BottomNavigation;

namespace FrpClient.UI
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity, BottomNavigationView.IOnNavigationItemSelectedListener
    {
        Button startButton;
        Button stopButton;
        EditText config;
        EditText logs;
        System.Diagnostics.Process process;
        Java.Lang.Process process2;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            BottomNavigationView navigation = FindViewById<BottomNavigationView>(Resource.Id.navigation);
            navigation.SetOnNavigationItemSelectedListener(this);

            startButton = FindViewById<Button>(Resource.Id.buttonStart);
            startButton.Click += StartButton_Click;
            stopButton = FindViewById<Button>(Resource.Id.buttonStop);
            stopButton.Click += StartButton_Click1;
            config = FindViewById<EditText>(Resource.Id.editTextForConfig);
            logs = FindViewById<EditText>(Resource.Id.logs);
        }

        void StartButton_Click1(object sender, System.EventArgs e)
        {
            try
            {
                process?.Kill();
                process2?.Destroy();
            }
            catch (System.Exception ex)
            {
                logs.Text = logs.Text + System.Environment.NewLine + ex;
            }

            process = null;
        }

        void StartButton_Click(object sender, System.EventArgs e)
        {
            StartButton_Click1(null, null);

            var filePath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
            var isFrps = FindViewById<RadioButton>(Resource.Id.frpsType).Checked;

            process2 = FrpUtils.StartFrp2(isFrps, config.Text, filePath);
            var output = process2.IsAlive;

            process = FrpUtils.StartFrp(isFrps, config.Text, filePath);
            var b = process.ExitCode;

            //while (!process.StandardOutput.EndOfStream)
            //{
            //    string line = process.StandardOutput.ReadLine();
            //    // do something with line
            //    logs.Text = logs.Text + System.Environment.NewLine + line;
            //}
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        public bool OnNavigationItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.navigation_home:
                    return true;

                case Resource.Id.navigation_dashboard:
                    return true;

                case Resource.Id.navigation_notifications:
                    return true;
            }

            return false;
        }
    }
}

