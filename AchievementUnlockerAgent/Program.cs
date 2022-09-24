using AchievementUnlockerAgent;

Common.Serilog.Init("Achievements");

#if LINUX
    Console.SetOut(TextWriter.Null);
#endif

string gameName = string.Empty;
string appId = string.Empty;

if (args.Length == 1)
{
    string id = args[0];
    if (!uint.TryParse(id, out _))
        Environment.Exit(1);
    gameName = appId = id;
}
else
{
    if (args.Length < 2)
        Environment.Exit(1);

    for (int i = 0; i < args.Length - 1; i++)
        gameName += $"{args[i]} ";
    gameName = gameName.TrimEnd();
    appId = args[^1];
}

var steam = new Steam();
var result = steam.Init(gameName, appId);
Environment.Exit(result);
