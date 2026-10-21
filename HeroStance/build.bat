@echo off
rem Rebuilds HeroStance.dll. You only need this if you change the code;
rem a ready-made HeroStance.dll is already in this folder.
rem Usage: build.bat                      (also copies into the default Steam BONELAB\Mods if it exists)
rem        build.bat "D:\Games\BONELAB"   (copies into that install instead)

set "BONELAB=C:\Program Files (x86)\Steam\steamapps\common\BONELAB"
if not "%~1"=="" set "BONELAB=%~1"

dotnet build "%~dp0HeroStance.csproj" -c Release
if errorlevel 1 (
    echo Build failed.
    pause
    exit /b 1
)

copy /y "%~dp0bin\Release\net6.0\HeroStance.dll" "%~dp0HeroStance.dll" >nul
echo Built HeroStance.dll

if exist "%BONELAB%\Mods" (
    copy /y "%~dp0HeroStance.dll" "%BONELAB%\Mods\" >nul
    echo Copied it to "%BONELAB%\Mods"
)
pause
