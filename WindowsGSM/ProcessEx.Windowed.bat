:: ProcessEx.Windowed.bat
:: A batchfile to start a process in windowed mode and obtain the process id
:: Github: https://github.com/WindowsGSM/WindowsGSM
:: Create: 2/18/2022

@echo off
set pid=-1

:: Create process with WMIC and find the ProcessId
for /f "tokens=2 delims==; " %%a in ('wmic process call create "%~1"^,"%~2." ^| find "ProcessId"') do set pid=%%a

:: Echo the ProcessId, WindowsGSM will get the ProcessId from here
echo %pid%
