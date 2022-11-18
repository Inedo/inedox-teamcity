@echo off

dotnet new tool-manifest --force
dotnet tool install inedo.extensionpackager

cd TeamCity\InedoExtension
dotnet inedoxpack pack . C:\LocalDev\BuildMaster\Extensions\TeamCity.upack --build=Debug -o
cd ..\..