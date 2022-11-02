using System.Threading.Tasks.Dataflow;
using Serilog;
using Steamworks;

namespace SteamAchievementUnlockerAgent;

internal class Steam : IDisposable
{
    private const string AppIdFile = "steam_appid.txt";
    private readonly string _gameName;
    private readonly string _appId;
    private readonly bool _clear;
    private readonly string _delimiter = string.Concat(Enumerable.Repeat("-", 20));
    
    private const int _parallelism = 5;
    private const uint _maxAttempts = 3;

    public Steam(string gameName, string appId, bool clear)
    {
        _gameName = gameName;
        _appId = appId;
        _clear = clear;
    }

    internal async Task<ushort> Init()
    {
        Log.Information("Game: {GameName}", _gameName);
        Log.Information("App: {AppId}", _appId);
        Log.Information("Clear: {Clear}", _clear.ToString().ToUpper());

        if (!await Connect(_appId))
        {
            Log.Error("Couldn't connect to steam");
            return 1;
        }
        
        try
        {
            InteropHelp.TestIfAvailableClient();
        }
        catch (InvalidOperationException ex)
        {
            Log.Error(ex, "SteamWorks not available");
        }
        
        if (!SteamUserStats.RequestCurrentStats())
        {
            Log.Error("Couldn't retrieve stats");
            return 1;
        }

        int achievementCount = await GetAchievementCount();
        Log.Information("Achievements: {NumOfAchievements}", achievementCount);

        if (achievementCount > 0)
        {
            Log.Information("{Delimiter}", _delimiter);
            
            var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };
            var settings = new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = _parallelism };
        
            var splitAchievementNum = new TransformManyBlock<int, int>(x => Enumerable.Range(0, x), settings);
            var numToAchievementName = new TransformBlock<int, string>(x => SteamUserStats.GetAchievementName((uint) x), settings);
            
            var unlockAchievement = new ActionBlock<string>(UnlockAchievement, settings);
            var clearAchievement = new ActionBlock<string>(ClearAchievement, settings);

            splitAchievementNum.LinkTo(numToAchievementName, linkOptions);

            numToAchievementName.LinkTo(_clear ? clearAchievement : unlockAchievement, linkOptions);

            splitAchievementNum.Post(achievementCount);
            splitAchievementNum.Complete();

            if (_clear)
                await clearAchievement.Completion.WaitAsync(CancellationToken.None);
            else
                await unlockAchievement.Completion.WaitAsync(CancellationToken.None);
        }
        
        Log.Information("{Delimiter}", _delimiter);
        
        return 0;
    }
    
    private async Task<bool> Connect(string appId)
    {
        await File.WriteAllTextAsync(AppIdFile, appId);
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

    private async Task<int> GetAchievementCount()
    {
        List<uint> maxResult = new List<uint>();
        for (int i = 0; i < _maxAttempts; i++)
        {
            maxResult.Add(SteamUserStats.GetNumAchievements());
            await Task.Delay(50);
        }
        return (int) maxResult.Max();
    }
    
    private void UnlockAchievement(string achievement)
    {
        ushort attempt = 0;
        bool success;
        bool alreadyDone;

        do
        {
            success = UnlockLoop(achievement, out alreadyDone);
            if (success || alreadyDone)
                break;
            attempt++;
        } while (attempt < _maxAttempts);

        if (alreadyDone)
            Log.Debug("Already Achieved: {Achievement}", achievement);
        else if (success)
            Log.Information("Unlocked: {Achievement}", achievement);
        else
            Log.Error("Failed: {Achievement}", achievement);
    }
    
    private bool UnlockLoop(string achievement, out bool alreadyDone)
    {
        try
        {
            if (SteamUserStats.GetAchievement(achievement, out bool done1))
            {
                if (done1)
                {
                    alreadyDone = true;
                    return true;
                }
            }
            else
            {
                alreadyDone = false;
                return false;
            }

            if (!SteamUserStats.SetAchievement(achievement))
            {
                alreadyDone = false;
                return false;
            }

            if (SteamUserStats.GetAchievement(achievement, out bool done2))
            {
                if (done2)
                {
                    alreadyDone = false;
                    return true;
                }
            }
            else
            {
                alreadyDone = false;
                return false;
            }
        }
        catch (InvalidOperationException ex)
        {
            Log.Error(ex, "Invalid Operation Exception: {Achievement}", achievement);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unknown Exception: {Achievement}", achievement);
        }

        alreadyDone = false;
        return false;
    }
    
    private void ClearAchievement(string achievement)
    {
        ushort attempt = 0;
        bool success;
        bool alreadyCleared;

        do
        {
            success = ClearLoop(achievement, out alreadyCleared);
            if (success || alreadyCleared)
                break;
            attempt++;
        } while (attempt < _maxAttempts);

        if (alreadyCleared)
            Log.Debug("Already Cleared: {Achievement}", achievement);
        else if (success)
            Log.Information("Cleared: {Achievement}", achievement);
        else
            Log.Error("Failed clearing: {Achievement}", achievement);
    }
    
    private bool ClearLoop(string achievement, out bool alreadyCleared)
    {
        try
        {
            if (SteamUserStats.GetAchievement(achievement, out bool done1))
            {
                if (!done1)
                {
                    alreadyCleared = true;
                    return true;
                }
            }
            else
            {
                alreadyCleared = false;
                return false;
            }

            if (!SteamUserStats.ClearAchievement(achievement))
            {
                alreadyCleared = false;
                return false;
            }

            if (SteamUserStats.GetAchievement(achievement, out bool done2))
            {
                if (!done2)
                {
                    alreadyCleared = true;
                    return true;
                }
            }
            else
            {
                alreadyCleared = false;
                return false;
            }
        }
        catch (InvalidOperationException ex)
        {
            Log.Error(ex, "Invalid Operation Exception: {Achievement}", achievement);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unknown Exception: {Achievement}", achievement);
        }

        alreadyCleared = false;
        return false;
    }

    public async void Dispose()
    {
        SteamAPI.Shutdown();
        SteamAPI.ReleaseCurrentThreadMemory();
        File.Delete(AppIdFile);
        await Task.Delay(100);
    }
}
