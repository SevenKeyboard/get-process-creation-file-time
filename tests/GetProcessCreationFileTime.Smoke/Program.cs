using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using SevenKeyboard;

internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length == 1 && args[0] == "--child")
        {
            Console.WriteLine("ready");
            Console.ReadLine();
            return 0;
        }

        // Lets a shared permission harness query its own controlled child.
        if (args.Length == 2 && args[0] == "--query")
        {
            uint processId = uint.Parse(args[1], CultureInfo.InvariantCulture);
            bool success = GetProcessCreationFileTime.TryGet(processId, out long fileTime);
            Console.WriteLine($"{success} {fileTime.ToString(CultureInfo.InvariantCulture)}");
            return success ? 0 : 1;
        }

        try
        {
            using Process current = Process.GetCurrentProcess();
            long expected = CheckCreationTime(current);
            Console.WriteLine($"PASS current PID ({RuntimeInformation.ProcessArchitecture})");
            CheckFailure(0);
            CheckFailure(uint.MaxValue);
            Console.WriteLine("PASS invalid PIDs return false and zero");
            CheckChild();
            CheckHandleCleanup(current, expected);
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static long CheckCreationTime(Process process)
    {
        DateTime expected = process.StartTime.ToUniversalTime();
        Check(GetProcessCreationFileTime.TryGet((uint)process.Id, out long actual), "Creation query failed.");
        Check(actual == expected.ToFileTimeUtc(), "Raw FILETIME differs from Process.StartTime.");
        Check(DateTime.FromFileTimeUtc(actual) == expected, "FILETIME conversion differs from Process.StartTime.");
        return actual;
    }

    private static void CheckFailure(uint processId)
    {
        long fileTime = -1;
        Check(!GetProcessCreationFileTime.TryGet(processId, out fileTime), "Invalid PID unexpectedly succeeded.");
        Check(fileTime == 0, "Failure did not clear the output.");
    }

    private static void CheckChild()
    {
        var startInfo = new ProcessStartInfo(Environment.ProcessPath!)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            WorkingDirectory = AppContext.BaseDirectory
        };
        startInfo.ArgumentList.Add("--child");

        uint childId;
        long expected;
        using (Process child = Process.Start(startInfo) ?? throw new InvalidOperationException("Child start failed."))
        {
            childId = (uint)child.Id;
            try
            {
                string? ready = child.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(15)).GetAwaiter().GetResult();
                Check(ready == "ready", "Child did not become ready.");
                expected = CheckCreationTime(child);
                Check(GetProcessCreationFileTime.TryGet(childId, out long repeated) && repeated == expected,
                    "Repeated child query changed the creation time.");
                Console.WriteLine("PASS child PID matches Process.StartTime");

                child.StandardInput.WriteLine();
                Check(child.WaitForExit(10000) && child.ExitCode == 0, "Child did not exit cleanly.");
                // A retained process handle keeps the exited process queryable.
                Check(GetProcessCreationFileTime.TryGet(childId, out long exited) && exited == expected,
                    "Exited child with a retained handle lost its creation time.");
            }
            finally
            {
                if (!child.HasExited)
                {
                    child.Kill();
                    child.WaitForExit();
                }
            }
        }

        // Other processes may retain a handle after ours closes, so either outcome is valid.
        bool success = GetProcessCreationFileTime.TryGet(childId, out long fileTime);
        Check(success ? fileTime == expected : fileTime == 0, "Exited PID returned an inconsistent result.");
        Console.WriteLine($"PASS exited PID: retained handle succeeds; after disposal success={success}");
    }

    private static void CheckHandleCleanup(Process current, long expected)
    {
        uint processId = (uint)current.Id;
        for (int iteration = 0; iteration < 100; iteration++)
            Check(GetProcessCreationFileTime.TryGet(processId, out _), "Warm-up query failed.");

        SafeProcessHandle process = current.SafeHandle;
        Check(GetProcessHandleCount(process, out uint before), "Initial handle count failed.");
        for (int iteration = 0; iteration < 20000; iteration++)
        {
            Check(GetProcessCreationFileTime.TryGet(processId, out long actual) && actual == expected,
                "Repeated query failed or changed creation time.");
            CheckFailure(0);
            CheckFailure(uint.MaxValue);
        }
        Check(GetProcessHandleCount(process, out uint after), "Final handle count failed.");
        Check(after <= before + 4, $"Process handles grew from {before} to {after}.");
        Console.WriteLine($"PASS 20,000 successful and 40,000 failed queries; handles {before} -> {after}");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    [DllImport("kernel32.dll", ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetProcessHandleCount(SafeProcessHandle process, out uint handleCount);
}
