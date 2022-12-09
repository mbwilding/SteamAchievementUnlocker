using Microsoft.Extensions.Configuration;
using Serilog;

namespace Common;

public static class Config
{
    public static Settings Get()
    {
        try
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            return configuration.GetRequiredSection("Settings").Get<Settings>()!;
        }
        catch
        {
            const string msg = "Missing the 'appsettings.json' file";
            Log.Error(msg);
            throw new Exception(msg);
        }
    }

    public class Settings
    {
        public int ParallelismApps { get; set; }
        public int ParallelismAchievements { get; set; }
        public int Retries { get; set; }
    }
}