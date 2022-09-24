using System.Diagnostics;
using Serilog;
using Serilog.Debugging;
using Serilog.Events;

namespace Common;

public static class Serilog
{
    public static void Init(string name)
    {
        SelfLog.Enable(message => Trace.WriteLine($"INTERNAL ERROR: {message}"));
        
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Console(LogEventLevel.Information)
            .WriteTo.File(
                $"Logs/{name}..log",
                rollingInterval: RollingInterval.Day)
            .CreateLogger();
    }
}
