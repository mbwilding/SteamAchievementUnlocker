namespace AchievementUnlockerAgent;

internal static class Program
{
    private static void Main(string[] args)
    {
#if LINUX
        Console.SetOut(TextWriter.Null);
        Environment.SetEnvironmentVariable("LD_PRELOAD", Path.Combine(Directory.GetCurrentDirectory(), "libsteam_api.so"));
#endif
        if (args.Length < 2)
            Environment.Exit(1);
        
        Common.Serilog.Init("Achievements");

        string gameName = string.Empty;
        for (int i = 0; i < args.Length - 1; i++)
            gameName += $"{args[i]} ";
        gameName = gameName.TrimEnd();
        var appId = args[args.Length - 1];
        
        var steam = new Steam();
        var result = steam.Init(gameName, appId);
        Environment.Exit(result);
    }
}