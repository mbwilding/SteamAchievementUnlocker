using Serilog;
using System.Text.RegularExpressions;
using SteamAchievementUnlocker;

Common.Serilog.Init("Achievement Unlocker");

#if WIN
bool first = true;
while (Helpers.ReadRegistry(@"Software\Valve\Steam\ActiveProcess", "ActiveUser") == 0)
{
    if (first)
    {
        Log.Information("Waiting for you to log into Steam\n");
        first = false;
    }
            
    await Task.Delay(500);
}
string app = "SteamAchievementUnlockerAgent.exe";
#elif LINUX
    Log.Information("Make sure Steam is running and logged in");
    Log.Information("Otherwise the following will all fail\n");
    string app = "SteamAchievementUnlockerAgent";
    Environment.SetEnvironmentVariable("LD_PRELOAD", Path.Combine(Directory.GetCurrentDirectory(), "libsteam_api.so"));
#endif

const string clearString = "-clear";
bool clearToggle = args.Contains(clearString);
var argsList = args.Where(x => !x.Contains(clearString)).ToList();

if (argsList.Any())
{
    foreach (var appId in argsList)
    {
        if (uint.TryParse(appId, out _))
        {
            Agent.Run(app, appId, "Manual", clearToggle);
        }
        else
            Log.Error("Please enter a numerical app ID: {Arg}", appId);
    }
}
else
{
    var games = await Helpers.GetGameList();
    foreach (var game in games)
    {
        var gameName = game.Key
            .Trim(Path.GetInvalidFileNameChars())
            .Trim(Path.GetInvalidPathChars());

        var appId = game.Value;

        Regex rgx = new Regex("[^a-zA-Z0-9 ()&$:_ -]");
        gameName = rgx.Replace(gameName, "");
        Agent.Run(app, appId, gameName, clearToggle);
    }
}

Console.WriteLine("\nPress any key to exit");
Console.ReadKey();