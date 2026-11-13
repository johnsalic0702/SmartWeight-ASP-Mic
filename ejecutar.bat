@echo off
setlocal

echo ========================================
echo  SmartWeight ASP.NET - Inicio servicios
echo ========================================
echo.

if not exist "C:\SmartWeight-ASP\Auth\Auth\bin\Debug\net9.0\Auth.exe" goto :missing
if not exist "C:\SmartWeight-ASP\Maestros\Maestros\bin\Debug\net9.0\Maestros.exe" goto :missing
if not exist "C:\SmartWeight-ASP\Core-Pesaje\Core-Pesaje\bin\Debug\net9.0\Core-Pesaje.exe" goto :missing
if not exist "C:\SmartWeight-ASP\Gateway\Gateway\bin\Debug\net9.0\Gateway.exe" goto :missing

echo Iniciando Auth        :8081
start "SmartWeight Auth" /D "C:\SmartWeight-ASP\Auth\Auth" "C:\SmartWeight-ASP\Auth\Auth\bin\Debug\net9.0\Auth.exe"

echo Iniciando Maestros    :8083
start "SmartWeight Maestros" /D "C:\SmartWeight-ASP\Maestros\Maestros" "C:\SmartWeight-ASP\Maestros\Maestros\bin\Debug\net9.0\Maestros.exe"

echo Iniciando Core-Pesaje :8082
start "SmartWeight Core-Pesaje" /D "C:\SmartWeight-ASP\Core-Pesaje\Core-Pesaje" "C:\SmartWeight-ASP\Core-Pesaje\Core-Pesaje\bin\Debug\net9.0\Core-Pesaje.exe"

echo Iniciando Gateway     :8080
start "SmartWeight Gateway" /D "C:\SmartWeight-ASP\Gateway\Gateway" "C:\SmartWeight-ASP\Gateway\Gateway\bin\Debug\net9.0\Gateway.exe"

echo.
echo Servicios iniciados en ventanas independientes.
goto :eof

:missing
echo ERROR: Falta uno o mas ejecutables compilados. No se inicio ningun servicio.
exit /b 1
