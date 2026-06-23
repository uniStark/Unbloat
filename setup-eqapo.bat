@echo off
REM One-time setup: copy EQ profiles into EqualizerAPO's config folder and grant
REM your account write access so PeltaTool can switch profiles without admin.
net session >nul 2>&1
if %errorlevel% equ 0 goto admin
echo Requesting administrator privileges...
echo Please click YES on the UAC popup.
powershell -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
exit /b

:admin
set "SRC=%~dp0eq"
set "DST=C:\Program Files\EqualizerAPO\config"
if not exist "%DST%" (echo EqualizerAPO not found at "%DST%". Install it first. & pause & exit /b)
echo Copying EQ profiles...
copy /Y "%SRC%\pelta-fps.txt" "%DST%\" >nul
copy /Y "%SRC%\pelta-default.txt" "%DST%\" >nul
copy /Y "%SRC%\t60.txt" "%DST%\" >nul
copy /Y "%SRC%\config.txt" "%DST%\config.txt" >nul
echo Granting write permission to %USERNAME% ...
icacls "%DST%" /grant "%USERNAME%:(OI)(CI)M" >nul
echo.
echo Done. Now run PeltaTool.exe.
echo.
pause
