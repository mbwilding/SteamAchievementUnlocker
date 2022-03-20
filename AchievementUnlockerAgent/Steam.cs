using Serilog;

namespace AchievementUnlockerAgent;

using Steamworks;

internal class Steam : IDisposable
{
    private readonly string _appIdFile = "steam_appid.txt";
    private readonly string _delimiter = string.Concat(Enumerable.Repeat("-", 20));
    private readonly int Threads = 3;

    public void Dispose()
    {
        SteamAPI.Shutdown();
        SteamAPI.ReleaseCurrentThreadMemory();
        File.Delete(_appIdFile);
    }

    public ushort Init(string gameName, string appId)
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
            Log.Error(ex, "SteamAPI failed to connect, make sure Steam is running");
        }
        return false;
    }

    private List<string> ListAchievements()
    {
        uint achievementCount = SteamUserStats.GetNumAchievements();
        Log.Information("Achievements: {NumOfAchievements}", achievementCount);
        
        var achievements = new List<string>();
        for (uint i = 0; i < achievementCount; i++)
        {
            var name = SteamUserStats.GetAchievementName(i);
            if (string.IsNullOrEmpty(name)) break;
            achievements.Add(name);
        }
        return achievements;
    }
    
    private void UnlockAchievements(List<string> achievements)
    {
        Log.Information("{Delimiter}", _delimiter);
        uint maxAttempts = 3;
        Parallel.ForEach(achievements, new ParallelOptions{MaxDegreeOfParallelism = Threads}, achievement =>
        {
            ushort attempt = 0;
            bool unlocked;
        
            do
            {
                unlocked = Loop(achievement);
                if (unlocked) break;
                attempt++;
            }
            while (attempt < maxAttempts);
            
            if (unlocked)
                Log.Information("Unlocked: {Achievement}", achievement);
            else
                Log.Error("Failed: {Achievement}", achievement);
        });
    }
    
    private bool Loop(string achievement)
    {
        try
        {
            if (!SteamUserStats.GetAchievement(achievement, out bool done)) return false;
            if (done) return true;
            if (!SteamUserStats.SetAchievement(achievement)) return false;
            if (!SteamUserStats.GetAchievement(achievement, out bool done2)) return false;
            if (done2) return true;
            return false;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Exception: {Achievement}", achievement);
            return false;
        }
    }
}