using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.ServiceProcess;

namespace AtmosphereTool.Helpers;
public partial class RunAsTi()
{
    private const string SE_DEBUG_NAME = "SeDebugPrivilege";
    private const string SE_IMPERSONATE_NAME = "SeImpersonatePrivilege";

    [LibraryImport("advapi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

    [LibraryImport("advapi32.dll", EntryPoint = "LookupPrivilegeValueW", StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool LookupPrivilegeValue(string? lpSystemName, string lpName, out LUID lpLuid);

    [LibraryImport("advapi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool AdjustTokenPrivileges(IntPtr TokenHandle, [MarshalAs(UnmanagedType.Bool)] bool DisableAllPrivileges,
        ref TOKEN_PRIVILEGES NewState, int BufferLength, IntPtr PreviousState, IntPtr ReturnLength);

    [LibraryImport("kernel32.dll")]
    private static partial IntPtr OpenProcess(uint processAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, int processId);

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CloseHandle(IntPtr hObject);

    [LibraryImport("advapi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool DuplicateTokenEx(IntPtr hExistingToken, uint dwDesiredAccess,
        IntPtr lpTokenAttributes, int ImpersonationLevel, int TokenType, out IntPtr phNewToken);

    [LibraryImport("advapi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool ImpersonateLoggedOnUser(IntPtr hToken);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CreateProcessWithTokenW(IntPtr hToken, int dwLogonFlags,
        string? lpApplicationName, string lpCommandLine, int dwCreationFlags,
        IntPtr lpEnvironment, string? lpCurrentDirectory,
        ref STARTUPINFO lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation);

    private const uint TOKEN_ADJUST_PRIVILEGES = 0x0020;
    private const uint TOKEN_QUERY = 0x0008;
    private const uint MAXIMUM_ALLOWED = 0x02000000;

    private const uint PROCESS_QUERY_INFORMATION = 0x0400;
    private const uint PROCESS_DUP_HANDLE = 0x0040;

    private const int SecurityImpersonation = 2;
    private const int TokenImpersonation = 2;

    private const int CREATE_UNICODE_ENVIRONMENT = 0x00000400;
    private const int LOGON_WITH_PROFILE = 0x00000001;

    [StructLayout(LayoutKind.Sequential)]
    private struct LUID
    {
        public uint LowPart;
        public int HighPart;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct LUID_AND_ATTRIBUTES
    {
        public LUID Luid;
        public uint Attributes;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct TOKEN_PRIVILEGES
    {
        public uint PrivilegeCount;
        public LUID_AND_ATTRIBUTES Privileges;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct STARTUPINFO
    {
        public int cb;
        public string lpReserved;
        public string lpDesktop;
        public string lpTitle;
        public uint dwX, dwY, dwXSize, dwYSize;
        public uint dwXCountChars, dwYCountChars;
        public uint dwFillAttribute;
        public uint dwFlags;
        public short wShowWindow;
        public short cbReserved2;
        public IntPtr lpReserved2;
        public IntPtr hStdInput, hStdOutput, hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PROCESS_INFORMATION
    {
        public IntPtr hProcess;
        public IntPtr hThread;
        public uint dwProcessId;
        public uint dwThreadId;
    }

    private static void EnablePrivilege(string privilege)
    {
        if (!OpenProcessToken(Process.GetCurrentProcess().Handle, TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out var hToken))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        if (!LookupPrivilegeValue(null, privilege, out var luid))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        var tp = new TOKEN_PRIVILEGES
        {
            PrivilegeCount = 1,
            Privileges = new LUID_AND_ATTRIBUTES
            {
                Luid = luid,
                Attributes = 0x00000002 // SE_PRIVILEGE_ENABLED
            }
        };

        if (!AdjustTokenPrivileges(hToken, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        CloseHandle(hToken);
    }

    private static int GetProcessIdByName(string name)
    {
        foreach (var p in Process.GetProcessesByName(name))
        {
            return p.Id;
        }
        throw new Exception($"{name} not found");
    }

    private static void ImpersonateSystem()
    {
        var pid = GetProcessIdByName("winlogon");
        var hProcess = OpenProcess(PROCESS_DUP_HANDLE | PROCESS_QUERY_INFORMATION, false, pid);

        if (!OpenProcessToken(hProcess, MAXIMUM_ALLOWED, out var hToken))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        if (!DuplicateTokenEx(hToken, MAXIMUM_ALLOWED, IntPtr.Zero, SecurityImpersonation, TokenImpersonation, out var hDup))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        if (!ImpersonateLoggedOnUser(hDup))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
    }

    private static int StartTrustedInstallerService()
    {
        var sc = new ServiceController("TrustedInstaller");
        if (sc.Status == ServiceControllerStatus.Stopped)
        {
            sc.Start();
            sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(10));
        }

        // Find TrustedInstaller PID
        return GetProcessIdByName("TrustedInstaller");
    }

    private static void CreateProcessAsTrustedInstaller(int pid, string commandLine)
    {
        EnablePrivilege(SE_DEBUG_NAME);
        EnablePrivilege(SE_IMPERSONATE_NAME);
        ImpersonateSystem();

        var hProcess = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_DUP_HANDLE, false, pid);
        if (!OpenProcessToken(hProcess, MAXIMUM_ALLOWED, out var hToken))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        if (!DuplicateTokenEx(hToken, MAXIMUM_ALLOWED, IntPtr.Zero, SecurityImpersonation, TokenImpersonation, out var hDup))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        var si = new STARTUPINFO();
        si.cb = Marshal.SizeOf(si);
        si.lpDesktop = "Winsta0\\Default";


        if (!CreateProcessWithTokenW(hDup, LOGON_WITH_PROFILE, null, commandLine, CREATE_UNICODE_ENVIRONMENT,
            IntPtr.Zero, null, ref si, out PROCESS_INFORMATION pi))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        LogHelper.LogError($"Started process with PID: {pi.dwProcessId}");
    }

    public static void RunAsTrustedInstaller(string exe)
    {
        try
        {
            var pid = StartTrustedInstallerService();
            CreateProcessAsTrustedInstaller(pid, $"\"{exe}\"");
        }
        catch (Exception ex)
        {
            LogHelper.LogError($"Error: {ex.Message}");
        }
    }
};
