#!/usr/bin/env pwsh
<#
.SYNOPSIS
  Builds the SteamEngine .NET 8 solution, and optionally runs it.

.DESCRIPTION
  Single entry point for building MoriaEngine/SteamEngine. Works identically
  on Windows (powershell.exe or pwsh) and macOS/Linux (pwsh).

  scripts/Generated's .Generated.cs files are produced at runtime by
  GameCore's own ClassTemplateParser, not checked into source control. On a
  fresh checkout this script runs GameCore just long enough to generate them
  before attempting the full solution build - see Invoke-ScriptGeneration
  below. You never need to do this by hand.

.PARAMETER Configuration
  Debug or Release. (There is no Sane/Optimized build anymore - that was a
  pre-msbuild-conversion scheme that never carried over; the current code
  only has the standard #if DEBUG / #else split.)

.PARAMETER Run
  None (default, just build), GameCore, AuxiliaryServer, or Both.

.PARAMETER Clean
  Deletes bin/obj for every project before building. Does not touch
  steamengine.ini, steamaux.ini, logs/, or saves/.

.EXAMPLE
  ./build.ps1
.EXAMPLE
  ./build.ps1 -Configuration Release -Run Both
#>
param(
	[ValidateSet('Debug', 'Release')]
	[string]$Configuration = 'Debug',

	[ValidateSet('None', 'GameCore', 'AuxiliaryServer', 'Both')]
	[string]$Run = 'None',

	[switch]$Clean
)

$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

$StandaloneProjects = @(
	'ThirdParty/SteamEngine.ThirdParty.csproj'
	'Common/SteamEngine.Common.csproj'
	'Communication/SteamEngine.Communication.csproj'
	'GameCore/SteamEngine.GameCore.csproj'
	'AuxiliaryServer/SteamEngine.AuxiliaryServer.csproj'
	'ProcessRestarter/SteamEngine.ProcessRestarter.csproj'
	'Tests/SteamEngine.Tests.csproj'
)

function Invoke-Dotnet {
	param([string[]]$DotnetArgs)
	& dotnet @DotnetArgs
	if ($LASTEXITCODE -ne 0) {
		throw "dotnet $($DotnetArgs -join ' ') failed with exit code $LASTEXITCODE"
	}
}

function Invoke-ScriptGeneration {
	# scripts/Generated needs GameCore to have actually booted once (it writes
	# a default steamengine.ini and exits) and then a second time (it compiles
	# scripts/Generated from the .ct templates via ClassTemplateParser). No UO
	# client data or AuxiliaryServer needed - that happens later in boot.
	param([string]$Configuration)

	$dll = "GameCore/bin/$Configuration/net8.0/SteamEngine.GameCore.dll"
	$generatedMarker = 'Generated/CompiledTriggerGroups.Generated.cs'

	if ((Test-Path 'steamengine.ini') -and (Test-Path $generatedMarker)) {
		Write-Host '==> scripts/Generated already present, skipping script generation'
		return
	}

	for ($run = 1; $run -le 2; $run++) {
		if ($run -eq 2 -and (Test-Path $generatedMarker)) {
			break
		}

		Write-Host "==> Running GameCore to generate scripts ($run/2)..."
		$stdout = New-TemporaryFile
		$stderr = New-TemporaryFile
		$proc = Start-Process -FilePath 'dotnet' -ArgumentList $dll -NoNewWindow -PassThru `
			-RedirectStandardOutput $stdout -RedirectStandardError $stderr

		$deadline = (Get-Date).AddSeconds(120)
		while (-not $proc.HasExited -and (Get-Date) -lt $deadline) {
			Start-Sleep -Milliseconds 300
			if ((Get-Content $stdout -ErrorAction SilentlyContinue | Select-String 'Init done\.|Getting started' -Quiet)) {
				Start-Sleep -Seconds 1 # give it a moment in case it's about to self-exit
				break
			}
		}

		if (-not $proc.HasExited) {
			$proc | Stop-Process -Force
		}
		$proc.WaitForExit()

		Get-Content $stdout -ErrorAction SilentlyContinue | Select-Object -Last 15 | ForEach-Object { Write-Host "    $_" }
		Remove-Item $stdout, $stderr -ErrorAction SilentlyContinue
	}

	if (-not (Test-Path $generatedMarker)) {
		throw 'Script generation did not produce scripts/Generated - see GameCore output above.'
	}
}

if ($Clean) {
	Write-Host '==> Cleaning bin/obj'
	# Scoped to known project dirs only - the top-level bin/ holds checked-in
	# legacy vendor binaries (see .gitignore's */bin/ pattern), not build
	# output, and must never be touched here.
	foreach ($proj in @($StandaloneProjects + 'Generated/SteamEngine.Generated.csproj' + 'scripts/SteamEngine.Scripts.csproj')) {
		$projDir = Split-Path $proj -Parent
		Remove-Item "$projDir/bin", "$projDir/obj" -Recurse -Force -ErrorAction SilentlyContinue
	}
}

Write-Host '==> Restoring SteamEngine.sln'
Invoke-Dotnet @('restore', 'SteamEngine.sln')

Write-Host "==> Building standalone-buildable projects ($Configuration)"
foreach ($proj in $StandaloneProjects) {
	Invoke-Dotnet @('build', $proj, '--no-restore', '-c', $Configuration)
}

Invoke-ScriptGeneration -Configuration $Configuration

Write-Host "==> Building full solution ($Configuration)"
Invoke-Dotnet @('build', 'SteamEngine.sln', '--no-restore', '-c', $Configuration)

Write-Host '==> Build succeeded.'

if ($Run -eq 'None') {
	exit 0
}

$gameCoreDll = "GameCore/bin/$Configuration/net8.0/SteamEngine.GameCore.dll"
$auxDll = "AuxiliaryServer/bin/$Configuration/net8.0/SteamEngine.AuxiliaryServer.dll"
$auxProc = $null
$auxOutSub = $null
$auxErrSub = $null

try {
	if ($Run -eq 'AuxiliaryServer') {
		Write-Host '==> Starting AuxiliaryServer (foreground)'
		& dotnet $auxDll
	}
	elseif ($Run -eq 'GameCore') {
		Write-Host '==> Starting GameCore (foreground)'
		& dotnet $gameCoreDll
	}
	elseif ($Run -eq 'Both') {
		Write-Host '==> Starting AuxiliaryServer (background, logs/auxiliaryserver.console.log)'
		New-Item -ItemType Directory -Path 'logs' -Force | Out-Null

		# AuxiliaryServer's Main() has a background thread that blocks on
		# Console.ReadLine() and shuts the whole process down the moment it
		# returns. Start-Process without an explicit RedirectStandardInput
		# inherits whatever this script's own stdin happens to be - if that's
		# not a real attached terminal (or GameCore below ends up reading from
		# the same inherited stdin concurrently), ReadLine() can return
		# instantly and tear the server down before it's done anything. Give
		# it its own dedicated stdin pipe and hold the write end open (never
		# write to it, never close it) so ReadLine() blocks indefinitely,
		# independent of whatever this script's own stdin looks like.
		$auxPsi = [System.Diagnostics.ProcessStartInfo]::new('dotnet', $auxDll)
		$auxPsi.RedirectStandardInput = $true
		$auxPsi.RedirectStandardOutput = $true
		$auxPsi.RedirectStandardError = $true
		$auxPsi.UseShellExecute = $false

		$auxProc = [System.Diagnostics.Process]::Start($auxPsi)
		$script:auxStdin = $auxProc.StandardInput # kept open & unused, see comment above

		$auxLogWriter = [System.IO.StreamWriter]::new('logs/auxiliaryserver.console.log')
		$auxLogWriter.AutoFlush = $true
		$auxOutSub = Register-ObjectEvent -InputObject $auxProc -EventName OutputDataReceived -Action {
			if ($null -ne $EventArgs.Data) { $Event.MessageData.WriteLine($EventArgs.Data) }
		} -MessageData $auxLogWriter
		$auxErrSub = Register-ObjectEvent -InputObject $auxProc -EventName ErrorDataReceived -Action {
			if ($null -ne $EventArgs.Data) { $Event.MessageData.WriteLine($EventArgs.Data) }
		} -MessageData $auxLogWriter
		$auxProc.BeginOutputReadLine()
		$auxProc.BeginErrorReadLine()
		Start-Sleep -Seconds 2

		Write-Host '==> Starting GameCore (foreground)'
		& dotnet $gameCoreDll
	}
}
finally {
	if ($auxOutSub) { Unregister-Event -SourceIdentifier $auxOutSub.Name -ErrorAction SilentlyContinue }
	if ($auxErrSub) { Unregister-Event -SourceIdentifier $auxErrSub.Name -ErrorAction SilentlyContinue }
	if ($auxProc -and -not $auxProc.HasExited) {
		Write-Host '==> Stopping AuxiliaryServer'
		$auxProc | Stop-Process -Force
	}
}
