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
# Set-Location only moves PowerShell's own location - .NET APIs called directly (like
# [System.IO.StreamWriter]::new(...) below) resolve relative paths via
# [Environment]::CurrentDirectory instead, which isn't always kept in sync (observed on
# pwsh/macOS: it stayed at the user's home directory). Sync it explicitly rather than
# relying on relative paths to just happen to work.
[System.Environment]::CurrentDirectory = $PSScriptRoot

$StandaloneProjects = @(
	'ThirdParty/SteamEngine.ThirdParty.csproj'
	'Common/SteamEngine.Common.csproj'
	'Communication/SteamEngine.Communication.csproj'
	'GameCore/SteamEngine.GameCore.csproj'
	'AuxiliaryServer/SteamEngine.AuxiliaryServer.csproj'
	'ProcessRestarter/SteamEngine.ProcessRestarter.csproj'
	# Tests/SteamEngine.Tests.csproj is NOT standalone anymore (issue #358 gave it a
	# ProjectReference on scripts/SteamEngine.Scripts.csproj, to test StaticMemberSaver
	# against a real [HasSavedMembers] type) - building it this early, before
	# Invoke-ScriptGeneration has produced Generated/*.Generated.cs, fails. The final
	# "Building full solution" step below already builds it, by which point script
	# generation has run.
)

function Invoke-Dotnet {
	param([string[]]$DotnetArgs)
	& dotnet @DotnetArgs
	if ($LASTEXITCODE -ne 0) {
		throw "dotnet $($DotnetArgs -join ' ') failed with exit code $LASTEXITCODE"
	}
}

function Set-MulPathToCheckedInUoFiles {
	# steamengine.ini defaults mulPath to the literal string "muls" (GameCore/Globals.cs),
	# a folder nobody actually has - real UO client data lives in the repo's own uo-files/
	# (one level up from here). Without this, every fresh steamengine.ini silently boots
	# against missing mul files (CRITICAL: Unable to locate tiledata.mul, etc.) until
	# someone notices and hand-edits the ini - which doesn't help the next person who
	# generates a fresh one, since the file is gitignored by design (real per-install
	# config, not something to commit). Rewrite it here instead, every time, so a fresh
	# clone just works.
	if (-not (Test-Path 'steamengine.ini')) {
		return
	}
	$uoFiles = Join-Path $PSScriptRoot '..' 'uo-files'
	if (-not (Test-Path $uoFiles)) {
		return
	}
	$ini = Get-Content 'steamengine.ini' -Raw
	$patched = $ini -replace '(?m)^mulPath\s*=\s*muls\s*$', 'mulPath = ../uo-files'
	if ($patched -ne $ini) {
		Write-Host '==> Pointing mulPath at ../uo-files in steamengine.ini'
		Set-Content 'steamengine.ini' $patched -NoNewline
	}
}

function Remove-OrphanedGeneratedFiles {
	# *.Generated.cs files are gitignored and sit beside their .ct template, so moving or
	# deleting a template leaves the old output behind on every machine that had built it,
	# where it collides with the new one (CS0102/CS0111 on the class). Hit moving Potion.ct
	# in #928: the author's copy had been deleted by hand, so only other checkouts failed.
	# The generator trims spaces and underscores off the name, and 'ItemLockPlugin .ct'
	# does have a space, so compare trimmed names.
	$templates = @{}
	Get-ChildItem -Path scripts -Filter '*.ct' -Recurse -File | ForEach-Object {
		$templates[(Join-Path $_.DirectoryName $_.BaseName.Trim('_', ' '))] = $true
	}
	Get-ChildItem -Path scripts -Filter '*.Generated.cs' -Recurse -File |
		Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' } |
		ForEach-Object {
			$template = Join-Path $_.DirectoryName ($_.Name -replace '\.Generated\.cs$', '')
			if (-not $templates.ContainsKey($template)) {
				Write-Host "==> Removing $($_.FullName): its template is gone"
				Remove-Item $_.FullName
			}
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

	Set-MulPathToCheckedInUoFiles

	# Real bug found 2026-10-14: checking only whether the marker file EXISTS (not
	# whether it's still current) meant editing a .ct template - e.g. adding a new
	# persisted field to Player.ct - left every *.Generated.cs file on disk silently
	# stale, with this function skipping regeneration entirely on every subsequent
	# build until something else happened to force a fresh one. Compare timestamps
	# instead: if any .ct template anywhere in the repo is newer than the marker, the
	# generated output no longer reflects the current templates and must be redone.
	#
	# Real bug found 2026-10-15: that fix only covered .ct -> CompiledTriggerGroups.
	# Generated/CompiledScriptHolders.Generated.cs is a SEPARATE generated output,
	# produced from reflection over [SteamFunction]-attributed plain C# methods under
	# scripts/ (GameCore/CompiledScripts/CompiledScriptHolder.cs's
	# CompiledScriptHolderGenerator) - not from .ct templates at all. Renaming/adding a
	# [SteamFunction] method (e.g. #190-193's Kill->InstaKill rename) doesn't touch any
	# .ct file, so the old check alone would report "up to date" and skip regeneration,
	# leaving a stale CompiledScriptHolders.Generated.cs (referencing an OLDER method
	# name from whatever session last ran the generator) to fail the final solution
	# build with a confusing CS0117 on an unrelated-looking class. Also compare against
	# the newest plain .cs source file (excluding the *.Generated.cs outputs
	# themselves, and bin/obj) anywhere in the repo.
	$newestTemplate = Get-ChildItem -Path . -Filter '*.ct' -Recurse -File -ErrorAction SilentlyContinue |
		Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
	$newestSource = Get-ChildItem -Path . -Filter '*.cs' -Recurse -File -ErrorAction SilentlyContinue |
		Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' -and $_.Name -notlike '*.Generated.cs' } |
		Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
	$newestInput = @($newestTemplate, $newestSource) | Where-Object { $_ } |
		Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
	function Test-GeneratedUpToDate {
		if (-not (Test-Path $generatedMarker)) {
			return $false
		}
		if (-not $newestInput) {
			return $true
		}
		return $newestInput.LastWriteTimeUtc -le (Get-Item $generatedMarker).LastWriteTimeUtc
	}

	if ((Test-Path 'steamengine.ini') -and (Test-GeneratedUpToDate)) {
		Write-Host '==> scripts/Generated already present and up to date, skipping script generation'
		return
	}
	if ((Test-Path $generatedMarker) -and -not (Test-GeneratedUpToDate)) {
		Write-Host "==> scripts/Generated is stale ($($newestInput.FullName) was edited after the last generation) - regenerating"
	}

	for ($run = 1; $run -le 2; $run++) {
		if ($run -eq 2 -and (Test-GeneratedUpToDate)) {
			break
		}

		Write-Host "==> Running GameCore to generate scripts ($run/2)..."
		$stdout = New-TemporaryFile
		$stderr = New-TemporaryFile
		$proc = Start-Process -FilePath 'dotnet' -ArgumentList $dll -NoNewWindow -PassThru `
			-WorkingDirectory $PSScriptRoot `
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

		Write-Host "    exit code: $($proc.ExitCode)"
		Get-Content $stdout -ErrorAction SilentlyContinue | Select-Object -Last 15 | ForEach-Object { Write-Host "    [out] $_" }
		Get-Content $stderr -ErrorAction SilentlyContinue | Select-Object -Last 30 | ForEach-Object { Write-Host "    [err] $_" }
		Remove-Item $stdout, $stderr -ErrorAction SilentlyContinue

		if ($run -eq 1) {
			# This run is the one that actually wrote steamengine.ini for the first time
			# on a fresh clone (see Set-MulPathToCheckedInUoFiles) - patch it before run 2,
			# which is the one that needs real mul files to fully boot cleanly.
			Set-MulPathToCheckedInUoFiles
		}
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
Remove-OrphanedGeneratedFiles
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
