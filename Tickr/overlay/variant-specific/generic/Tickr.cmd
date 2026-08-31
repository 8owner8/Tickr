@echo off
pushd %~dp0

SETLOCAL
SET Tickr_ARGS=%Tickr_ARGS% %*

dotnet --info

dotnet Tickr.dll %Tickr_ARGS%
