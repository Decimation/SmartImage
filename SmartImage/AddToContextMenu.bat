@echo off

SET "SMARTIMAGE=C:\Library\SmartImage.exe"
SET COMMAND=%SMARTIMAGE% \"%%1\"

%SystemRoot%\System32\reg.exe ADD HKEY_CLASSES_ROOT\*\shell\SmartImage\command /ve /d "%COMMAND%" /f >nul

pause