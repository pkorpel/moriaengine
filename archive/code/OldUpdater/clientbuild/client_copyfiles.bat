..\bin\rm.exe -frd clientbuild\bin
mkdir bin
mkdir bin\Codecs\
mkdir bin\Formats\
mkdir release\

copy ..\bin\7z.exe bin\
copy ..\bin\Codecs\Rar29.dll bin\Codecs\
copy ..\bin\Formats\rar.dll bin\Formats\
copy ..\bin\rm.exe bin\
copy ..\bin\mv.exe bin\
copy ..\bin\jpatch.exe bin\
copy ..\bin\jpatch_readme.htm bin\
copy ..\bin\msvcr71.dll bin\
copy ..\background.* bin\
copy ..\okbutton.* bin\
copy ..\logo.txt bin\
copy ..\icon.ico .

copy ..\common.py .
copy ..\client.py .
copy ..\downloader.py .
copy ..\text_ui.py .
copy ..\tk_ui.py .

copy ..\client_readme.txt release\readme.txt
copy ..\client_changelog.txt release\changelog.txt