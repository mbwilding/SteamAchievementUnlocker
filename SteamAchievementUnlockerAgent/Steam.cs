using System.Threading.Tasks.Dataflow;
using Common;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Polly.Retry;
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

    private readonly Config.Settings _settings;

    private readonly RetryPolicy _policyException;
    private readonly RetryPolicy<bool> _policyBool;

    public Steam(string gameName, string appId, bool clear)
    {
        _settings = Config.Get();

        _gameName = gameName;
        _appId = appId;
        _clear = clear;

        var backoff = Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay: TimeSpan.FromSeconds(0.10), retryCount: _settings.Retries);

        // ReSharper disable PossibleMultipleEnumeration
        _policyException = Policy
            .Handle<InvalidOperationException>()
            .WaitAndRetry(backoff);

        _policyBool = Policy
            .HandleResult(false)
            .WaitAndRetry(backoff);
        // ReSharper enable PossibleMultipleEnumeration
    }

    internal async Task<int> InitAsync()
    {
        Log.Information("Game: {GameName}", _gameName);
        Log.Information("App: {AppId}", _appId);

        if (!await ConnectAsync(_appId).ConfigureAwait(false))
        {
            Log.Error("Couldn't connect to steam");
            return -1;
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
            return -1;
        }

        uint achievementCount = _policyException.Execute(SteamUserStats.GetNumAchievements);
        Log.Information("Achievements: {NumOfAchievements}", achievementCount);

        if (achievementCount > 0)
        {
            Log.Information("{Delimiter}", _delimiter);

            var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };
            var settings = new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = _settings.ParallelismAchievements };

            var splitAchievementNum = new TransformManyBlock<uint, uint>(x => Enumerable.Range(0, (int) x).Select(y => (uint) y), settings);
            var numToAchievementName = new TransformBlock<uint, string>(x => _policyException.Execute(() => SteamUserStats.GetAchievementName(x)), settings);

            var unlockAchievement = new ActionBlock<string>(UnlockAchievement, settings);
            var clearAchievement = new ActionBlock<string>(ClearAchievement, settings);

            splitAchievementNum.LinkTo(numToAchievementName, linkOptions);

            numToAchievementName.LinkTo(_clear ? clearAchievement : unlockAchievement, linkOptions);

            splitAchievementNum.Post(achievementCount);
            splitAchievementNum.Complete();

            if (_clear)
                await clearAchievement.Completion.WaitAsync(CancellationToken.None).ConfigureAwait(false);
            else
                await unlockAchievement.Completion.WaitAsync(CancellationToken.None).ConfigureAwait(false);
        }

        Log.Information("{Delimiter}", _delimiter);

        return 0;
    }

    private async Task<bool> ConnectAsync(string appId)
    {
        await File.WriteAllTextAsync(AppIdFile, appId).ConfigureAwait(false);
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

    private void UnlockAchievement(string achievement)
    {
        bool result = false;
        _policyBool.Execute(() => SteamUserStats.GetAchievement(achievement, out result));

        if (result)
        {
            Log.Information("Already Achieved: {Achievement}", achievement);
            return;
        }

        result = _policyBool.Execute(() => SteamUserStats.SetAchievement(achievement));
        if (result)
        {
            Log.Information("Unlocked: {Achievement}", achievement);
            return;
        }

        Log.Error("Failed: {Achievement}", achievement);
    }

    private void ClearAchievement(string achievement)
    {
        bool result = false;
        _policyBool.Execute(() => SteamUserStats.GetAchievement(achievement, out result));

        if (!result)
        {
            Log.Information("Already Cleared: {Achievement}", achievement);
            return;
        }

        result = _policyBool.Execute(() => SteamUserStats.ClearAchievement(achievement));
        if (result)
        {
            Log.Information("Cleared: {Achievement}", achievement);
            return;
        }

        Log.Error("Failed clearing: {Achievement}", achievement);
    }

    public void Dispose()
    {
        SteamAPI.Shutdown();
        SteamAPI.ReleaseCurrentThreadMemory();
    }
}
