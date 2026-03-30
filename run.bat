@echo off
setlocal enabledelayedexpansion

chcp 65001 >nul
title BeCinema - One Click Startup (Super Auto)

:: 0. Kiem tra quyen Admin (De tu dong mo Firewall & SQL)
>nul 2>&1 "%SYSTEMROOT%\system32\cacls.exe" "%SYSTEMROOT%\system32\config\system"
if errorlevel 1 (
    echo [INFO] Dang yeu cau quyen Admin de tu dong cau hinh...
    echo Set UAC = CreateObject("Shell.Application") > "%temp%\getadmin.vbs"
    echo UAC.ShellExecute "cmd.exe", "/c ""%~s0"" %*", "", "runas", 1 >> "%temp%\getadmin.vbs"
    "%temp%\getadmin.vbs"
    del "%temp%\getadmin.vbs"
    exit /B
)

pushd "%~dp0"

echo ===================================================
echo   BE-CINEMA ONE-CLICK STARTUP (SUPER AUTO)
echo ===================================================
echo.

:: 1. Lay IP Local (Tu dong cap nhat khi doi Wi-Fi)
for /f "tokens=2 delims=:" %%a in ('ipconfig ^| findstr "IPv4 Address"') do (
    set "LOCAL_IP_RAW=%%a"
    goto :found_ip
)
:found_ip
set "LOCAL_IP=%LOCAL_IP_RAW: =%"
if "%LOCAL_IP%"=="" set "LOCAL_IP=localhost"

:: 2. Mo Firewall (Cho phep may khac truy cap)
echo [1/5] Dang mo cong vao (Firewall)...
netsh advfirewall firewall show rule name="BeCinema" >nul 2>&1
if errorlevel 1 (
    netsh advfirewall firewall add rule name="BeCinema" dir=in action=allow protocol=TCP localport=5000 >nul 2>&1
    echo =^> Firewall OK!
) else (
    echo =^> Firewall Da San Sang.
)

:: 3. Tim va Chay SQL Server (Thong minh)
echo [2/5] Dang kiem tra SQL Server...
set "DB_SERVER="

:: Kiem tra LocalDB truoc (Thuong dung nhat)
sqllocaldb info MSSQLLocalDB >nul 2>&1
if not errorlevel 1 (
    sqllocaldb start MSSQLLocalDB >nul 2>&1
    set "DB_SERVER=(localdb)\mssqllocaldb"
)

:: Kiem tra SQLExpress neu khong co LocalDB
if "%DB_SERVER%"=="" (
    sc query "MSSQL$SQLEXPRESS" >nul 2>&1
    if not errorlevel 1 (
        net start "MSSQL$SQLEXPRESS" >nul 2>&1
        set "DB_SERVER=.\SQLEXPRESS"
    )
)

:: Kiem tra SQL Mac dinh
if "%DB_SERVER%"=="" (
    sc query "MSSQLSERVER" >nul 2>&1
    if not errorlevel 1 (
        net start "MSSQLSERVER" >nul 2>&1
        set "DB_SERVER=."
    )
)

if "%DB_SERVER%"=="" (
    echo [LOI] Khong tim thay SQL Server tren may nay!
    echo       Vui long tai va cai dat SQL Server Express hoac LocalDB.
    start "" https://www.microsoft.com/en-us/sql-server/sql-server-downloads
    pause & exit /b
)

echo =^> Da tim thay: %DB_SERVER%
echo [INFO] Dang doi SQL Server "Tinh day" (Wait-loop 15s)...

:: Vong lap cho SQL san sang phan hoi (Thiet yeu cho One-Click)
set "COUNT=0"
:check_sql
set /a COUNT+=1
powershell -Command "$c = New-Object System.Data.SqlClient.SqlConnection('Server=%DB_SERVER%;Database=master;Trusted_Connection=True;TrustServerCertificate=True;'); try { $c.Open(); $c.Close(); exit 0 } catch { exit 1 }" >nul 2>&1
if errorlevel 1 (
    if !COUNT! leq 15 (
        echo   - Dang thu ket noi... (!COUNT!/15)
        timeout /t 1 >nul
        goto :check_sql
    ) else (
        echo [CANH BAO] SQL Server phan hoi cham, van se tiep tuc thu...
    )
)
echo =^> SQL Server SAN SANG!

:: 4. Kiem tra Redis
echo [3/5] Kiem tra Redis Server (6379)...
netstat -an | findstr ":6379" | findstr "LISTENING" >nul 2>&1
if errorlevel 1 (
    if not exist ".redis\redis-server.exe" (
        echo   - Dang tai Redis Portable (Tu dong)...
        powershell -Command "Invoke-WebRequest -Uri 'https://github.com/tporadowski/redis/releases/download/v5.0.14.1/Redis-x64-5.0.14.1.zip' -OutFile 'redis.zip'; Expand-Archive 'redis.zip' -DestinationPath '.redis' -Force; del redis.zip"
    )
    start "Redis Server" /MIN ".redis\redis-server.exe"
    timeout /t 2 >nul
)
echo =^> Redis OK!

:: 5. Cap nhat Connection & Chay Migration
echo [4/5] Cap nhat Database Migrations...
set "ConnectionStrings__DefaultConnection=Server=%DB_SERVER%;Database=CinemaBooking_DB;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;"
:: Override cac link VNPay theo IP hien tai
set "Vnpay__ReturnUrl=http://%LOCAL_IP%:5000/Payment/VNPayReturn"
set "Vnpay__IpnUrl=http://%LOCAL_IP%:5000/Payment/VNPayIPN"

dotnet restore >nul
dotnet tool install --global dotnet-ef >nul 2>&1
dotnet ef database update >nul 2>&1

:: 6. Khoi dong App
echo [5/5] Dang khoi dong BE-CINEMA...
echo.
echo ===================================================
echo   DA KET NOI THANH CONG!
echo   - Truy cap may nay: http://localhost:5000
echo   - May khac trong mang: http://%LOCAL_IP%:5000
echo ===================================================
echo.

set "ASPNETCORE_URLS=http://0.0.0.0:5000"
start "" "http://localhost:5000"
dotnet run

popd
pause
