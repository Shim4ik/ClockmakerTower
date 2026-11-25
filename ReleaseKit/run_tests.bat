@echo off
setlocal
REM Automatic performance tests of the release build (resolutions and graphics APIs).
REM Each run takes about 75 seconds (10 s warm-up + 60 s recording) and closes the game by itself.
cd /d "%~dp0"
set EXE=ClockmakerTower.exe
set OUT=test_results
set DATA=%USERPROFILE%\AppData\LocalLow\ClockmakerStudio\Clockmaker Tower

if not exist "%EXE%" (
  echo %EXE% not found. Run this script from the build folder.
  pause
  exit /b 1
)
if not exist "%OUT%" mkdir "%OUT%"

echo ===== 1/6: 1280x720 windowed, Direct3D 12 =====
start "" /wait "%EXE%" -autobench -screen-width 1280 -screen-height 720 -screen-fullscreen 0
echo ===== 2/6: 1920x1080 windowed, Direct3D 12 =====
start "" /wait "%EXE%" -autobench -screen-width 1920 -screen-height 1080 -screen-fullscreen 0
echo ===== 3/6: 2560x1440 fullscreen, Direct3D 12 =====
start "" /wait "%EXE%" -autobench -screen-width 2560 -screen-height 1440 -screen-fullscreen 1
echo ===== 4/6: 1280x720 windowed, Direct3D 11 =====
start "" /wait "%EXE%" -autobench -force-d3d11 -screen-width 1280 -screen-height 720 -screen-fullscreen 0
echo ===== 5/6: 1920x1080 windowed, Direct3D 11 =====
start "" /wait "%EXE%" -autobench -force-d3d11 -screen-width 1920 -screen-height 1080 -screen-fullscreen 0
echo ===== 6/6: 2560x1440 fullscreen, Direct3D 11 =====
start "" /wait "%EXE%" -autobench -force-d3d11 -screen-width 2560 -screen-height 1440 -screen-fullscreen 1

copy /Y "%DATA%\perf_Release_*_summary.txt" "%OUT%\" >nul
echo.
echo Done. Result files (*_summary.txt) are in the folder: %OUT%
pause
