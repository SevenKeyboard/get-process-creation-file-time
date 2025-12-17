#Requires AutoHotkey v2.0.0+
;==============================================================
; getProcessCreationFileTime — Get a process creation time as FILETIME (Int64) by PID
;
; GitHub: https://github.com/SevenKeyboard/get-process-creation-file-time
; Author: SevenKeyboard Ltd. (2025)
; License: The Unlicense
;==============================================================
class VersionManager_getProcessCreationFileTime
{
    static _ := this._init()
    static _init()    {
        global
        GETPROCESSCREATIONFILETIME_VERSION := "1.0.0"
    }
}
getProcessCreationFileTime(PID)    {
    static PROCESS_QUERY_INFORMATION:=0x0400, PROCESS_VM_READ:=0x0010
    if !(hProcess:=dllCall("Kernel32.dll\OpenProcess", "UInt",PROCESS_QUERY_INFORMATION+PROCESS_VM_READ, "Int",0, "UInt",dwProcessId:=PID, "Ptr"))
        return false
    dllCall("Kernel32.dll\GetProcessTimes", "Ptr",hProcess, "Int64*",&lpCreationTime:=0, "Int64*",&lpExitTime:=0, "Int64*",&lpKernelTime:=0, "Int64*",&lpUserTime:=0)
    dllCall("Kernel32.dllCloseHandle", "Ptr",hProcess)
    return lpCreationTime
}