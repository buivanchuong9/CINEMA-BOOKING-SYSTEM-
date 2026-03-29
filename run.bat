@echo off
setlocal

chcp 65001 >nul
title BeCinema - Auto Run Script

:: 0. Kiem tra quyen Admin (Bat buoc de start SQL Service)
>nul 2>&1 "%SYSTEMROOT%\system32\cacls.exe" "%SYSTEMROOT%\system32\config\system"
if errorlevel 1 (
    echo [INFO] Dang yeu cau quyen (Admin) de Start Service...
    echo Set UAC = CreateObject^("Shell.Application"^) > "%temp%\getadmin.vbs"
    echo UAC.ShellExecute "cmd.exe", "/c ""%~s0"" %*", "", "runas", 1 >> "%temp%\getadmin.vbs"
    "%temp%\getadmin.vbs"
    del "%temp%\getadmin.vbs"
    exit /B
)

pushd "%~dp0"

echo ===================================================
echo   BE-CINEMA AUTO SETUP
echo ===================================================
echo.

:: 1. Kiem tra .NET 8
echo [1/5] Kiem tra .NET 8 SDK...
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo [LOI] Khong tim thay .NET 8 SDK!
    start "" https://dotnet.microsoft.com/en-us/download/dotnet/8.0
    pause
    exit /b 1
)
dotnet --version | findstr "8." >nul 2>&1
if errorlevel 1 (
    echo [LOI] Can phai cai dat .NET 8 SDK!
    start "" https://dotnet.microsoft.com/en-us/download/dotnet/8.0
    pause
    exit /b 1
)
echo =^> .NET 8 OK!
echo.

:: 2. Kiem tra SQL Server
echo [2/5] Kiem tra SQL Server...
set "DB_SERVER="

sc query "MSSQLSERVER" >nul 2>&1
if not errorlevel 1 (
    net start "MSSQLSERVER" >nul 2>&1
    set "DB_SERVER=localhost"
)

if "%DB_SERVER%"=="" (
    sc query "MSSQL$SQLEXPRESS" >nul 2>&1
    if not errorlevel 1 (
        net start "MSSQL$SQLEXPRESS" >nul 2>&1
        set "DB_SERVER=localhost\SQLEXPRESS"
    )
)

if "%DB_SERVER%"=="" (
    sqllocaldb create MSSQLLocalDB -s >nul 2>&1
    if not errorlevel 1 (
        sqllocaldb start MSSQLLocalDB >nul 2>&1
        set "DB_SERVER=(localdb)\mssqllocaldb"
    )
)

if "%DB_SERVER%"=="" (
    echo [LOI] Khong the tim thay hoac cai dat SQL Server / LocalDB.
    start "" https://www.microsoft.com/en-us/sql-server/sql-server-downloads
    pause
    exit /b 1
)

echo =^> SQL Server / LocalDB OK! (%DB_SERVER%)
echo.

:: 3. Kiem tra Redis
echo [3/5] Kiem tra Redis Server...
netstat -an | findstr ":6379" | findstr "LISTENING" >nul 2>&1
if errorlevel 1 (
    if not exist "%~dp0.redis\redis-server.exe" (
        echo   - Dang tai Redis Portable tu Github...
        powershell -Command "[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; Invoke-WebRequest -Uri 'https://github.com/tporadowski/redis/releases/download/v5.0.14.1/Redis-x64-5.0.14.1.zip' -OutFile '.redis.zip'"
        powershell -Command "Expand-Archive -Path '.redis.zip' -DestinationPath '.redis' -Force"
        del .redis.zip
    )
    start "Redis Server" /MIN "%~dp0.redis\redis-server.exe"
    timeout /t 2 >nul
)
echo =^> Redis Server OK! (Port 6379)
echo.

:: 4. Database Migration 
echo [4/5] Khoi tao Database (CinemaBooking_DB)...
set "ConnectionStrings__DefaultConnection=Server=%DB_SERVER%;Database=CinemaBooking_DB;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;"

dotnet tool install --global dotnet-ef >nul 2>&1
echo Dang chay Entity Framework Migrations...
dotnet ef database update

echo.
:: 5. Chay du an
echo [5/5] Khoi dong BE-CINEMA...
echo.
echo ===================================================
echo   CHAY UNG DUNG TREN: http://localhost:5000
echo ===================================================
start "" "http://localhost:5000"
dotnet run --urls "http://localhost:5000"

popd
pause
