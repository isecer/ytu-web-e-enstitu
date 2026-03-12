@echo off
setlocal

cd /d "%~dp0"

echo ==========================================
echo Line count analysis started...
echo Target: %cd%
echo ==========================================
echo.

cloc . --timeout=0

echo.
echo ==========================================
echo Finished.
echo ==========================================
pause