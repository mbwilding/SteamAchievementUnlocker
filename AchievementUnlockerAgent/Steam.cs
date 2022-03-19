using Serilog;

namespace AchievementUnlockerAgent;

using Steamworks;

internal class Steam : IDisposable
{
    private readonly string _appIdFile = "steam_appid.txt";
    private readonly string _delimiter = string.Concat(Enumerable.Repeat("-", 20));
    
    public void Dispose()
    {
        SteamAPI.Shutdown();
        SteamAPI.ReleaseCurrentThreadMemory();
        File.Delete(_appIdFile);
    }

    public int Init(string gameName, string appId)
    {
        if (!Connect(appId)) return 1;
        SteamUserStats.RequestCurrentStats();
        Log.Information("Game: {GameName}", gameName);
        Log.Information("App: {AppId}", appId);
        var achievements = ListAchievements();
        if (achievements.Count != 0)
            UnlockAchievements(achievements);
        Log.Information("{Delimiter}", _delimiter);
        Dispose();
        return 0;
    }

    private bool Connect(string appId)
    {
        File.WriteAllText(_appIdFile, appId);
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
        Log.Information("{Delimiter}", _delimiter);
        foreach (var achievement in achievements)
        {
            try
            {
                if (SteamUserStats.GetAchievement(achievement, out bool done))
                {
                    if (!done)
                    {
                        if (SteamUserStats.SetAchievement(achievement))
                            Log.Information("Unlocked: {Achievement}", achievement);
                        continue;
                    }
                    Log.Information("Complete: {Achievement}", achievement);
                    continue;
                }
                Log.Error("Failed: {Achievement}", achievement);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception: {Achievement}", achievement);
            }
        }
    }
}