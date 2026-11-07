#Requires AutoHotkey v1.1.0+
;==============================================================
; getProcessCreationFileTime — Get a process creation time as FILETIME (Int64) by PID
;
; GitHub: https://github.com/SevenKeyboard/get-process-creation-file-time
; Author: SevenKeyboard Ltd. (2025-2026)
; License: The Unlicense
;==============================================================
class VersionManager_getProcessCreationFileTime
{
    static _ := VersionManager_getProcessCreationFileTime._init()
    _init()    {
        global
        GETPROCESSCREATIONFILETIME_VERSION := "1.0.1"
    }
}
getProcessCreationFileTime(PID)    {
    static PROCESS_QUERY_LIMITED_INFORMATION:=0x1000
    if !(hProcess:=dllCall("Kernel32.dll\OpenProcess", "UInt",PROCESS_QUERY_LIMITED_INFORMATION, "Int",0, "UInt",dwProcessId:=PID, "Ptr"))
        return false
    try  {
        if !dllCall("Kernel32.dll\GetProcessTimes", "Ptr",hProcess, "Int64*",lpCreationTime, "Int64*",lpExitTime, "Int64*",lpKernelTime, "Int64*",lpUserTime)
            return false
        return lpCreationTime
    }  finally  {
        dllCall("Kernel32.dll\CloseHandle", "Ptr",hProcess)
    }
}