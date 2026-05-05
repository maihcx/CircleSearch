@echo off
setlocal enabledelayedexpansion

echo ============================================
echo   Building CircleSearch Installer (WPF)
echo ============================================

set APP_PROJECTS=CircleSearch CircleSearch.Overlay CircleSearch.Tray CircleSearch.Core
set INSTALLER_PROJECT=CircleSearch.Installer\CircleSearch.Installer.csproj
set OUTPUT_DIR=.\installer-output
set PAYLOAD_DIR=%OUTPUT_DIR%\publish
set PAYLOAD_ZIP=.\CircleSearch.Installer\Resources\payload.zip

REM Xóa output cũ
if exist "%OUTPUT_DIR%" (
    echo Cleaning previous output...
    rmdir /s /q "%OUTPUT_DIR%"
)
mkdir "%OUTPUT_DIR%"
mkdir "%PAYLOAD_DIR%"

echo.
echo Starting build process...

REM ── Bước 1: Build các app project vào publish/ ──
for %%P in (%APP_PROJECTS%) do (
    echo [%%P] Building...

    dotnet publish .\%%P\%%P.csproj -c Release -r win-x64 ^
        /p:DebugType=None ^
        /p:DebugSymbols=false ^
        -o "%PAYLOAD_DIR%"

    if !errorlevel! neq 0 (
        echo [%%P] Build FAILED!
        pause
        exit /b !errorlevel!
    )

    echo [%%P] Build successful!
    echo.
)

REM ── Bước 2: Tạo payload.zip rỗng placeholder để pass 1 compile được ──
echo Creating placeholder payload.zip for pass 1...
if exist "%PAYLOAD_ZIP%" del /f /q "%PAYLOAD_ZIP%"
powershell -NoProfile -Command "Add-Type -AssemblyName System.IO.Compression; $ms = New-Object System.IO.MemoryStream; $za = New-Object System.IO.Compression.ZipArchive($ms, 'Create'); $za.Dispose(); [System.IO.File]::WriteAllBytes('%PAYLOAD_ZIP%', $ms.ToArray())"
if !errorlevel! neq 0 (
    echo [ERROR] Failed to create placeholder payload.zip!
    pause
    exit /b !errorlevel!
)

REM ── Bước 3: Build Installer lần 1 → installer-output/ (payload rỗng) ──
echo [CircleSearch.Installer] Building (pass 1 - placeholder payload)...

dotnet publish "%INSTALLER_PROJECT%" -c Release -r win-x64 ^
    /p:DebugType=None ^
    /p:DebugSymbols=false ^
    -o "%OUTPUT_DIR%"

if !errorlevel! neq 0 (
    echo [CircleSearch.Installer] Pass 1 FAILED!
    pause
    exit /b !errorlevel!
)
echo [CircleSearch.Installer] Pass 1 successful!
echo.

REM ── Bước 4: Copy CircleSearch.Installer.exe vào publish/ để nhúng vào payload ──
echo Copying CircleSearch.Installer.exe into payload...
copy /y "%OUTPUT_DIR%\CircleSearch.Installer.exe" "%PAYLOAD_DIR%\CircleSearch.Installer.exe" >nul

REM Copy LICENSE vào payload nếu có
if exist "LICENSE" (
    copy /y "LICENSE" "%PAYLOAD_DIR%\LICENSE" >nul
    echo Copied LICENSE into payload
)

REM ── Bước 5: Zip publish/* → payload.zip (ghi đè bản rỗng) ──
echo.
echo Packaging payload into zip...

if exist "%PAYLOAD_ZIP%" del /f /q "%PAYLOAD_ZIP%"

powershell -NoProfile -Command ^
    "Compress-Archive -Path '%PAYLOAD_DIR%\*' -DestinationPath '%PAYLOAD_ZIP%' -Force"

if !errorlevel! neq 0 (
    echo [ERROR] Failed to create payload.zip!
    pause
    exit /b !errorlevel!
)
echo Payload zip created: %PAYLOAD_ZIP%
echo.

REM ── Bước 6: Rebuild Installer với payload.zip thật → installer-output/ ──
echo [CircleSearch.Installer] Building (pass 2 - with payload)...

dotnet publish "%INSTALLER_PROJECT%" -c Release -r win-x64 ^
    /p:DebugType=None ^
    /p:DebugSymbols=false ^
    -o "%OUTPUT_DIR%"

if !errorlevel! neq 0 (
    echo [CircleSearch.Installer] Pass 2 FAILED!
    pause
    exit /b !errorlevel!
)
echo [CircleSearch.Installer] Pass 2 successful!

REM ── Bước 7: Dọn dẹp tất cả file tạm ──
echo.
echo Cleaning up intermediate files...
if exist "%PAYLOAD_DIR%" rmdir /s /q "%PAYLOAD_DIR%"
if exist "%PAYLOAD_ZIP%" del /f /q "%PAYLOAD_ZIP%"

echo.
echo ============================================
echo   Done!
echo   Single-file installer: %OUTPUT_DIR%\CircleSearch.Installer.exe
echo ============================================
pause
