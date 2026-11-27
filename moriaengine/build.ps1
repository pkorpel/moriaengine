#!/usr/bin/env pwsh
<#
.SYNOPSIS
  Builds the SteamEngine solution, and optionally runs it.

.DESCRIPTION
  Single entry point for building MoriaEngine/SteamEngine. Works identically
  on Windows (powershell.exe or pwsh) and macOS/Linux (pwsh).

  Generated code comes from the Roslyn source generators during the normal
  build. On a fresh checkout without steamengine.ini, this script runs GameCore
  once afterwards so it writes its default ini - see Initialize-SteamEngineIni
  below. You never need to do this by hand.

  Before building, it deletes what the old code generators left in older clones
  (#1592): the *.Generated.cs files under scripts/, which break the build, and
  the dead Generated/ folder.

.PARAMETER Configuration
  Debug or Release. (There is no Sane/Optimized build anymore - that was a
  pre-msbuild-conversion scheme that never carried over; the current code
  only has the standard #if DEBUG / #else split.)

.PARAMETER Run
  None (default, just build), GameCore, AuxiliaryServer, or Both.

.PARAMETER Clean
  Runs dotnet clean (this configuration) on GameCore's and AuxiliaryServer's
  projects and everything they reference, before building. Does not touch
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
# Set-Location only moves PowerShell's own location - .NET APIs called directly (like
# [System.IO.StreamWriter]::new(...) below) resolve relative paths via
# [Environment]::CurrentDirectory instead, which isn't always kept in sync (observed on
# pwsh/macOS: it stayed at the user's home directory). Sync it explicitly rather than
# relying on relative paths to just happen to work.
[System.Environment]::CurrentDirectory = $PSScriptRoot

# dotnet clean follows project references: scripts covers GameCore, Common,
# Communication, ThirdParty and the generators.
$CleanProjects = @(
	'scripts/SteamEngine.Scripts.csproj'
	'AuxiliaryServer/SteamEngine.AuxiliaryServer.csproj'
)

# #1551: the target framework (bin/<config>/<tfm>) comes from Directory.Build.props, the one place it is set.
$TargetFramework = ([xml](Get-Content (Join-Path $PSScriptRoot 'Directory.Build.props'))).Project.PropertyGroup.SteamEngineTargetFramework
if (-not $TargetFramework) {
	throw 'Directory.Build.props sets no SteamEngineTargetFramework.'
}

function Invoke-Dotnet {
	param([string[]]$DotnetArgs)
	& dotnet @DotnetArgs
	if ($LASTEXITCODE -ne 0) {
		throw "dotnet $($DotnetArgs -join ' ') failed with exit code $LASTEXITCODE"
	}
}

function Initialize-SteamEngineIni {
	# A first boot of GameCore writes a default steamengine.ini and exits by itself
	# ("Getting started"); the full-boot tests and run-local.sh need that file.
	# No UO client data or AuxiliaryServer needed for this.
	param([string]$Configuration)

	if (Test-Path 'steamengine.ini') {
		return
	}

	Write-Host '==> No steamengine.ini yet, running GameCore once to write the default one...'
	$dll = "GameCore/bin/$Configuration/$TargetFramework/SteamEngine.GameCore.dll"
	$stdout = New-TemporaryFile
	$stderr = New-TemporaryFile
	try {
		$proc = Start-Process -FilePath 'dotnet' -ArgumentList $dll -NoNewWindow -PassThru `
			-WorkingDirectory $PSScriptRoot `
			-RedirectStandardOutput $stdout -RedirectStandardError $stderr

		# Wait for GameCore to exit on its own, so the ini is complete. A first boot
		# that doesn't exit is a failure; its ini may be half written, so remove it.
		$exited = $proc.WaitForExit(120000)
		if (-not $exited) {
			$proc | Stop-Process -Force
			$proc.WaitForExit()
			Remove-Item 'steamengine.ini' -ErrorAction SilentlyContinue
		}
		if (-not $exited -or -not (Test-Path 'steamengine.ini')) {
			Get-Content $stdout -ErrorAction SilentlyContinue | Select-Object -Last 15 | ForEach-Object { Write-Host "    [out] $_" }
			Get-Content $stderr -ErrorAction SilentlyContinue | Select-Object -Last 30 | ForEach-Object { Write-Host "    [err] $_" }
			$why = if ($exited) { 'exited without writing steamengine.ini' } else { "didn't exit within 120 s" }
			throw "GameCore's first boot $why - see its output above."
		}
	} finally {
		Remove-Item $stdout, $stderr -ErrorAction SilentlyContinue
	}
}

function Remove-StaleGeneratedCode {
	# #1592: the .ct template generator wrote a *.Generated.cs beside each script. Its removal
	# (11990dc3) also dropped the cleanup that deleted them here, and they are gitignored, so an
	# older clone keeps them after a pull. The Roslyn generators then make every member they
	# hold a second time (CS0111, SEGEN905/SEGEN012: thousands of errors). No tracked file has
	# that name. Generated/ is the dead output of the runtime pass removed in #1563.
	$stale = @(Get-ChildItem -Path 'scripts' -Filter '*.Generated.cs' -Recurse -File)
	foreach ($file in $stale) {
		Remove-Item -LiteralPath $file.FullName
	}
	if ($stale.Count -gt 0) {
		Write-Host "==> Removed $($stale.Count) leftover *.Generated.cs file(s) from scripts/ (old code generator, #1592)"
	}
	if (Test-Path 'Generated') {
		Remove-Item -LiteralPath 'Generated' -Recurse -Force
		Write-Host '==> Removed the leftover Generated/ folder (old code generator, #1592)'
	}
}

Remove-StaleGeneratedCode

if ($Clean) {
	Write-Host "==> Cleaning ($Configuration)"
	foreach ($proj in $CleanProjects) {
		Invoke-Dotnet @('clean', $proj, '-c', $Configuration, '--nologo', '-v', 'quiet')
	}
}

Write-Host "==> Building full solution ($Configuration)"
Invoke-Dotnet @('build', 'SteamEngine.sln', '-c', $Configuration)

Initialize-SteamEngineIni -Configuration $Configuration

Write-Host '==> Build succeeded.'

if ($Run -eq 'None') {
	exit 0
}

$gameCoreDll = "GameCore/bin/$Configuration/$TargetFramework/SteamEngine.GameCore.dll"
$auxDll = "AuxiliaryServer/bin/$Configuration/$TargetFramework/SteamEngine.AuxiliaryServer.dll"
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
		$logsDir = Join-Path $PSScriptRoot 'logs'
		New-Item -ItemType Directory -Path $logsDir -Force | Out-Null

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
		$auxPsi.WorkingDirectory = $PSScriptRoot
		$auxPsi.RedirectStandardInput = $true
		$auxPsi.RedirectStandardOutput = $true
		$auxPsi.RedirectStandardError = $true
		$auxPsi.UseShellExecute = $false

		$auxProc = [System.Diagnostics.Process]::Start($auxPsi)
		$script:auxStdin = $auxProc.StandardInput # kept open & unused, see comment above

		$auxLogWriter = [System.IO.StreamWriter]::new((Join-Path $logsDir 'auxiliaryserver.console.log'))
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
