@echo off
title EB AI - Mission Control
cd /d "%~dp0"
echo Starting EB AI - Mission Control...
start "EB Mission Control" /min cmd /c "py server.py 2>nul || python server.py 2>nul || ""%LOCALAPPDATA%\Programs\Python\Python312\python.exe"" server.py"
timeout /t 2 >nul
start "" http://localhost:8190
exit
