Import-Module -Name ".\lib\Invoke-MsBuild.psm1"

Invoke-MsBuild -Path ".\SteamEngine.sln" -AutoLaunchBuildLog -MsBuildParameters "/target:SteamEngine_RemoteConsole /property:Configuration=Debug" 

.\build\Debug\SteamEngine.RemoteConsole.exe