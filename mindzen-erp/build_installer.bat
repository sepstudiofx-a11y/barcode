
@echo off
echo Installing PyInstaller...
python -m pip install pyinstaller

echo Cleaning up previous builds...
rd /s /q build
rd /s /q dist
del /q *.spec

echo Building MindZen ERP...
REM --onedir: Create a directory (easier for debugging assets)
REM --name: Name of the exe
REM --add-data: Include templates and static files. Format is "source;dest" on Windows.
REM --hidden-import: Ensure engine and database drivers are included if missed by auto-analysis.

python -m PyInstaller --noconfirm ^
    --name "MindZenERP" ^
    --onedir ^
    --windowed ^
    --add-data "src/mindzen_erp/templates;mindzen_erp/templates" ^
    --add-data "src/mindzen_erp/static;mindzen_erp/static" ^
    --hidden-import "uvicorn.logging" ^
    --hidden-import "uvicorn.loops" ^
    --hidden-import "uvicorn.loops.auto" ^
    --hidden-import "uvicorn.protocols" ^
    --hidden-import "uvicorn.protocols.http" ^
    --hidden-import "uvicorn.protocols.http.auto" ^
    --hidden-import "uvicorn.lifespan" ^
    --hidden-import "uvicorn.lifespan.on" ^
    --hidden-import "sqlalchemy.sql.default_comparator" ^
    --hidden-import "mindzen_erp.modules.crm" ^
    --hidden-import "mindzen_erp.modules.sales" ^
    --collect-all "mindzen_erp" ^
    src/run_dist.py

echo.
echo Build Complete!
echo The application is located in dist/MindZenERP/MindZenERP.exe
pause
