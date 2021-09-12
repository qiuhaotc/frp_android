using System.Threading;
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
        Thread frpThread;

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
            stopButton.Click += StopButton_Click;
            config = FindViewById<EditText>(Resource.Id.editTextForConfig);
            logs = FindViewById<EditText>(Resource.Id.logs);
        }

        void StopButton_Click(object sender, System.EventArgs e)
        {
            Process.KillProcess(Android.OS.Process.MyPid());
        }

        void StartButton_Click(object sender, System.EventArgs e)
        {
            var filePath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
            var isFrps = FindViewById<RadioButton>(Resource.Id.frpsType).Checked;

            try
            {
                if (frpThread == null)
                {
                    frpThread = new Thread(new ThreadStart(StartFrp));
                    frpThread.Start();
                }
            }
            catch (System.Exception ex)
            {
                Toast.MakeText(null, ex.Message, ToastLength.Long);
            }
        }

        void StartFrp()
        {
            var filePath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
            var isFrps = FindViewById<RadioButton>(Resource.Id.frpsType).Checked;
            FrpUtils.StartFrp(isFrps, config.Text, filePath);
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

