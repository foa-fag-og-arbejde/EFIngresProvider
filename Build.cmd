@echo off
setlocal

set MSBUILD="C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\MSBuild\15.0\Bin\MSBuild.exe"
set DEVENV="C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\Common7\IDE\devenv.com"
set SOLUTIONDIR=%~dp0
set SOLUTION=%SOLUTIONDIR%\EFIngresProvider.sln
set DistDir=%SOLUTIONDIR%\dist
set EFIngresProviderDir=%SOLUTIONDIR%\EFIngresProvider
set EFIngresProviderDeployDir=%SOLUTIONDIR%\EFIngresProviderDeploy
set EFIngresProviderVSIXDir=%SOLUTIONDIR%\EFIngresProviderVSIX

rd /S /Q %DistDir%
@if errorlevel 1 (exit /b 1)

nuget restore %SOLUTION%
@if errorlevel 1 (exit /b 1)

%DEVENV% %SOLUTION% /Rebuild Release
@if errorlevel 1 (exit /b 1)

nuget pack %EFIngresProviderDir%\EFIngresProvider.csproj -Properties Configuration=Release -outputdirectory %DistDir%
@if errorlevel 1 (exit /b 1)

copy /Y %EFIngresProviderVSIXDir%\bin\Release\*.vsix %DistDir%
@if errorlevel 1 (exit /b 1)
