using System.Diagnostics;
using Serilog;
using System.Text.RegularExpressions;
using Common;
using SteamAchievementUnlocker;

Process.GetProcessesByName("SteamAchievementUnlockerAgent")
    .ToList()
    .ForEach(x => x.Kill());

Common.Serilog.Init("Achievement Unlocker", false);

#if WIN
bool first = true;
while (Helpers.ReadRegistry(@"Software\Valve\Steam\ActiveProcess", "ActiveUser") == 0)
{
    if (first)
    {
        Log.Information("Waiting for you to log into Steam\n");
        first = false;
    }

    await Task.Delay(500).ConfigureAwait(false);
}
string app = "SteamAchievementUnlockerAgent.exe";
#elif LINUX
    Log.Information("Make sure Steam is running and logged in");
    Log.Information("Otherwise the following will all fail\n");
    string app = "SteamAchievementUnlockerAgent";
    Environment.SetEnvironmentVariable("LD_PRELOAD", Path.Combine(Directory.GetCurrentDirectory(), "libsteam_api.so"));
#endif

const string clearString = "--clear";
bool clearToggle = args.Contains(clearString);
var argsList = args.Where(x => !x.Contains(clearString)).ToList();

var settings = Config.Get();
var options = new ParallelOptions { MaxDegreeOfParallelism = settings.ParallelismApps };

if (argsList.Any())
{
    await Parallel.ForEachAsync(argsList.Distinct(), options, async (appId, _) =>
    {
        if (uint.TryParse(appId, out var _))
            await Agent.RunAsync(app, appId, appId, clearToggle).ConfigureAwait(false);
        else
            Log.Error("Please enter a numerical app ID: {Arg}", appId);
    }).ConfigureAwait(false);
}
else
{
    var games = await Helpers.GetGameListAsync().ConfigureAwait(false);
    
    await Parallel.ForEachAsync(games, options, async (game, _) =>
    {
        var gameName = game.Key
            .Trim(Path.GetInvalidFileNameChars())
            .Trim(Path.GetInvalidPathChars());

        var appId = game.Value;

        Regex rgx = new Regex("[^a-zA-Z0-9 ()&$:_ -]");
        gameName = rgx.Replace(gameName, string.Empty);
        await Agent.RunAsync(app, appId, gameName, clearToggle).ConfigureAwait(false);
    }).ConfigureAwait(false);
}

if (Directory.Exists("Apps"))
    Directory.Delete("Apps", true);

Console.WriteLine("\nPress any key to exit");
Console.ReadKey();