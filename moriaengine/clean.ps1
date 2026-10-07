Import-Module -Name ".\lib\Invoke-MsBuild.psm1"

Invoke-MsBuild -Path ".\SteamEngine.sln" -AutoLaunchBuildLog -MsBuildParameters "/target:Clean /property:Configuration=Debug" 

Invoke-MsBuild -Path ".\SteamEngine.sln" -AutoLaunchBuildLog -MsBuildParameters "/target:Clean /property:Configuration=Release" 