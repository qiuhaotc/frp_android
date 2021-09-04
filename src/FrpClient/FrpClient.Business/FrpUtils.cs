using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace FrpClient.Business
{
    public static class FrpUtils
    {
        public static Process StartFrp(bool isFrps, string configuration, string location)
        {
            var cpuArchitecture = GetArchitechture(Android.OS.Build.SupportedAbis);
            var embeddedSource = typeof(FrpUtils).Namespace + (isFrps ? $".Assests.{cpuArchitecture}.frpc" : $".Assests.{cpuArchitecture}.frps");
            var frpConfigFilePath = Path.Combine(location, isFrps ? "frps.ini" : "frpc.ini");
            var frpExecutableFilePath = Path.Combine(location, isFrps ? "frps" : "frpc");
            File.WriteAllText(frpConfigFilePath, configuration);

            var assembly = typeof(FrpUtils).Assembly;
            var resFilestream = assembly.GetManifestResourceStream(embeddedSource);

            if (File.Exists(frpExecutableFilePath))
            {
                File.Delete(frpExecutableFilePath);
            }

            using (var file = new FileStream(frpExecutableFilePath, FileMode.Create, FileAccess.Write))
            {
                resFilestream.CopyTo(file);
            }

            var cmd = new[] { "chmod", "744", frpExecutableFilePath };
            Java.Lang.Runtime.GetRuntime().Exec(cmd);

            var process = Process.Start(new ProcessStartInfo
            {
                FileName = frpExecutableFilePath,
                Arguments = $"-c {frpConfigFilePath}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = false,
            });

            return process;
        }

        public static Java.Lang.Process StartFrp2(bool isFrps, string configuration, string location)
        {
            var cpuArchitecture = GetArchitechture(Android.OS.Build.SupportedAbis);
            var embeddedSource = typeof(FrpUtils).Namespace + (isFrps ? $".Assests.{cpuArchitecture}.frpc" : $".Assests.{cpuArchitecture}.frps");
            var frpConfigFilePath = Path.Combine(location, isFrps ? "frps.ini" : "frpc.ini");
            var frpExecutableFilePath = Path.Combine(location, isFrps ? "frps" : "frpc");
            File.WriteAllText(frpConfigFilePath, configuration);

            var assembly = typeof(FrpUtils).Assembly;

            if (File.Exists(frpExecutableFilePath))
            {
                File.Delete(frpExecutableFilePath);
            }

            var resFilestream = assembly.GetManifestResourceStream(embeddedSource);
            using (var file = new FileStream(frpExecutableFilePath, FileMode.Create, FileAccess.Write))
            {
                resFilestream.CopyTo(file);
            }

            var process2 = Java.Lang.Runtime.GetRuntime().Exec(new[] { "chmod", "777", frpExecutableFilePath });
            var process3 = Java.Lang.Runtime.GetRuntime().Exec(new[] { "chmod", "777", frpConfigFilePath });
            var process = Java.Lang.Runtime.GetRuntime().Exec(new[] { frpExecutableFilePath, "-c", frpConfigFilePath });
            return process;
        }

        static string GetArchitechture(IList<string> supportedAbis)
        {
            var arch = "armFrp";

            if (supportedAbis != null)
            {
                if (supportedAbis.Contains("x86") || supportedAbis.Contains("amd64"))
                {
                    arch = "amd64";
                }
                else if (supportedAbis.Contains("arm64"))
                {
                    arch = "arm64Frp";
                }
            }

            return arch;
        }
    }
}
