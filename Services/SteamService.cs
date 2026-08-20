using System.IO;
using Microsoft.Win32;

namespace CalyRecallNative.Services
{
    public class SteamService
    {
        public string GetSteamPath()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
                if (key != null)
                {
                    var path = key.GetValue("SteamPath")?.ToString();
                    if (!string.IsNullOrEmpty(path))
                    {
                        return path.Replace("/", "\\");
                    }
                }
            }
            catch { }
            return string.Empty;
        }

        public int GetRunningAppId()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
                if (key != null)
                {
                    var val = key.GetValue("RunningAppID");
                    if (val != null)
                    {
                        return (int)val;
                    }
                }
            }
            catch { }
            return 0;
        }
    }
}
