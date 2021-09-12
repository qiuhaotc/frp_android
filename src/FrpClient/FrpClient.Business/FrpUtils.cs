using System.IO;

namespace FrpClient.Business
{
    public static class FrpUtils
    {
        public static void StartFrp(bool isFrps, string configuration, string location)
        {
            var frpConfigFilePath = Path.Combine(location, isFrps ? "frps.ini" : "frpc.ini");
            File.WriteAllText(frpConfigFilePath, configuration);

            Frpclib.Frpclib.Run(frpConfigFilePath);
        }

        public static string GetFrpConfiguration(bool isFrps, string location)
        {
            var frpConfigFilePath = Path.Combine(location, isFrps ? "frps.ini" : "frpc.ini");
            if (File.Exists(frpConfigFilePath))
            {
                return File.ReadAllText(frpConfigFilePath);
            }

            return string.Empty;
        }
    }
}
