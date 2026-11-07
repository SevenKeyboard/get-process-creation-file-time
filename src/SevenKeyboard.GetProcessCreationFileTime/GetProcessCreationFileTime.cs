//==============================================================
// GetProcessCreationFileTime — Get a process creation time as FILETIME (Int64) by PID
//
// GitHub: https://github.com/SevenKeyboard/get-process-creation-file-time
// Author: SevenKeyboard Ltd. (2026)
// License: The Unlicense
//==============================================================

using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace SevenKeyboard;

public static class GetProcessCreationFileTime
{
    private const uint ProcessQueryLimitedInformation = 0x1000;

    public static bool TryGet(uint processId, out long fileTime)
    {
        fileTime = 0;
        using SafeProcessHandle process = OpenProcess(ProcessQueryLimitedInformation, false, processId);
        if (process.IsInvalid)
            return false;

        if (!GetProcessTimes(process, out long creationTime, out _, out _, out _))
            return false;

        fileTime = creationTime;
        return true;
    }

    [DllImport("kernel32.dll", ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern SafeProcessHandle OpenProcess(
        uint desiredAccess, [MarshalAs(UnmanagedType.Bool)] bool inheritHandle, uint processId);

    [DllImport("kernel32.dll", ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetProcessTimes(
        SafeProcessHandle process, out long creationTime, out long exitTime, out long kernelTime, out long userTime);
}
