@echo off
chcp 65001 >nul
title BeCinema - Auto Run Script

:: ==========================================
:: 0. Kiem tra va xin quyen Admin
:: ==========================================
>nul 2>&1 "%SYSTEMROOT%\system32\cacls.exe" "%SYSTEMROOT%\system32\config\system"
if '%errorlevel%' NEQ '0' (
    echo [INFO] Dang yeu cau quyen Quan tri vien (Administrator) de khoi dong Dich vu...
    goto UACPrompt
) else ( goto gotAdmin )

:UACPrompt
    echo Set UAC = CreateObject^("Shell.Application"^) > "%temp%\getadmin.vbs"
    set params= %*
    echo UAC.ShellExecute "cmd.exe", "/c ""%~s0"" %params%", "", "runas", 1 >> "%temp%\getadmin.vbs"
    "%temp%\getadmin.vbs"
    del "%temp%\getadmin.vbs"
    exit /B

:gotAdmin
    pushd "%CD%"
    CD /D "%~dp0"

:: Kiem tra xem da setup chua, neu co roi thi nhay coc qua phan kiem tra cham
if exist ".setup_done" goto FastRun

echo ===================================================
echo   CÔNG CỤ SETUP DỰ ÁN (CHỈ CHẠY LẦN ĐẦU TIÊN)
echo ===================================================
echo.

:: 1. Kiem tra .NET 8 SDK
echo [1/4] Dang kiem tra .NET 8 SDK...
dotnet --version | findstr "8." >nul 2>&1
if %errorlevel% neq 0 (
    echo [LOI] Khong tim thay .NET 8 SDK hoac phien ban khong dung! 
    echo Vui long cai dat .NET 8 SDK tu trang chu cua Microsoft.
    pause >nul
    start https://dotnet.microsoft.com/en-us/download/dotnet/8.0
    exit /b 1
)
echo =^> .NET 8 SDK OK!
echo.

:: 2. Kiem tra va khoi dong SQL Server
echo [2/4] Dang tim kiem SQL Server tren may...
set DB_SERVER=
sc query MSSQLSERVER >nul 2>&1
if %errorlevel% equ 0 (
    net start MSSQLSERVER >nul 2>&1
    set "DB_SERVER=localhost"
    set "SQL_SERVICE=MSSQLSERVER"
    goto SqlFound
)
sc query MSSQL$SQLEXPRESS >nul 2>&1
if %errorlevel% equ 0 (
    net start MSSQL$SQLEXPRESS >nul 2>&1
    set "DB_SERVER=.\SQLEXPRESS"
    set "SQL_SERVICE=MSSQL$SQLEXPRESS"
    goto SqlFound
)
echo [INFO] Chua co SQL Server Full/Express. Dang khoi tao LocalDB...
sqllocaldb create MSSQLLocalDB -s >nul 2>&1
if %errorlevel% neq 0 (
    echo.
    echo [LOI CHI MANG] May ban CHUA CAI BAT KY PHIEN BAN SQL SERVER NAO!
    echo Script khong the tao LocalDB vi tren may ban chua co data engine cua Microsoft.
    echo Vui long nhan phim bat ky de mo trang Web va TAI VE BAN SQL SERVER EXPRESS.
    echo ^(Tai ve =^> Cai dat =^> Mo lai script nay^)
    pause >nul
    start https://www.microsoft.com/en-us/sql-server/sql-server-downloads
    exit /b 1
)
sqllocaldb start MSSQLLocalDB >nul 2>&1
set "DB_SERVER=(localdb)\mssqllocaldb"
set "SQL_SERVICE=LOCALDB"

:SqlFound
echo =^> SQL Server duoc chon: %DB_SERVER%
echo.

:: 3. Kiem tra va cai dat Redis
echo [3/4] Dang kiem tra Redis Server...
netstat -an | findstr ":6379" | findstr "LISTENING" >nul 2>&1
if %errorlevel% equ 0 (
    echo =^> Redis da san sang chay tren port 6379!
) else (
    if not exist "%~dp0.redis\redis-server.exe" (
        echo   - Dang tai Redis Portable (2MB) tu Github...
        :: FIX TLS 1.2: Ep PowerShell dung chuan TLS 1.2 moi tai duoc file tu Github tren Win cu
        powershell -Command "[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; Invoke-WebRequest -Uri 'https://github.com/tporadowski/redis/releases/download/v5.0.14.1/Redis-x64-5.0.14.1.zip' -OutFile '.redis.zip'"
        powershell -Command "Expand-Archive -Path '.redis.zip' -DestinationPath '.redis' -Force"
        del .redis.zip
    )
    start "Redis Server" /MIN "%~dp0.redis\redis-server.exe"
    timeout /t 2 >nul
    echo =^> Redis khoi dong xong!
)
echo.

:: 4. Tao Database bang EF Core Migrations
echo [4/4] Dang khoi tao Database (Buoc nay chi can chay 1 lan)...
set "ConnectionStrings__DefaultConnection=Server=%DB_SERVER%;Database=CinemaBooking_DB;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;"
dotnet tool install --global dotnet-ef >nul 2>&1
echo Dang chay lenh cap nhat schema Database...
dotnet ef database update

:: Luu trang thai (.setup_done) de lan sau boot toc do cao (FastRun)
> .setup_done echo %DB_SERVER%
>> .setup_done echo %SQL_SERVICE%

echo.
echo ===================================================
echo SETUP HOAN TAT! DANG KHOI CHAY BE-CINEMA...
echo Nhan Ctrl + C de dung Server.
echo ===================================================
goto RunApp

:FastRun
:: Doc cau hinh tu file .setup_done sinh ra tu lan chay dau
< .setup_done (
  set /p DB_SERVER=
  set /p SQL_SERVICE=
)

echo ===================================================
echo    KHỞI ĐỘNG NHANH BE-CINEMA (FAST RUN)
echo ===================================================

:: Tu dong bat lai SQL Server (neu bi tat)
if "%SQL_SERVICE%"=="LOCALDB" (
    sqllocaldb start MSSQLLocalDB >nul 2>&1
) else (
    net start %SQL_SERVICE% >nul 2>&1
)

:: Tu dong bat lai Redis ngam (neu bi tat)
netstat -an | findstr ":6379" | findstr "LISTENING" >nul 2>&1
if %errorlevel% neq 0 (
    if exist "%~dp0.redis\redis-server.exe" (
        start "Redis Server" /MIN "%~dp0.redis\redis-server.exe"
    )
)

:: Khoi phuc Connection String
set "ConnectionStrings__DefaultConnection=Server=%DB_SERVER%;Database=CinemaBooking_DB;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;"

:RunApp
dotnet run --launch-profile "http"
pause
