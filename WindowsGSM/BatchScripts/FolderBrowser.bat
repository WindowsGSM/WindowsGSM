:: FolderBrowser.bat
:: Launches a folder browser and obtain the folder path
:: Source: https://stackoverflow.com/a/15885133/1683264

@echo off
setlocal
set folder=%~1

set "psCommand="(new-object -COM 'Shell.Application').BrowseForFolder(0,'Please choose a folder.',0,'%folder%').self.path""

for /f "usebackq delims=" %%I in (`powershell %psCommand%`) do set "folder=%%I"

setlocal enabledelayedexpansion
echo !folder!
endlocal
