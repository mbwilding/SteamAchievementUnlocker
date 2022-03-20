using Serilog;

namespace AchievementUnlockerAgent;

internal static class Program
{
    private const string DebugGameName = "";
    private const string DebugAppId = "";
    private const bool Debug = false;
    
    private static void Main(string[] args)
    {
        Common.Serilog.Init("Achievements");

#if LINUX
        if (!Debug) Console.SetOut(TextWriter.Null);
#endif
        string gameName = string.Empty;
        string appId = string.Empty;

#pragma warning disable CS0162
        if (Debug)
        {
            // ReSharper disable once HeuristicUnreachableCode
            gameName = DebugGameName;
            appId = DebugAppId;
        }
        else
#pragma warning restore CS0162
        {
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
        }

        var steam = new Steam();
        var result = steam.Init(gameName, appId);
        Environment.Exit(result);
    }
}