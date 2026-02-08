@echo off
cd /d "%~dp0"
if "%~1"=="" (
    set /p NAME="Migration name: "
) else (
    set NAME=%~1
)
dotnet ef migrations add %NAME% --project src\EquipmentSolver.Infrastructure --startup-project src\EquipmentSolver.Api
pause
