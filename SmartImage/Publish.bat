@echo off

dotnet publish -c Release -r win10-x64 --self-contained true

CHOICE /N /C:YN /M "Copy to Desktop (Y/N)"%1
IF ERRORLEVEL ==2 GOTO NCOPY
IF ERRORLEVEL ==1 GOTO YCOPY
GOTO END

:NCOPY
ECHO Not copying executable to Desktop
GOTO END

:YCOPY
ECHO Copying to %userprofile%\Desktop\
copy bin\Release\net5.0\win10-x64\publish\SmartImage.exe %userprofile%\Desktop\ /Y
GOTO END

:END
ping localhost >nul