@echo off

bin\nant runConverter -D:debug=true -D:defineSymbols="TRACE,DEBUG,MSWIN" -buildfile:distrib/nant/default.build