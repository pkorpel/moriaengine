cd clientbuild
call client_copyfiles.bat

..\bin\rm -fr dist

setup.py py2exe

mkdir dist\library

echo "let's repack the library:"
echo "unzipping the created library"
..\bin\7z x dist\library.zip -odist\library\

echo "re-zipping the created library"
cd dist\library\
..\..\..\bin\7z a -tzip ..\..\release\bin\library.zip * -r -mx=9
cd ..\..


..\bin\rm -fr dist\library
..\bin\rm -fr dist\library.zip

..\bin\mv -f dist\* release\bin
..\bin\mv -f bin\* release\bin

echo "now let's pack the release into a distribution-ready archive"

cd release\
..\..\bin\rm -fr temp
..\..\bin\rm -fr backup
..\..\bin\rm -fr AutoUpdaterSetup.exe
..\..\bin\rm -fr bin\AutoUpdater.exe.log
..\..\bin\rm -fr bin\AutoUpdater_text.exe.log
..\..\bin\rm -fr config.xml


..\..\bin\7z u AutoUpdaterSetup.exe *.* -sfx..\..\bin\7zC.sfx -mx=9 -ms=on -x!*cvs* -r
cd ..\..

pause