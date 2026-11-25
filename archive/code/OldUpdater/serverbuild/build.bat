@echo off
del /Q dist.final\bin\library.zip
serversetup.py py2exe


copy dist\library.zip server.dist\library.zip
copy dist\servercore.exe server.dist\servercore.exe
copy serverprotocol.py server.dist\serverprotocol.py
copy serverscript.py server.dist\serverscript.py

pause