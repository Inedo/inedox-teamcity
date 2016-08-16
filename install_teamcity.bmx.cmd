
REM Requirements:
REM     - This script MUST be run with Administrator rights
REM     - 7zip to be accessible in command (added to PATH)

REM Example from Project Post-Build event :
REM     $(ProjectDir)install_extension.cmd $(TargetName).bmx $(TargetDir) D:\BuildMaster\Extensions\

SET EXT_NAME=TeamCity
SET PROJECT_HOME=D:\home\myusername\Documents\bmx-teamcity\
SET BUILDMASTER_HOME=D:\BuildMaster\

call %PROJECT_HOME%install_extension.cmd %EXT_NAME%.bmx %PROJECT_HOME%bin\Debug\ %BUILDMASTER_HOME%Extensions\ 

pause