using Serilog;
using SteamAchievementUnlockerAgent;

Common.Serilog.Init("Achievements");

#if LINUX
    Console.SetOut(TextWriter.Null);
#endif

string gameName = string.Empty;
string appId;

if (args.Length < 2)
{
    Log.Error("Invalid argument count");
    Environment.Exit(1);
}

for (int i = 0; i < args.Length - 1; i++)
    gameName += $"{args[i]} ";
gameName = gameName.TrimEnd();
appId = args[^1];

var steam = new Steam(gameName, appId);
var result = await steam.Init();
Environment.Exit(result);
