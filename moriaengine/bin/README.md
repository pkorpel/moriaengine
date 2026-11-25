# Files in `bin/`

This directory contains one checked-in legacy tool:

- `grammatica-1.4.jar` is the Grammatica parser generator used by
  `GameCore/LScript/generate.bat` to regenerate the LScript parser sources.

The legacy image assets are preserved in [`archive/images/`](../../archive/images/).

Other dependencies are managed by project files or stored with their source.
For example, `scripts/SteamEngine.Scripts.csproj` references MySql.Data through
NuGet; its package DLL is not stored in this directory.
