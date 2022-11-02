using Serilog;
using SteamAchievementUnlockerAgent;

Common.Serilog.Init("Achievements");

#if LINUX
    Console.SetOut(TextWriter.Null);
#endif

string gameName = string.Empty;
string appId;
bool clear;

if (args.Length < 3)
{
    Log.Error("Invalid argument count");
    Environment.Exit(1);
}

for (int i = 0; i < args.Length - 1; i++)
    gameName += $"{args[i]} ";
gameName = gameName.TrimEnd();
appId = args[^2];
clear = args[^1].Contains("clear=True");

var steam = new Steam(gameName, appId, clear);
var result = await steam.Init();
steam.Dispose();
Environment.Exit(result);
