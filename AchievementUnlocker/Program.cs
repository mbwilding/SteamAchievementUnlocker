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
        Log.Information("Started");

        var games = GetGameList().Result;

        string steamworksDll = "Steamworks.NET.dll";
        string exe = string.Empty;
        if (Platform.OS.IsWindows())
        {
            exe = "Agent.exe";
            if (File.Exists(steamworksDll))
                File.Delete(steamworksDll);
            File.Copy($"runtimes/win-x64/lib/netstandard2.1/{steamworksDll}", steamworksDll);
        }
        else if (Platform.OS.IsLinux())
        {
            exe = "Agent";
            if (File.Exists(steamworksDll))
                File.Delete(steamworksDll);
            File.Copy($"runtimes/linux-x64/lib/netstandard2.1/{steamworksDll}", steamworksDll);


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
                FileName = exe,
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
        if (Platform.OS.IsWindows())
        {
            ulong steamId3 = ReadRegistry(@"Software\Valve\Steam\ActiveProcess", "ActiveUser");
            profileId = ((ulong)1 << 56) | ((ulong)1 << 52) | ((ulong)1 << 32) | steamId3;
        }

        if (Platform.OS.IsLinux())
        {
            var homeDir = Environment.GetEnvironmentVariable("HOME");
            var file = ".steam/steam/config/loginusers.vdf";
            var combined = Path.Combine(homeDir, file);
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

                return ParseXmlToDictionary(responseBody);
            }
        }
        catch(HttpRequestException ex)
        {
            string msg = "Make sure your steam profile is public";
            Log.Error("{Msg}\n{Error}", msg, ex.Message);
            throw new Exception(msg, ex);
        }
    }
    
    private static Dictionary<string, string> ParseXmlToDictionary(string xml)
    {
        XDocument doc = XDocument.Parse(xml, LoadOptions.None);

        var result = new Dictionary<string, string>();
        try
        {
            var result2 = doc.Descendants("games")
                .First()
                .Elements()
                .Where(x => x.Name == "game");

            var names = new List<string>();
            var eNames = result2.Descendants("name");
            foreach (var element in eNames)
            {
                names.Add((string) element);
            }
            
            var appIds = new List<string>();
            var eAppIds = result2.Descendants("appID");
            foreach (var element in eAppIds)
            {
                appIds.Add((string) element);
            }

            var dic = names.Zip(appIds, (k, v) => new { k, v })
                .ToDictionary(x => x.k, x => x.v);
            
            Log.Debug("Successfully parsed XML as Dictionary");

            return dic;
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to parse XML as Dictionary\n{ex}");
            return result;
        }
    }

    private static uint ReadRegistry(string basePath, string dword)
    {
#if WIN
        Microsoft.Win32.RegistryKey key = Microsoft.Win32.RegistryKey.OpenBaseKey(
            Microsoft.Win32.RegistryHive.CurrentUser, Microsoft.Win32.RegistryView.Registry64);
        key = key.OpenSubKey(basePath);
        
        if (key != null)
        {
            return Convert.ToUInt32(key.GetValue(dword).ToString());
        }
#endif
        return default; // TODO
    }
}