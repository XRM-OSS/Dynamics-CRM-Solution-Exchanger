@echo off
cls
"tools\nuget\nuget.exe" "install" "FAKE" "-OutputDirectory" "tools" "-ExcludeVersion"
"tools\nuget\nuget.exe" "install" "NUnit.Runners" "-OutputDirectory" "tools" "-ExcludeVersion"
call 02_build.bat %1