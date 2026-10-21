@echo off
rem Builds HeroStance.dll and copies it into BONELAB\Mods.
rem Usage: build.bat                      (default Steam install path)
rem        build.bat "D:\Games\BONELAB"   (custom install path)

set "BONELAB=C:\Program Files (x86)\Steam\steamapps\common\BONELAB"
if not "%~1"=="" set "BONELAB=%~1"

if not exist "%BONELAB%\MelonLoader\Il2CppAssemblies" (
    echo Could not find "%BONELAB%\MelonLoader\Il2CppAssemblies".
    echo Install MelonLoader 0.6.x, launch BONELAB once, then run this again.
    echo If BONELAB is somewhere else, run: build.bat "D:\path\to\BONELAB"
    pause
    exit /b 1
)

dotnet build "%~dp0HeroStance.csproj" -c Release "/p:BonelabPath=%BONELAB%" /p:CopyToGame=true
if errorlevel 1 (
    echo Build failed.
) else (
    echo.
    echo Built bin\Release\net6.0\HeroStance.dll and copied it to "%BONELAB%\Mods".
)
pause
