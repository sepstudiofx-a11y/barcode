
@echo off
set VERSION=v1.0.0
set RELEASE_DIR=releases\%VERSION%

echo Packaging Release %VERSION%...

if not exist "dist\MindZenERP\MindZenERP.exe" (
    echo Error: Build not found! Please run build_installer.bat first.
    exit /b 1
)

if not exist "%RELEASE_DIR%" (
    mkdir "%RELEASE_DIR%"
)

echo Copying build artifacts to %RELEASE_DIR%...
xcopy /E /I /Y "dist\MindZenERP" "%RELEASE_DIR%\MindZenERP"

echo.
echo Versioned release created at: %RELEASE_DIR%\MindZenERP
echo.
pause
