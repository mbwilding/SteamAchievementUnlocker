using System.Diagnostics;
using Serilog;
using Serilog.Debugging;
using Serilog.Events;

namespace Common;

public static class Serilog
{
    public static void Init(string name)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Async(x => x.Console(LogEventLevel.Information))
            .WriteTo.Async(x => x.File($"Logs/{DateTime.Now:yyyyMMdd}/{name}.log"))
            .CreateLogger();
    }
}
