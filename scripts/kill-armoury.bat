@echo off
REM Stop Armoury Crate + its background audio agent (handy for testing without it,
REM before you uninstall it). Self-elevating. To bring it back, just launch Armoury
REM Crate again or reboot.
net session >nul 2>&1
if %errorlevel% equ 0 goto admin
powershell -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
exit /b

:admin
sc stop ArmouryCrateService >nul 2>&1
timeout /t 1 >nul
taskkill /F /IM ArmouryAudioAgent.exe >nul 2>&1
taskkill /F /IM ArmouryCrate.exe >nul 2>&1
taskkill /F /IM "ArmouryCrate.Service.exe" >nul 2>&1
taskkill /F /IM "ArmouryCrate.UserSessionHelper.exe" >nul 2>&1
taskkill /F /IM ArmouryHtmlDebugServer.exe >nul 2>&1
taskkill /F /IM ArmourySocketServer.exe >nul 2>&1
taskkill /F /IM ArmourySwAgent.exe >nul 2>&1
taskkill /F /IM asus_framework.exe >nul 2>&1
echo Done.
pause
