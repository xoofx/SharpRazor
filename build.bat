@echo off
setlocal
call "C:\Program Files (x86)\Microsoft Visual Studio 11.0\vc\vcvarsall.bat" x86
msbuild /tv:4.0 /t:Build /verbosity:quiet /clp:ErrorsOnly /fl /flp:logfile=BuildErrors.log;ErrorsOnly "/p:Configuration=Release;Platform=Any CPU" SharpRazor.sln
if NOT ERRORLEVEL 0 pause
pushd SharpRazor
..\nuget pack -Symbols -OutputDirectory .. SharpRazor.nuspec
if NOT ERRORLEVEL 0 pause
popd
