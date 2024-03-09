namespace Common;

public static class Config
{
    public static int ParallelismApps { get; set; } = 10;
    public static int ParallelismAchievements { get; set; } = 5;
    public static int Retries { get; set; } = 2;
}