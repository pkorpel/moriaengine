# Assembly dependency diagram

The projects in `SteamEngine.sln`, what each holds and what it references. For the role of
each project see [Architecture overview](../../current/01-overview/architecture-overview.md#top-level-projects-and-what-theyre-for).

```mermaid
flowchart TD
    common["<b>Common</b><br/>Logger, ini files, Tools,<br/>localisation, MsBuildLauncher"]
    comm["<b>Communication</b><br/>AsyncCore, TCP and named-pipe<br/>connections, packets, buffers"]
    third["<b>ThirdParty</b><br/>client encryption<br/>(Twofish, UO login),<br/>Grammatica parser runtime"]
    abstr["<b>Generators.Abstractions</b><br/>attributes the generators read"]
    gen["<b>Generators</b><br/>Roslyn source generators"]
    core["<b>GameCore</b> (exe)<br/>Thing/ThingDef, Plugin, TagHolder,<br/>timers, regions, LScript, persistence,<br/>networking, CompilerInvoker"]
    scripts["<b>Scripts</b> (SteamEngine.CompiledScripts)<br/>items, characters, AI, skills,<br/>spells, dialogs, gameplay systems"]
    aux["<b>AuxiliaryServer</b> (exe)<br/>LoginServer, ConsoleServer,<br/>SEGameServerServer"]
    rc["<b>RemoteConsole</b> (WinExe, Windows only)"]
    ssc["<b>SphereShardConverter</b> (exe)<br/>one-time Sphere content converter"]
    tests["<b>Tests</b> (xUnit)"]
    gtests["<b>Generators.Tests</b> (xUnit)"]

    comm --> common
    gen --> abstr
    core --> common
    core --> comm
    core --> third
    core --> abstr
    core -. "analyzer" .-> gen
    scripts --> core
    scripts --> abstr
    scripts -. "analyzer" .-> gen
    aux --> common
    aux --> comm
    rc --> common
    rc --> comm
    ssc --> core
    ssc --> scripts
    tests --> core
    tests --> third
    tests --> scripts
    gtests --> gen
    gtests --> abstr
    gtests --> scripts
    core == "builds and loads at boot" ==> scripts
```

Arrows point from a project to what it references. Transitive references (for example
Scripts → Common) are left out.

## Notes

- **Dotted arrows** are analyzer references (`OutputItemType="Analyzer"`): the generators run
  inside the compile of GameCore and Scripts and add code there. Nothing is written to disk;
  see [Scripting & code generation pipeline](../../current/02-engine-core/scripting-pipeline.md).
- **The thick arrow** is not a project reference. At boot GameCore builds
  `scripts/SteamEngine.Scripts.csproj` with MSBuild (or reuses the last build if nothing
  changed) and loads the resulting assembly (`GameCore/CompiledScripts/CompilerInvoker.cs`).
- **NuGet packages:** Scripts uses SharpZipLib and System.Management (`BackupsManager`) and
  MySql.Data; Generators uses Microsoft.CodeAnalysis.CSharp; the test projects use xUnit.
- **Not in the solution:** `tools/perf/stmbench` (a standalone benchmark) and the archived
  code under `archive/code/` at the repo root.
