using Common;

namespace AchievementUnlockerAgent;

public static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length < 2) return 1;

        string gameName = string.Empty;
        for (int i = 0; i < args.Length - 1; i++)
        {
            gameName += args[i] + " ";
        }
        gameName = gameName.TrimEnd();
        var appId = args[args.Length - 1];

        Common.Serilog.Init("Achievements");
        
        if(Platform.OS.IsLinux())
            Environment.SetEnvironmentVariable("LD_PRELOAD", Path.Combine(Directory.GetCurrentDirectory(), "libsteam_api.so"));

        var steam = new SteamWorksFuncs();
        var result = steam.Init(gameName, appId);
        steam.Dispose();
        return result;
    }
}