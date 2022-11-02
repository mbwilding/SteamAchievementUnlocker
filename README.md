# Steam Achievement Unlocker
Unlocks every achievement for all owned Steam titles.<br>
Tested on Windows and Linux.

**[Windows]**<br>
Install [this](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-6.0.10-windows-x64-installer) first.<br>
Download 'SteamAchievementUnlocker-Win64.zip' from the release section<br>
Make sure Steam is logged in.<br>
Extract the files then run 'SteamAchievementUnlocker.exe' while steam is running.<br>
If you only want to unlock for specific app IDs, just add them on the end seperated by spaces via CMD.<br>
`SteamAchievementUnlocker.exe 730 813780`<br>

**[Linux]**<br>
Install [this](https://docs.microsoft.com/dotnet/core/install/linux) first.<br>
Download 'SteamAchievementUnlocker-Linux64.zip' from the release section<br>
Make sure Steam is logged in.<br>
Extract the files, then in the CLI browse to the directory.<br>
Run these commands;<br>
`sudo chmod +x SteamAchievementUnlocker SteamAchievementUnlockerAgent`<br>
`./SteamAchievementUnlocker`<br>
If you only want to unlock for specific app IDs, just add them on the end seperated by spaces.<br>
`./SteamAchievementUnlocker 730 813780`<br>

**[Clearing achievements]**<br>
Add the switch statement to the arguments<br>
`-clear`

_**Requires .NET 6 SDK to build from source**_<br>