using System.Diagnostics;
using System.Xml.Linq;
using Common;
using Serilog;

namespace AchievementUnlocker;

public static class Program
{
    private static void Main()
    {
        Common.Serilog.Init("AchievementUnlocker");

#if WIN
        bool first = true;
        while (ReadRegistry(@"Software\Valve\Steam\ActiveProcess", "ActiveUser") == 0)
        {
            if (first)
            {
                Log.Information("Waiting for you to log into Steam");
                first = false;
            }
                
            Thread.Sleep(500);
        }
#endif
        
        var games = GetGameList().Result;
        
        string app = string.Empty;
        if (Platform.OS.IsWindows())
        {
            app = "Agent.exe";
        }
        else if (Platform.OS.IsLinux())
        {
            app = "Agent";
            Environment.SetEnvironmentVariable("LD_PRELOAD", Path.Combine(Directory.GetCurrentDirectory(), "libsteam_api.so"));
        }

        foreach (var game in games)
        {
            var gameName = game.Key
                .Trim(Path.GetInvalidFileNameChars())
                .Trim(Path.GetInvalidPathChars());

            var appId = game.Value;

            string arguments = $"{string.Concat(string.Join(' ', gameName))} {appId}";

            var agent = Process.Start(new ProcessStartInfo
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = app,
                Arguments = arguments,
                CreateNoWindow = true,
                UseShellExecute = false
            });
            
            if (agent is not null)
            {
                agent.WaitForExit();
                if (agent.ExitCode == 0)
                    Log.Information("Agent success: {GameName}", gameName);
                else
                    Log.Error("Agent failed: {GameName}", gameName);
            }
            else
            {
                Log.Error("Agent failed to launch: {GameName}", gameName);
            }
        }
        
        Log.Information("Finished");
        Console.ReadLine();
    }

    private static async Task<Dictionary<string, string>> GetGameList()
    {
        ulong profileId = 0;
        
#if WIN
        if (Platform.OS.IsWindows())
        {
            ulong steamId3 = ReadRegistry(@"Software\Valve\Steam\ActiveProcess", "ActiveUser");
            profileId = ((ulong)1 << 56) | ((ulong)1 << 52) | ((ulong)1 << 32) | steamId3;
        }
#endif

        if (Platform.OS.IsLinux())
        {
            var homeDir = Environment.GetEnvironmentVariable("HOME");
            var file = ".steam/steam/config/loginusers.vdf";
            var combined = Path.Combine(homeDir!, file);
            var strings = await File.ReadAllLinesAsync(combined);
            var unformatted = strings
                .ToList()
                .FindAll(x => x.Contains("765"));
            var steamIds = new List<string>();
            foreach (var text in unformatted)
            {
                steamIds.Add(text
                    .Replace("\t", "")
                    .Replace("\"", "")
                );
            }
            
            profileId = ulong.Parse(steamIds.FirstOrDefault()!);
        }
        
        var url = $"https://steamcommunity.com/profiles/{profileId}/games?xml=1";
        
        try
        {
            using(var client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                // Above three lines can be replaced with new helper method below
                // string responseBody = await client.GetStringAsync(uri);

                var dict = ParseXmlToDictionary(responseBody);
                if (dict == null || dict.Count == 0)
                    SettingFailure();
                return dict!;
            }
        }
        catch(Exception)
        {
            SettingFailure();
        }

        return default!;
    }

    private static void SettingFailure()
    {
        const string errorMsg = "Preparation required\n\n" +
                       "Sign in to steam here: 'https://steamcommunity.com/my/edit/settings'\n" +
                       "Set 'Game details' to 'Public'\n\n" +
                       "Then re-run this program";
        
        Log.Error("{Error}", errorMsg);
        Console.ReadKey();
        Environment.Exit(1);
    }
    
    private static Dictionary<string, string> ParseXmlToDictionary(string xml)
    {
        XDocument doc = XDocument.Parse(xml, LoadOptions.None);
        
        try
        {
            var mainNode = doc.Descendants("games")
                .First()
                .Elements()
                .Where(x => x.Name == "game");

            var names = new List<string>();
            var eNames = mainNode.Descendants("name");
            foreach (var element in eNames)
            {
                names.Add((string) element);
            }
            
            var appIds = new List<string>();
            var eAppIds = mainNode.Descendants("appID");
            foreach (var element in eAppIds)
            {
                appIds.Add((string) element);
            }

            var dict = names.Zip(appIds, (k, v) => new { k, v })
                .ToDictionary(x => x.k, x => x.v);
            
            Log.Debug("Successfully parsed XML as Dictionary");

            return dict;
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to parse XML as Dictionary\n{ex}");
            return null!;
        }
    }

#if WIN
    private static uint ReadRegistry(string basePath, string dword)
    {
        Microsoft.Win32.RegistryKey key = Microsoft.Win32.RegistryKey.OpenBaseKey(
            Microsoft.Win32.RegistryHive.CurrentUser, Microsoft.Win32.RegistryView.Registry64);
        key = key.OpenSubKey(basePath)!;
        
        if (key != null!)
            return Convert.ToUInt32(key.GetValue(dword)!.ToString());

        return default;
    }
#endif
}