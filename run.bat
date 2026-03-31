@echo off
setlocal enabledelayedexpansion
chcp 65001 >nul
title BeCinema - One Click Startup (Super Auto)

:: [0] Request Admin Privileges (For SQL/Redis services & Firewall)
>nul 2>&1 "%SYSTEMROOT%\system32\cacls.exe" "%SYSTEMROOT%\system32\config\system"
IF %ERRORLEVEL% NEQ 0 (
    echo [INFO] Requesting Admin privileges for configuration...
    echo Set UAC = CreateObject("Shell.Application") > "%temp%\getadmin.vbs"
    echo UAC.ShellExecute "cmd.exe", "/c ""%~s0"" %*", "", "runas", 1 >> "%temp%\getadmin.vbs"
    "%temp%\getadmin.vbs"
    del "%temp%\getadmin.vbs"
    exit /B
)

set "APP_URL=http://localhost:5000"
set "PROJECT_DIR=%~dp0"
pushd "%PROJECT_DIR%"

echo ===================================================
echo   BE-CINEMA ONE-CLICK STARTUP (SUPER AUTO)
echo ===================================================
echo.

echo [1/6] Checking .NET 8 SDK...
dotnet --version >nul 2>&1
IF %ERRORLEVEL% NEQ 0 (
    echo .NET SDK not found. Trying to install .NET 8 automatically...
    where winget >nul 2>&1
    IF %ERRORLEVEL% EQU 0 (
        winget install --id Microsoft.DotNet.SDK.8 --source winget --silent --accept-package-agreements --accept-source-agreements
        IF %ERRORLEVEL% NEQ 0 (
            echo Auto install failed. Opening .NET 8 download page...
            start "" https://dotnet.microsoft.com/en-us/download/dotnet/8.0
            echo Please install .NET 8 SDK, then run this file again.
            pause
            exit /b 1
        )
    ) ELSE (
        echo winget not available. Opening .NET 8 download page...
        start "" https://dotnet.microsoft.com/en-us/download/dotnet/8.0
        echo Please install .NET 8 SDK, then run this file again.
        pause
        exit /b 1
    )
)
echo .NET SDK is ready.
echo.

echo [2/6] Checking SQL Server service...
set "SQL_SERVICE="
sc query "MSSQL$SQLEXPRESS" >nul 2>&1
IF %ERRORLEVEL% EQU 0 set "SQL_SERVICE=MSSQL$SQLEXPRESS"

IF "%SQL_SERVICE%"=="" (
    sc query "MSSQLSERVER" >nul 2>&1
    IF %ERRORLEVEL% EQU 0 set "SQL_SERVICE=MSSQLSERVER"
)

IF "%SQL_SERVICE%"=="" (
    echo SQL Server service not found.
    echo Opening SQL Server Express download page...
    start "" https://www.microsoft.com/en-us/sql-server/sql-server-downloads
    echo Please install SQL Server Express, then run this file again.
    pause
    exit /b 1
)

sc query "%SQL_SERVICE%" | find "RUNNING" >nul
IF %ERRORLEVEL% NEQ 0 (
    echo Starting service %SQL_SERVICE% ...
    net start "%SQL_SERVICE%" >nul 2>&1
)

:: Set Connection String based on detected service
IF /I "%SQL_SERVICE%"=="MSSQL$SQLEXPRESS" (
    set "ConnectionStrings__DefaultConnection=Server=localhost\SQLEXPRESS;Database=CinemaBooking_DB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
) ELSE (
    set "ConnectionStrings__DefaultConnection=Server=localhost;Database=CinemaBooking_DB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
)
echo SQL Server is running.
echo.

echo [3/6] Checking Redis Server (6379)...
netstat -an | findstr ":6379" | findstr "LISTENING" >nul 2>&1
IF %ERRORLEVEL% NEQ 0 (
    IF NOT EXIST ".redis\redis-server.exe" (
        echo   - Downloading Redis Portable...
        powershell -Command "Invoke-WebRequest -Uri 'https://github.com/tporadowski/redis/releases/download/v5.0.14.1/Redis-x64-5.0.14.1.zip' -OutFile 'redis.zip'; Expand-Archive 'redis.zip' -DestinationPath '.redis' -Force; del redis.zip"
    )
    start "Redis Server" /MIN ".redis\redis-server.exe"
    timeout /t 2 >nul
)
echo Redis is running.
echo.

echo [4/6] Restoring dependencies & Database...
dotnet restore
dotnet tool install --global dotnet-ef >nul 2>&1
dotnet ef database update >nul 2>&1
echo Restore & Update OK.
echo.

echo [5/6] Opening browser...
:: Get Local IP for VNPAY & External Access
for /f "tokens=2 delims=:" %%a in ('ipconfig ^| findstr "IPv4 Address"') do (
    set "LOCAL_IP_RAW=%%a"
    goto :found_ip
)
:found_ip
set "LOCAL_IP=!LOCAL_IP_RAW: =!"
IF "%LOCAL_IP%"=="" set "LOCAL_IP=localhost"

start "" "%APP_URL%"
echo.

echo [6/6] Starting BE-CINEMA...
:: Environment variables for VNPAY callback & Remote access
set "Vnpay__ReturnUrl=http://%LOCAL_IP%:5000/Payment/VNPayReturn"
set "Vnpay__IpnUrl=http://%LOCAL_IP%:5000/Payment/VNPayIPN"
set "ASPNETCORE_URLS=http://0.0.0.0:5000"

echo ===================================================
echo   - Local access: %APP_URL%
echo   - Network access: http://%LOCAL_IP%:5000
echo ===================================================
dotnet run --urls "%APP_URL%"
set "APP_EXIT_CODE=%ERRORLEVEL%"
popd

IF %APP_EXIT_CODE% NEQ 0 (
    echo Application exited with code %APP_EXIT_CODE%.
)
pause