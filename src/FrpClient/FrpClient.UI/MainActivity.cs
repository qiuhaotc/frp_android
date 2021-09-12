using System.Threading;
using Android.App;
using Android.OS;
using Android.Runtime;
using Android.Widget;
using AndroidX.AppCompat.App;
using FrpClient.Business;

namespace FrpClient.UI
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        Button startButton;
        Button stopButton;
        EditText config;
        EditText logs;
        Thread frpThread;
        RadioButton chooseFrps;
        string filePath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            startButton = FindViewById<Button>(Resource.Id.buttonStart);
            startButton.Click += StartButton_Click;
            stopButton = FindViewById<Button>(Resource.Id.buttonStop);
            stopButton.Click += StopButton_Click;
            config = FindViewById<EditText>(Resource.Id.editTextForConfig);
            logs = FindViewById<EditText>(Resource.Id.logs);

            chooseFrps = FindViewById<RadioButton>(Resource.Id.frpsType);
            config.Text = FrpUtils.GetFrpConfiguration(IsFrps, filePath);
        }

        bool IsFrps => chooseFrps.Checked;

        void StopButton_Click(object sender, System.EventArgs e)
        {
            Process.KillProcess(Process.MyPid());
        }

        void StartButton_Click(object sender, System.EventArgs e)
        {
            var isFrps = FindViewById<RadioButton>(Resource.Id.frpsType).Checked;

            try
            {
                logs.Text = string.Empty;

                if (frpThread == null)
                {
                    frpThread = new Thread(new ThreadStart(StartFrp));
                    frpThread.Start();
                    startButton.Enabled = false;
                    logs.Text = "Frp Start";
                }
            }
            catch (System.Exception ex)
            {
                logs.Text = logs.Text + System.Environment.NewLine + ex;
            }
        }

        void StartFrp()
        {
            FrpUtils.StartFrp(IsFrps, config.Text, filePath);
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}

