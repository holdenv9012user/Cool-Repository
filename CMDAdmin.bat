@echo off
:: Run command prompt as administrator
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo Requesting administrative privileges...
    powershell -Command "Start-Process cmd.exe -Verb RunAs"
    exit /b
)
echo Running elevated command prompt...
cmd.exe
exit /b
