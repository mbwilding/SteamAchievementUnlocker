using Serilog;

namespace AchievementUnlockerAgent;

using Steamworks;

public class SteamWorksFuncs : IDisposable
{
    private readonly string IdFile = "steam_appid.txt";
    
    public void Dispose()
    {
        SteamAPI.Shutdown();
        SteamAPI.ReleaseCurrentThreadMemory();
        File.Delete(IdFile);
    }

    public int Init(string gameName, string appId)
    {
        if (!Connect(appId)) return 1;
        SteamUserStats.RequestCurrentStats();
        Log.Information("Game: {GameName}", gameName);
        Log.Information("App: {AppId}", appId);
        var achievements = ListAchievements();
        UnlockAchievements(achievements);
        Log.Information("{Delimiter}", string.Concat(Enumerable.Repeat("-", 20)));
        return 0;
    }

    public bool Connect(string appId)
    {
        File.WriteAllText(IdFile, appId);
        try
        {
            return SteamAPI.Init();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "SteamAPI failed to connect");
        }

        return false;
    }

    private void TotalAchievements()
    {
        Log.Information("Achievements: {NumOfAchievements}", SteamUserStats.GetNumAchievements());
    }
    
    private List<string> ListAchievements()
    {
        TotalAchievements();
        
        var achievements = new List<string>();
        for (uint i = 0; i < uint.MaxValue; i++)
        {
            var name = SteamUserStats.GetAchievementName(i);
            if (string.IsNullOrEmpty(name))
                break;
            achievements.Add(name);
        }
        return achievements;
    }
    
    private void UnlockAchievements(List<string> achievements)
    {
        Log.Information("Unlocking all achievements");
        foreach (var achievement in achievements)
        {
            SteamUserStats.SetAchievement(achievement);
            Log.Information("Unlocked: {Achievement}", achievement);
        }
    }
}