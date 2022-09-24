using System.Xml.Linq;
using Serilog;

namespace SteamAchievementUnlocker;

public static class Helpers
{
    internal static async Task<Dictionary<string, string>> GetGameList()
    {
        ulong profileId;
#if WIN
        ulong steamId3 = ReadRegistry(@"Software\Valve\Steam\ActiveProcess", "ActiveUser");
        profileId = ((ulong)1 << 56) | ((ulong)1 << 52) | ((ulong)1 << 32) | steamId3;
#elif LINUX
        var homeDir = Environment.GetEnvironmentVariable("HOME");
        var file = ".steam/steam/config/loginusers.vdf";
        var combined = Path.Combine(homeDir!, file);
        var lines = await File.ReadAllLinesAsync(combined);
        var steamIds = lines
            .ToList()
            .Find(x => x.StartsWith("\t\"765"))!
            .Replace("\t", "")
            .Replace("\"", "");

        profileId = ulong.Parse(steamIds);
#endif

        var url = $"https://steamcommunity.com/profiles/{profileId}/games?xml=1";

        try
        {
            using var client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();
            // Above three lines can be replaced with new helper method below
            // string responseBody = await client.GetStringAsync(uri);

            var dict = ParseXmlToDictionary(responseBody);
            if (dict is not { Count: not 0 })
                SettingFailure();
            return dict;
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
        Console.ReadLine();
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
                .Where(x => x.Name == "game")
                .ToList();

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

            Log.Information("Total Applications: {Count}", dict.Count);

            return dict;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to parse XML as Dictionary");
            return null!;
        }
    }
#pragma warning disable CA1416
#if WIN
    internal static uint ReadRegistry(string basePath, string dword)
    {
        Microsoft.Win32.RegistryKey key = Microsoft.Win32.RegistryKey.OpenBaseKey(
            Microsoft.Win32.RegistryHive.CurrentUser, Microsoft.Win32.RegistryView.Registry64);

        key = key.OpenSubKey(basePath)!;

        return key != null! ?
            Convert.ToUInt32(key.GetValue(dword)!.ToString()) : default;
    }
#endif
#pragma warning restore CA1416
}
