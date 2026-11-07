# get-process-creation-file-time

Get a process creation time as a raw 64-bit Windows FILETIME by PID.
Targets .NET 10 on Windows 10 or later; supports x64, x86 and NativeAOT.

Reference `src/SevenKeyboard.GetProcessCreationFileTime/SevenKeyboard.GetProcessCreationFileTime.csproj`:

```csharp
if (SevenKeyboard.GetProcessCreationFileTime.TryGet(processId, out long fileTime))
    Console.WriteLine(fileTime);
```

Version 2.0.0 separates success from the timestamp: `TryGet(uint processId, out long fileTime)`
returns `false` and zero on failure. It requests only `PROCESS_QUERY_LIMITED_INFORMATION` (`0x1000`).

Run the smoke checks with `dotnet run --project tests/GetProcessCreationFileTime.Smoke -c Release`.

License: The Unlicense.
