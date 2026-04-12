@echo off
echo ========================================
echo  RESET DATABASE - HEALTHCARE SYSTEM
echo ========================================
echo.
echo Dang reset database va load data moi...
echo.

dotnet run -- --reset-db

echo.
echo ========================================
echo  HOAN THANH!
echo ========================================
echo.
pause
