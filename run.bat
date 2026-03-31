@echo off
setlocal enabledelayedexpansion
chcp 65001 >nul
title BeCinema - One Click Startup (Super Auto)

:: [0] Yeu cau quyen Admin (De tu dong mo Firewall & bat SQL Service)
>nul 2>&1 "%SYSTEMROOT%\system32\cacls.exe" "%SYSTEMROOT%\system32\config\system"
IF %ERRORLEVEL% NEQ 0 (
    goto UACPrompt
) ELSE (
    goto GotAdmin
)

:UACPrompt
echo [INFO] Dang yeu cau quyen Admin de tu dong cau hinh...
echo Set UAC = CreateObject("Shell.Application") > "%temp%\getadmin.vbs"
echo UAC.ShellExecute "cmd.exe", "/c ""%~s0"" %*", "", "runas", 1 >> "%temp%\getadmin.vbs"
"%temp%\getadmin.vbs"
del "%temp%\getadmin.vbs"
exit /B

:GotAdmin
set "PROJECT_DIR=%~dp0"
pushd "%PROJECT_DIR%"

:: (Giữ nguyên toàn bộ phần code bên dưới từ đoạn echo ========= trở đi)
echo ===================================================
echo   BE-CINEMA ONE-CLICK STARTUP (SUPER AUTO)
echo ===================================================
echo.

echo [1/6] Lay IP Local va Mo Firewall...
for /f "tokens=2 delims=:" %%a in ('ipconfig ^| findstr "IPv4 Address"') do (
    set "LOCAL_IP_RAW=%%a"
    goto :found_ip
)
:found_ip
set "LOCAL_IP=!LOCAL_IP_RAW: =!"
IF "%LOCAL_IP%"=="" set "LOCAL_IP=localhost"

netsh advfirewall firewall show rule name="BeCinema" >nul 2>&1
IF %ERRORLEVEL% NEQ 0 (
    netsh advfirewall firewall add rule name="BeCinema" dir=in action=allow protocol=TCP localport=5000 >nul 2>&1
    echo =^> Firewall da duoc mo cho cong 5000!
) ELSE (
    echo =^> Firewall da san sang.
)
echo.

echo [2/6] Kiem tra SQL Server service...
set "DB_SERVER="
set "SQL_SERVICE="

:: Kiem tra LocalDB truoc (Thuong dung trong moi truong dev)
sqllocaldb info MSSQLLocalDB >nul 2>&1
IF %ERRORLEVEL% EQU 0 (
    sqllocaldb start MSSQLLocalDB >nul 2>&1
    set "DB_SERVER=(localdb)\mssqllocaldb"
    goto :sql_ready
)

:: Kiem tra SQLExpress
sc query "MSSQL$SQLEXPRESS" >nul 2>&1
IF %ERRORLEVEL% EQU 0 set "SQL_SERVICE=MSSQL$SQLEXPRESS"

:: Kiem tra SQL Server mac dinh
IF "%SQL_SERVICE%"=="" (
    sc query "MSSQLSERVER" >nul 2>&1
    IF %ERRORLEVEL% EQU 0 set "SQL_SERVICE=MSSQLSERVER"
)

IF "%SQL_SERVICE%"=="" (
    echo SQL Server khong duoc tim thay tren may nay.
    echo Vui long tai va cai dat SQL Server Express hoac LocalDB.
    start "" https://www.microsoft.com/en-us/sql-server/sql-server-downloads
    pause
    exit /b 1
)

:: Kiem tra xem service da chay chua, neu chua thi bat len
sc query "%SQL_SERVICE%" | find "RUNNING" >nul
IF %ERRORLEVEL% NEQ 0 (
    echo Dang khoi dong service %SQL_SERVICE%...
    net start "%SQL_SERVICE%" >nul 2>&1
)

sc query "%SQL_SERVICE%" | find "RUNNING" >nul
IF %ERRORLEVEL% NEQ 0 (
    echo Khong the khoi dong SQL Server service %SQL_SERVICE%.
    echo Vui long mo SQL Server Configuration Manager va bat thu cong.
    pause
    exit /b 1
)

IF /I "%SQL_SERVICE%"=="MSSQL$SQLEXPRESS" (
    set "DB_SERVER=.\SQLEXPRESS"
) ELSE (
    set "DB_SERVER=."
)

:sql_ready
echo =^> Da tim thay va san sang: %DB_SERVER%
set "ConnectionStrings__DefaultConnection=Server=%DB_SERVER%;Database=CinemaBooking_DB;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;"
echo.

echo [3/6] Kiem tra Redis Server (6379)...
netstat -an | findstr ":6379" | findstr "LISTENING" >nul 2>&1
IF %ERRORLEVEL% NEQ 0 (
    IF NOT EXIST ".redis\redis-server.exe" (
        echo   - Dang tai Redis Portable tu dong...
        powershell -Command "Invoke-WebRequest -Uri 'https://github.com/tporadowski/redis/releases/download/v5.0.14.1/Redis-x64-5.0.14.1.zip' -OutFile 'redis.zip'; Expand-Archive 'redis.zip' -DestinationPath '.redis' -Force; del redis.zip"
    )
    start "Redis Server" /MIN ".redis\redis-server.exe"
    timeout /t 2 >nul
)
echo =^> Redis dang chay OK!
echo.

echo [4/6] Restoring dependencies ^& Migrations...
dotnet restore
IF %ERRORLEVEL% NEQ 0 (
    echo Restore failed. Vui long kiem tra ket noi mang va chay lai.
    popd
    pause
    exit /b 1
)

dotnet tool install --global dotnet-ef >nul 2>&1
dotnet ef database update >nul 2>&1
echo =^> Update Database OK.
echo.

echo [5/6] Mo trinh duyet...
start "" "http://localhost:5000"
echo.

echo [6/6] Dang khoi dong BE-CINEMA...
:: Cap nhat cac bien moi truong rieng cho BeCinema
set "Vnpay__ReturnUrl=http://%LOCAL_IP%:5000/Payment/VNPayReturn"
set "Vnpay__IpnUrl=http://%LOCAL_IP%:5000/Payment/VNPayIPN"
set "ASPNETCORE_URLS=http://0.0.0.0:5000"

echo ===================================================
echo   - Truy cap may nay: http://localhost:5000
echo   - May khac truy cap: http://%LOCAL_IP%:5000
echo ===================================================
dotnet run
set "APP_EXIT_CODE=%ERRORLEVEL%"

popd

IF %APP_EXIT_CODE% NEQ 0 (
    echo.
    echo Application exited with code %APP_EXIT_CODE%.
)

pause