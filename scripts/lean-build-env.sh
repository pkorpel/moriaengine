# Sourced by the test scripts and run-local.sh (#1212): no idle build processes left behind.
#
# A build normally leaves MSBuild worker nodes (node reuse) and a compiler server (VBCSCompiler) running for later
# builds. Every test run builds, and every GameCore boot compiles scripts/ with `dotnet build` too
# (Common/MsBuildLauncher.cs; the servers a test boots inherit this environment), so a long session piled up dozens of
# idle workers and a compiler server of up to 1.8 GB: about 3.5 GB on a 5 GB box, and runs stopped for low memory.
# Each variable is set only if unset or empty, so a run can choose otherwise.
: "${MSBUILDDISABLENODEREUSE:=1}"          # worker nodes exit after each build
: "${DOTNET_CLI_USE_MSBUILD_SERVER:=0}"    # no MSBuild server process (opt-in on SDK 8 anyway)
# Workstation GC for the compiler and the build's tasks (1.7 -> 0.5 GB measured). It also fixes the GC mode of the
# servers and the test host these scripts start, over their runtimeconfig: to measure server GC, export DOTNET_gcServer=1.
: "${DOTNET_gcServer:=0}"
export MSBUILDDISABLENODEREUSE DOTNET_CLI_USE_MSBUILD_SERVER DOTNET_gcServer

# The compiler server is left on during a run: every server boot compiles scripts/, and without it each boot ran a cold
# compiler (28-30 s to ready, against 22-27 s with a warm one). The scripts call this when they're done building. --vbcscompiler only: the full `dotnet build-server shutdown` waits forever on MSBuild nodes here.
lean_build_shutdown_compiler() {
	timeout 60 dotnet build-server shutdown --vbcscompiler >/dev/null 2>&1 || true
}
