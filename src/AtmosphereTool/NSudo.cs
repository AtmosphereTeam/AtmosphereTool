using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Principal;

namespace AtmosphereTool
{
    [StructLayout(LayoutKind.Sequential)]

    public partial class NSudo
    {
        private struct SECURITY_ATTRIBUTES
        {
            public int nLength;
            public unsafe byte* lpSecurityDescriptor;
            public int bInheritHandle;
        }
        private enum SECURITY_IMPERSONATION_LEVEL
        {
            SecurityAnonymous,
            SecurityIdentification,
            SecurityImpersonation,
            SecurityDelegation
        }
        private enum TOKEN_TYPE
        {
            TokenPrimary = 1,
            TokenImpersonation
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct LUID
        {
            public uint LowPart;
            public uint HighPart;
        }
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        private struct LUID_AND_ATTRIBUTES
        {
            public LUID Luid;
            public uint Attributes;
        }

        private struct TOKEN_PRIVILEGES
        {
            public int PrivilegeCount;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
            public LUID_AND_ATTRIBUTES[] Privileges;
        }


        private static readonly uint MAXIMUM_ALLOWED = (uint)TokenAccessLevels.MaximumAllowed;

        // First time using library import
        [LibraryImport("advapi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool OpenProcessToken(IntPtr ProcessHandle,
            uint DesiredAccess, out IntPtr TokenHandle);

        [LibraryImport("advapi32.dll", StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool DuplicateTokenEx(
            IntPtr hExistingToken,
            uint dwDesiredAccess,
            IntPtr lpTokenAttributes,
            SECURITY_IMPERSONATION_LEVEL ImpersonationLevel,
            TOKEN_TYPE TokenType,
            out IntPtr phNewToken);
        [LibraryImport("advapi32.dll", StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool DuplicateTokenEx(
            IntPtr hExistingToken,
            uint dwDesiredAccess,
            ref SECURITY_ATTRIBUTES lpTokenAttributes,
            SECURITY_IMPERSONATION_LEVEL ImpersonationLevel,
            TOKEN_TYPE TokenType,
            out IntPtr phNewToken);
        [LibraryImport("advapi32.dll", EntryPoint = "LookupPrivilegeValueW", StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool LookupPrivilegeValue(IntPtr lpSystemName, string lpName,
            ref LUID lpLuid);
        internal const int SE_PRIVILEGE_ENABLED = 0x00000002;

        // Use this signature if you do not want the previous state
        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AdjustTokenPrivileges(IntPtr TokenHandle,
            [MarshalAs(UnmanagedType.Bool)] bool DisableAllPrivileges,
            ref TOKEN_PRIVILEGES NewState,
            uint Zero,
            IntPtr Null1,
            IntPtr Null2);

        [LibraryImport("advapi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool SetThreadToken(IntPtr pHandle,
            IntPtr hToken);




        [LibraryImport("wtsapi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool WTSQueryUserToken(uint sessionId, out IntPtr Token);

        [LibraryImport("advapi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool SetTokenInformation(IntPtr TokenHandle, TOKEN_INFORMATION_CLASS TokenInformationClass,
            ref uint TokenInformation, uint TokenInformationLength);


        [LibraryImport("userenv.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool CreateEnvironmentBlock(out IntPtr lpEnvironment, IntPtr hToken, [MarshalAs(UnmanagedType.Bool)] bool bInherit);
        public static bool GetUserPrivilege(IntPtr Token)
        {
            DuplicateTokenEx(Token, MAXIMUM_ALLOWED, IntPtr.Zero, SECURITY_IMPERSONATION_LEVEL.SecurityImpersonation, TOKEN_TYPE.TokenImpersonation, out nint NewToken);
            SetThreadToken(IntPtr.Zero, NewToken);
            return true;
        }

        [LibraryImport("advapi32.dll", StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool CreateProcessAsUser(
            IntPtr hToken,
            string? lpApplicationName,
            string lpCommandLine,
            ref SECURITY_ATTRIBUTES lpProcessAttributes,
            ref SECURITY_ATTRIBUTES lpThreadAttributes,
            [MarshalAs(UnmanagedType.Bool)] bool bInheritHandles,
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            string? lpCurrentDirectory,
            ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation);

        [Flags]
        private enum CreationFlags
        {
            CREATE_SUSPENDED = 0x00000004,
            CREATE_UNICODE_ENVIRONMENT = 0x00000400,
            CREATE_NO_WINDOW = 0x08000000,
            CREATE_NEW_CONSOLE = 0x00000010
        }
        [LibraryImport("advapi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool LogonUser(
            [MarshalAs(UnmanagedType.LPStr)] string pszUserName,
            [MarshalAs(UnmanagedType.LPStr)] string pszDomain,
            [MarshalAs(UnmanagedType.LPStr)] string pszPassword,
            int dwLogonType,
            int dwLogonProvider,
            ref IntPtr phToken);

        public static int? RunProcessAsUser(IntPtr Token, string Executable, string Arguments, uint timeout = 0xFFFFFFFF)
        {
            GetAssignPrivilege();
            GetQuotaPrivilege();

            STARTUPINFO startupInfo = new();
            startupInfo.cb = Marshal.SizeOf(startupInfo);
            startupInfo.dwFlags = 0x00000001;
            startupInfo.wShowWindow = 1;


            SECURITY_ATTRIBUTES procAttrs = new();
            SECURITY_ATTRIBUTES threadAttrs = new();
            procAttrs.nLength = Marshal.SizeOf(procAttrs);
            threadAttrs.nLength = Marshal.SizeOf(threadAttrs);

            // Log on user temporarily in order to start console process in its security context.
            DuplicateTokenEx(Token, MAXIMUM_ALLOWED, IntPtr.Zero, SECURITY_IMPERSONATION_LEVEL.SecurityIdentification, TOKEN_TYPE.TokenPrimary, out _);

            CreateEnvironmentBlock(out nint pEnvironmentBlock, Token, false);

            if (CreateProcessAsUser(IntPtr.Zero, null, string.IsNullOrEmpty(Arguments) ? $"\"{Executable}\"" : $"\"{Executable}\" {Arguments}",
                    ref procAttrs, ref threadAttrs, false, (uint)CreationFlags.CREATE_NO_WINDOW |
                                                           (uint)CreationFlags.CREATE_UNICODE_ENVIRONMENT,
                    pEnvironmentBlock, null, ref startupInfo, out PROCESS_INFORMATION _processInfo))
            {
                _ = WaitForSingleObject(_processInfo.hProcess, timeout);
                GetExitCodeProcess(_processInfo.hProcess, out uint exitCode);

                return (int)exitCode;
            }

            return null;
        }
        [LibraryImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool GetExitCodeProcess(IntPtr hProcess, out uint lpExitCode);
        [LibraryImport("kernel32.dll")]
        private static partial uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct STARTUPINFO
        {
            public int cb;
            public IntPtr lpReserved;

            public IntPtr lpDesktop;
            public IntPtr lpTitle;
            public int dwX;
            public int dwY;
            public int dwXSize;
            public int dwYSize;
            public int dwXCountChars;
            public int dwYCountChars;
            public int dwFillAttribute;
            public int dwFlags;
            public short wShowWindow;
            public short cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }
        public static IntPtr GetUserToken()
        {

            WTSQueryUserToken((uint)SessionID, out nint Token);
            return Token;
        }

        private static int SessionID = -1;
        public static bool GetSystemPrivilege()
        {
            OpenProcessToken(Process.GetCurrentProcess().Handle, MAXIMUM_ALLOWED, out nint CurrentProcessToken);
            DuplicateTokenEx(CurrentProcessToken, MAXIMUM_ALLOWED, IntPtr.Zero, SECURITY_IMPERSONATION_LEVEL.SecurityImpersonation, TOKEN_TYPE.TokenImpersonation, out nint DuplicatedCurrentProcessToken);
            LUID_AND_ATTRIBUTES RawPrivilege = new();
            LookupPrivilegeValue(IntPtr.Zero, "SeDebugPrivilege", ref RawPrivilege.Luid);
            RawPrivilege.Attributes = SE_PRIVILEGE_ENABLED;

            TOKEN_PRIVILEGES TokenPrivilege = new()
            {
                Privileges = [RawPrivilege],
                PrivilegeCount = 1
            };
            AdjustTokenPrivileges(DuplicatedCurrentProcessToken, false, ref TokenPrivilege, 0, IntPtr.Zero, IntPtr.Zero);

            SetThreadToken(IntPtr.Zero, DuplicatedCurrentProcessToken);

            SessionID = GetActiveSession();

            nint OriginalProcessToken = new(-1);
            CreateSystemToken((int)MAXIMUM_ALLOWED, SessionID, ref OriginalProcessToken);

            DuplicateTokenEx(OriginalProcessToken, MAXIMUM_ALLOWED, IntPtr.Zero, SECURITY_IMPERSONATION_LEVEL.SecurityImpersonation, TOKEN_TYPE.TokenImpersonation, out nint SystemToken);

            SetThreadToken(IntPtr.Zero, SystemToken);

            return true;
        }

        [LibraryImport("advapi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool GetTokenInformation(
            IntPtr TokenHandle,
            TOKEN_INFORMATION_CLASS TokenInformationClass,
            IntPtr TokenInformation,
            int TokenInformationLength,
            out int ReturnLength);

        private enum TOKEN_INFORMATION_CLASS
        {
            TokenUser = 1,
            TokenGroups,
            TokenPrivileges,
            TokenOwner,
            TokenPrimaryGroup,
            TokenDefaultDacl,
            TokenSource,
            TokenType,
            TokenImpersonationLevel,
            TokenStatistics,
            TokenRestrictedSids,
            TokenSessionId,
            TokenGroupsAndPrivileges,
            TokenSessionReference,
            TokenSandBoxInert,
            TokenAuditPolicy,
            TokenOrigin
        }

        private static int GetActiveSession()
        {
            nint pSessionInfo = IntPtr.Zero;
            int Count = 0;
            int retval = WTSEnumerateSessions((IntPtr)null, 0, 1, ref pSessionInfo, ref Count);
            int dataSize = Marshal.SizeOf<WTS_SESSION_INFO>();

            long current = (long)pSessionInfo;

            int result = -1;
            if (retval != 0)
            {
                for (int i = 0; i < Count; i++)
                {
                    WTS_SESSION_INFO si = Marshal.PtrToStructure<WTS_SESSION_INFO>((nint)current);

                    current += dataSize;

                    if (si.State == WTS_CONNECTSTATE_CLASS.WTSActive)
                    {
                        result = si.SessionID;
                        break;
                    }
                }
                WTSFreeMemory(pSessionInfo);
            }

            return result;
        }

        private static void CreateSystemToken(int DesiredAccess, int dwSessionID, ref IntPtr TokenHandle)
        {
            int dwLsassPID = -1;
            int dwWinLogonPID = -1;
            WTS_PROCESS_INFO[] pProcesses;
            nint pProcessInfo = IntPtr.Zero;

            int dwProcessCount = 0;

            if (WTSEnumerateProcesses((IntPtr)null, 0, 1, ref pProcessInfo, ref dwProcessCount))
            {
                nint pMemory = pProcessInfo;
                pProcesses = new WTS_PROCESS_INFO[dwProcessCount];

                for (int i = 0; i < dwProcessCount; i++)
                {
                    pProcesses[i] = Marshal.PtrToStructure<WTS_PROCESS_INFO>(pProcessInfo);
                    checked
                    {
                        pProcessInfo = (IntPtr)((long)pProcessInfo + Marshal.SizeOf(pProcesses[i]));
                    }

                    string? processName = Marshal.PtrToStringAnsi(pProcesses[i].ProcessName);
                    ConvertSidToStringSid(pProcesses[i].UserSid, out string? sid);

                    if (processName == null || pProcesses[i].UserSid == default || sid != "S-1-5-18")
                    {
                        continue;
                    }

                    if ((-1 == dwLsassPID) && (0 == pProcesses[i].SessionID) && (processName == "lsass.exe"))
                    {
                        dwLsassPID = pProcesses[i].ProcessID;
                        continue;
                    }

                    if ((-1 == dwWinLogonPID) && (dwSessionID == pProcesses[i].SessionID) && (processName == "winlogon.exe"))
                    {
                        dwWinLogonPID = pProcesses[i].ProcessID;
                        continue;
                    }
                }

                WTSFreeMemory(pMemory);
            }

            nint SystemProcessHandle;
            try
            {
                SystemProcessHandle = Process.GetProcessById(dwLsassPID).Handle;
            }
            catch
            {
                SystemProcessHandle = Process.GetProcessById(dwWinLogonPID).Handle;
            }
            if (OpenProcessToken(SystemProcessHandle, TOKEN_DUPLICATE, out nint SystemTokenHandle))
            {
                _ = DuplicateTokenEx(SystemTokenHandle, (uint)DesiredAccess, IntPtr.Zero, SECURITY_IMPERSONATION_LEVEL.SecurityIdentification, TOKEN_TYPE.TokenPrimary, out TokenHandle);
                CloseHandle(SystemTokenHandle);
            }

            CloseHandle(SystemProcessHandle);

            // return Result;
            return;
        }

        [LibraryImport("kernel32.dll")]
        public static partial IntPtr OpenProcess(
            uint processAccess,
            [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle,
            uint processId
        );
        public const uint TOKEN_DUPLICATE = 0x0002;

        [LibraryImport("advapi32", EntryPoint = "ConvertSidToStringSidW", StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool ConvertSidToStringSid(IntPtr pSid, out string strSid);


        [LibraryImport("kernel32.dll")]
        [SuppressUnmanagedCodeSecurity]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool CloseHandle(IntPtr hObject);

        [LibraryImport("wtsapi32.dll")]
        private static partial int WTSEnumerateSessions(
            nint hServer,
            int Reserved,
            int Version,
            ref nint ppSessionInfo,
            ref int pCount);


        [StructLayout(LayoutKind.Sequential)]
        private struct WTS_SESSION_INFO
        {
            public int SessionID;

            [MarshalAs(UnmanagedType.LPStr)]
            public string pWinStationName;

            public WTS_CONNECTSTATE_CLASS State;
        }
        public enum WTS_CONNECTSTATE_CLASS
        {
            WTSActive,
            WTSConnected,
            WTSConnectQuery,
            WTSShadow,
            WTSDisconnected,
            WTSIdle,
            WTSListen,
            WTSReset,
            WTSDown,
            WTSInit
        }
        [LibraryImport("wtsapi32.dll")]
        private static partial void WTSFreeMemory(IntPtr pMemory);

        [LibraryImport("wtsapi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool WTSEnumerateProcesses(
            IntPtr serverHandle, // Handle to a terminal server.
            int reserved,     // must be 0
            int version,      // must be 1
            ref IntPtr ppProcessInfo, // pointer to array of WTS_PROCESS_INFO
            ref int pCount     // pointer to number of processes
        );
#pragma warning disable CS0649
        private struct WTS_PROCESS_INFO
        {
            public int SessionID;
            public int ProcessID;
            //This is a pointer to string...
            public IntPtr ProcessName;
            public IntPtr UserSid;
        }
#pragma warning restore CS0649


        [LibraryImport("ntdll.dll")]
        private static partial IntPtr RtlAdjustPrivilege(int Privilege, [MarshalAs(UnmanagedType.Bool)] bool bEnablePrivilege, [MarshalAs(UnmanagedType.Bool)] bool IsThreadPrivilege, [MarshalAs(UnmanagedType.Bool)] out bool PreviousValue);
        [LibraryImport("advapi32.dll", StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool LookupPrivilegeValue(IntPtr lpSystemName, string lpName, ref ulong lpLuid);
        public static void GetOwnershipPrivilege()
        {
            ulong luid = 0;
            LookupPrivilegeValue(IntPtr.Zero, "SeTakeOwnershipPrivilege", ref luid);
            RtlAdjustPrivilege((int)luid, true, true, out _);
        }
        public static void GetAssignPrivilege()
        {
            ulong luid = 0;
            LookupPrivilegeValue(IntPtr.Zero, "SeAssignPrimaryTokenPrivilege", ref luid);
            RtlAdjustPrivilege((int)luid, true, true, out _);
        }
        public static void GetQuotaPrivilege()
        {
            ulong luid = 0;
            LookupPrivilegeValue(IntPtr.Zero, "SeIncreaseQuotaPrivilege", ref luid);
            RtlAdjustPrivilege((int)luid, true, true, out _);
        }

        public static void GetShutdownPrivilege()
        {
            ulong luid = 0;
            LookupPrivilegeValue(IntPtr.Zero, "SeShutdownPrivilege", ref luid);
            RtlAdjustPrivilege((int)luid, true, true, out _);
        }

        public static void RunAsUser(Action action)
        {
            nint token = NSudo.GetUserToken();
            Task.Run((Action)Delegate.Combine((Action)(() => { NSudo.GetUserPrivilege(token); }),
                action)).Wait();
            Marshal.FreeHGlobal(token);
        }

        private static async Task RunAsUserAsync(Action action)
        {
            nint token = NSudo.GetUserToken();
            await Task.Run((Action)Delegate.Combine((Action)(() => { NSudo.GetUserPrivilege(token); }),
                action));
            Marshal.FreeHGlobal(token);
        }
    }
}