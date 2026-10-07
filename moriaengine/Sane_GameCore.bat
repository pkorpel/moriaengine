@echo off

bin\nant -D:debug=false -D:cmdLineParams=/debug+ -D:defineSymbols="TRACE,SANE,MSWIN" -buildfile:distrib/nant/default.build buildCore

bin\Sane.SteamEngine.GameCore.exe

