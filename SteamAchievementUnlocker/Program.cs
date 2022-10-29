using System.Diagnostics;
using Serilog;
using System.Text.RegularExpressions;
using SteamAchievementUnlocker;

const string title = "Achievement Unlocker";

Common.Serilog.Init(title);
Log.Information("Started: {Title}", title);

#if WIN
bool first = true;
while (Helpers.ReadRegistry(@"Software\Valve\Steam\ActiveProcess", "ActiveUser") == 0)
{
    if (first)
    {
        Log.Information("Waiting for you to log into Steam");
        first = false;
    }
            
    Thread.Sleep(500);
}
string app = "SteamAchievementUnlockerAgent.exe";
#elif LINUX
    Log.Information("Make sure Steam is running and logged in");
    Log.Information("Otherwise the following will all fail");
    string app = "SteamAchievementUnlockerAgent";
    Environment.SetEnvironmentVariable("LD_PRELOAD", Path.Combine(Directory.GetCurrentDirectory(), "libsteam_api.so"));
#endif

var games = Helpers.GetGameList().Result;
foreach (var game in games)
{
    var gameName = game.Key
        .Trim(Path.GetInvalidFileNameChars())
        .Trim(Path.GetInvalidPathChars());

    var appId = game.Value;

    Regex rgx = new Regex("[^a-zA-Z0-9 ()&$:_ -]");
    gameName = rgx.Replace(gameName, "");
    string arguments = $"{string.Concat(string.Join(' ', gameName.Trim()))} {appId.Trim()}";
    var startInfo = new ProcessStartInfo
    {
        WindowStyle = ProcessWindowStyle.Hidden,
        FileName = app,
        Arguments = arguments,
        CreateNoWindow = true,
        UseShellExecute = false
    };
#if LINUX
    startInfo.RedirectStandardOutput = true;
    startInfo.RedirectStandardError = true;
#endif
    var agent = Process.Start(startInfo);
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

Log.Information("Finished: {Title}", title);
