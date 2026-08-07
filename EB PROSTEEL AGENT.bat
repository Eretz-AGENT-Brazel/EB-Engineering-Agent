@echo off
title EB PROSTEEL AGENT
cd /d "%~dp0"

echo ================================================
echo            E B   P R O S T E E L   A G E N T
echo            Eretz Barzel - Steel Modeling AI
echo ================================================
echo.
echo NOTE: open AutoCAD 2015 + ProSteel YOURSELF (manually), then in the
echo       console create a project and press "Connect to AutoCAD + ProSteel".
echo.
echo [1/2] Starting the Agent Console server...
set PYEXE=C:\Users\User\AppData\Local\Programs\Python\Python312\python.exe
if not exist "%PYEXE%" set PYEXE=python
start "EB-Console-Server" /min cmd /c ""%PYEXE%" "%~dp0app\console.py""
timeout /t 2 /nobreak >nul

echo [2/2] Opening the workspace...
set URL=http://localhost:8788
set EDGE=C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe
set CHROME=C:\Program Files\Google\Chrome\Application\chrome.exe
if exist "%CHROME%" (
  start "" "%CHROME%" --app=%URL% --window-size=1180,820
) else (
  start "" "%EDGE%" --app=%URL% --window-size=1180,820
)

echo.
echo Ready. The console is open. Open AutoCAD+ProSteel manually, then press Connect.
echo (You may minimize this window - it runs the console server.)
timeout /t 4 /nobreak >nul
