using SteamAchievementUnlockerAgent;

#if LINUX || MAC
    Console.SetOut(TextWriter.Null);
#endif

string gameName = string.Empty;

if (args.Length < 3)
    Environment.Exit(1);

for (int i = 0; i < args.Length - 1; i++)
    gameName += $"{args[i]} ";
gameName = gameName.TrimEnd();
var appId = args[^2];
var clear = args[^1].Contains("clear=True");

Common.Serilog.Init($"Achievements/{appId}", true);

var steam = new Steam(gameName, appId, clear);
var result = await steam.InitAsync().ConfigureAwait(false);
steam.Dispose();
Environment.Exit(result);
