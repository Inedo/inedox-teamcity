
REM Requirements:
REM     - This script MUST be run with Administrator rights
REM     - 7zip to be accessible in command (added to PATH)

REM Example from Project Post-Build event :
REM     $(ProjectDir)install_extension.cmd $(TargetName).bmx $(TargetDir) D:\BuildMaster\Extensions\


REM Extension name ending with .bmx
SET EXT_NAME=%1

REM Path to the source files ending with /
SET SRC_DIR=%2

del %SRC_DIR%BuildMaster*
del %SRC_DIR%Inedo*


REM Path to the BuildMaster extensions folder ending with /
SET DST_DIR=%3

REM Stop the buildmaster service
net stop INEDOBMWEBSRV
net stop INEDOBMSVC

REM Delete the old extension
del %DST_DIR%%EXT_NAME%

REM Zip the build files  down to the destination folder
7z a -tzip %DST_DIR%%EXT_NAME% %SRC_DIR%*  

REM Start the buildmaster service
net start INEDOBMSVC
net start INEDOBMWEBSRV