Import-Module -Name ".\lib\Invoke-MsBuild.psm1"

Invoke-MsBuild -Path ".\SteamEngine.sln" -AutoLaunchBuildLog -MsBuildParameters "/target:SteamEngine_AuxiliaryServer /property:Configuration=Release" 

.\build\Release\SteamEngine.AuxiliaryServer.exe