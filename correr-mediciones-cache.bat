@echo off
REM Experimento secc. 7: cache lectores/escritores (preferencia a lectores vs justa).
REM Compila y corre el harness. Duracion estimada: 5-10 min (los escenarios de
REM inanicion esperan su timeout de 90 s, es esperado que "se queden pensando").
cd /d "%~dp0"
echo Compilando harness...
dotnet build -c Release Mediciones\Harness\Harness.csproj > Mediciones\build-log.txt 2>&1
if errorlevel 1 (
    echo ERROR de compilacion. Detalle en Mediciones\build-log.txt
    pause
    exit /b 1
)
echo Ejecutando corridas (no cierres esta ventana)...
dotnet run -c Release --no-build --project Mediciones\Harness
echo.
echo Terminado. Resultados en Mediciones\Resultados\
pause
