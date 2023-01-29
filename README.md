# Steam Achievement Unlocker
Unlocks every achievement for all owned Steam titles.<br>
Tested on Windows Linux and Mac.<br>
Install the [runtime](https://dotnet.microsoft.com/en-us/download/dotnet/7.0) first<br>
Make sure Steam is logged in.<br>

**[Windows]**<br>
Download 'SteamAchievementUnlocker-Win64.zip' from the release section<br>
Extract the files then run 'SteamAchievementUnlocker.exe' while steam is running.<br>
If you only want to unlock for specific app IDs, just add them on the end seperated by spaces via CMD.<br>
`SteamAchievementUnlocker.exe 730 813780`<br>

**[Linux / Mac]**<br>
Download 'SteamAchievementUnlocker-Linux64.zip' or 'SteamAchievementUnlocker-Mac64.zip' from the release section<br>
Extract the files, then in the CLI browse to the directory.<br>
Run these commands;<br>
`sudo chmod +x SteamAchievementUnlocker SteamAchievementUnlockerAgent`<br>
`./SteamAchievementUnlocker`<br>
If you only want to unlock for specific app IDs, just add them on the end seperated by spaces.<br>
`./SteamAchievementUnlocker 730 813780`<br>

**[Clearing achievements]**<br>
Add the switch statement to the arguments<br>
`--clear`

_**Requires .NET 7 SDK to build from source**_<br>
