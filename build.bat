@echo off
REM Build PeltaTool.exe with the .NET Framework C# compiler (always present on Windows).
set "CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"
"%CSC%" /nologo /target:winexe /out:"%~dp0PeltaTool.exe" /r:System.Windows.Forms.dll /r:System.Drawing.dll "%~dp0src\PeltaTool.cs"
if exist "%~dp0PeltaTool.exe" (echo Build OK -^> PeltaTool.exe) else (echo Build FAILED)
pause
