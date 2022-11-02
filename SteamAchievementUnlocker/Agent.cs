using System.Diagnostics;
using Serilog;

namespace SteamAchievementUnlocker;

public class Agent
{
    public static void Run(string app, string appId, string gameName, bool clear)
    {
        string arguments = $"{string.Concat(string.Join(' ', gameName.Trim()))} {appId.Trim()} clear={clear}";
        
        var startInfo = new ProcessStartInfo
        {
            WindowStyle = ProcessWindowStyle.Hidden,
            FileName = app,
            Arguments = arguments,
            CreateNoWindow = true,
            UseShellExecute = false
        };
#if LINUX
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
#endif
        var agent = Process.Start(startInfo);
        if (agent is not null)
        {
            agent.WaitForExit();
            if (agent.ExitCode == 0)
                Log.Information("Agent success: {GameName} [{AppId}]", gameName, appId);
            else
                Log.Error("Agent failed: {GameName} [{AppId}]", gameName, appId);
        }
        else
        {
            Log.Error("Agent failed to launch: {GameName} [{AppId}]", gameName, appId);
        }
    }
}