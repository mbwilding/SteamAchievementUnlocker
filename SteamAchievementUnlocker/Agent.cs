using System.Diagnostics;
using Serilog;

namespace SteamAchievementUnlocker;

public class Agent
{
    public static async Task RunAsync(string app, string appId, string gameName, bool clear)
    {
        var dir = Clone(appId);
        
        string arguments = $"{string.Concat(string.Join(' ', gameName.Trim()))} {appId.Trim()} clear={clear}";

        var startInfo = new ProcessStartInfo
        {
            WindowStyle = ProcessWindowStyle.Hidden,
            FileName = $"{dir}{app}",
            Arguments = arguments,
            CreateNoWindow = true,
            UseShellExecute = false,
            WorkingDirectory = dir
        };
#if LINUX
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
#endif
        var agent = Process.Start(startInfo);
        if (agent is not null)
        {
            await agent.WaitForExitAsync().ConfigureAwait(false);
            if (agent.ExitCode == 0)
                Log.Information("Agent success: {GameName} [{AppId}]", gameName, appId);
            else
                Log.Error("Agent failed: {GameName} [{AppId}]", gameName, appId);
        }
        else
        {
            Log.Error("Agent failed to launch: {GameName} [{AppId}]", gameName, appId);
        }
        
        Directory.Delete(dir, true);
    }

    private static string Clone(string appId)
    {
        var current = Directory.GetCurrentDirectory();
        var dir = $"{current}/Apps/{appId}/";
        if (Directory.Exists(dir))
            Directory.Delete(dir, true);
        Directory.CreateDirectory(dir);
        Directory.CreateDirectory($"{dir}runtimes");
        
        var files = Directory.EnumerateFiles(current).Where(x =>
            Path.GetFileNameWithoutExtension(x).Contains("SteamAchievementUnlockerAgent") ||
            Path.GetExtension(x).Contains(".dll") ||
            Path.GetExtension(x).Contains(".json"));

        var runtimes = Directory.EnumerateFiles($"{current}/runtimes", "*.*", new EnumerationOptions { RecurseSubdirectories = true });

        foreach (var file in runtimes)
        {
            var directory = file.Replace(Path.GetFileName(file), string.Empty).Replace("runtimes", $"Apps/{appId}/runtimes");
            Directory.CreateDirectory($"{directory}");
            File.CreateSymbolicLink($"{directory}/{Path.GetFileName(file)}", file);
        }
        
        foreach (var file in files)
            File.CreateSymbolicLink($"{dir}{Path.GetFileName(file)}", file);

        return dir;
    }
}