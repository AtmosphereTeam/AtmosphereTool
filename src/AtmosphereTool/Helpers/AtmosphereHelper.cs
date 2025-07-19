using System.Diagnostics;
using System.DirectoryServices.AccountManagement;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace AtmosphereTool.Helpers;
internal class AmelioratedHelper
{

}
public static class SecurityHelper
{
    public static Task<(bool Success, string Error)> ElevateAsync()
    {
        return Task.Run(() =>
        {
            try
            {
                using var context = new PrincipalContext(ContextType.Machine);
                var sid = UserSidHelper.GetLoggedInUserSidFromExplorer();
                if (sid != null)
                {
                    var userPrincipal = UserPrincipal.FindByIdentity(context, IdentityType.Sid, sid.ToString());
                    if (userPrincipal == null)
                    {
                        return (false, "User not found by SID.");
                    }
                    // Add user to Administrators group
                    var group = GroupPrincipal.FindByIdentity(context, "Administrators");
                    if (group != null && !group.Members.Contains(userPrincipal))
                    {
                        group.Members.Add(userPrincipal);
                        group.Save();
                    }
                    // Disable the built-in Administrator account
                    var disableadmin = CommandHelper.StartInCmd("net user Administrator /active:no");
                    if (!(disableadmin.Result.Success))
                    {
                        return (true, "Failed to disable the built-in Administrator account.");
                    }
                    return (true, string.Empty);
                }
                return (false, "Could not retrieve logged-in user SID.");
            }
            catch (Exception ex)
            {
                return (false, ex.Message ?? "Unknown Error");
            }
        });
    }

    public static Task<(bool Success, string Error)> DeElevateAsync()
    {
        return Task.Run(async () =>
        {
            try
            {
                // Enable the built-in Administrator account
                await CommandHelper.StartInCmd("net user Administrator /active:yes");
                using var context = new PrincipalContext(ContextType.Machine);
                var sid = UserSidHelper.GetLoggedInUserSidFromExplorer();
                if (sid == null)
                {
                    return (false, "Could not retrieve logged-in user SID.");
                }
                var userPrincipal = UserPrincipal.FindByIdentity(context, IdentityType.Sid, sid.ToString());
                if (userPrincipal == null)
                {
                    return (false, "User not found by SID.");
                }
                // Enable the built-in Administrator account
                var allUsers = new PrincipalSearcher(new UserPrincipal(context)).FindAll();
                if (allUsers.FirstOrDefault(x => x.Sid.IsWellKnown(WellKnownSidType.AccountAdministratorSid)) is UserPrincipal admin)
                {
                    admin.Enabled = true;
                    admin.Save();
                }
                // Remove user from Administrators
                var group = GroupPrincipal.FindByIdentity(context, "Administrators");
                if (group != null && group.Members.Contains(userPrincipal))
                {
                    group.Members.Remove(userPrincipal);
                    group.Save();
                }
                // Add user to Users group
                var usergroup = GroupPrincipal.FindByIdentity(context, "Users");
                if (usergroup != null && !usergroup.Members.Contains(userPrincipal))
                {
                    usergroup.Members.Add(userPrincipal);
                    usergroup.Save();
                }
                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                return (false, ex.Message ?? "Unknown Error");
            }
        });
    }
}
public static partial class UserSidHelper
{
    // Constants for OpenProcessToken
    private const uint TOKEN_QUERY = 0x0008;

    // Token information class enum
    private enum TOKEN_INFORMATION_CLASS
    {
        TokenUser = 1,
    }

    // Struct for TOKEN_USER
    [StructLayout(LayoutKind.Sequential)]
    private struct TOKEN_USER
    {
        public SID_AND_ATTRIBUTES User;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SID_AND_ATTRIBUTES
    {
        public IntPtr Sid;
        public int Attributes;
    }

    [LibraryImport("advapi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

    [LibraryImport("kernel32.dll")]
    private static partial IntPtr OpenProcess(uint dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, int dwProcessId);

    [LibraryImport("advapi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetTokenInformation(
        IntPtr TokenHandle,
        TOKEN_INFORMATION_CLASS TokenInformationClass,
        IntPtr TokenInformation,
        int TokenInformationLength,
        out int ReturnLength);

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CloseHandle(IntPtr hObject);

    private const uint PROCESS_QUERY_INFORMATION = 0x0400;

    public static SecurityIdentifier? GetLoggedInUserSidFromExplorer()
    {
        try
        {
            // Get explorer process (there could be multiple, take the first)
            var explorerProcess = Process.GetProcessesByName("explorer").Length > 0
                ? Process.GetProcessesByName("explorer")[0]
                : null;
            if (explorerProcess == null)
            {
                return null; // Explorer not found
            }
            var processHandle = OpenProcess(PROCESS_QUERY_INFORMATION, false, explorerProcess.Id);
            if (processHandle != IntPtr.Zero)
            {
                if (!OpenProcessToken(processHandle, TOKEN_QUERY, out var tokenHandle))
                {
                    CloseHandle(processHandle);
                    return null;
                }
                // Get required size for token info
                GetTokenInformation(tokenHandle, TOKEN_INFORMATION_CLASS.TokenUser, IntPtr.Zero, 0, out int tokenInfoLength);
                var tokenInfo = Marshal.AllocHGlobal(tokenInfoLength);
                var result = GetTokenInformation(tokenHandle, TOKEN_INFORMATION_CLASS.TokenUser, tokenInfo, tokenInfoLength, out _);
                if (!result)
                {
                    Marshal.FreeHGlobal(tokenInfo);
                    CloseHandle(tokenHandle);
                    CloseHandle(processHandle);
                    return null;
                }
                // Marshal the TOKEN_USER struct
                var tokenUser = Marshal.PtrToStructure<TOKEN_USER>(tokenInfo);
                var sid = new SecurityIdentifier(tokenUser.User.Sid);
                Marshal.FreeHGlobal(tokenInfo);
                CloseHandle(tokenHandle);
                CloseHandle(processHandle);
                return sid;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}
public static partial class PowerHelper
{
    private const int SystemPowerCapabilities = 4;

    [StructLayout(LayoutKind.Sequential)]
    private struct SYSTEM_POWER_CAPABILITIES
    {
        [MarshalAs(UnmanagedType.U1)] public bool PowerButtonPresent;
        [MarshalAs(UnmanagedType.U1)] public bool SleepButtonPresent;
        [MarshalAs(UnmanagedType.U1)] public bool LidPresent;
        [MarshalAs(UnmanagedType.U1)] public bool SystemS1;
        [MarshalAs(UnmanagedType.U1)] public bool SystemS2;
        [MarshalAs(UnmanagedType.U1)] public bool SystemS3;
        [MarshalAs(UnmanagedType.U1)] public bool SystemS4; // Hibernate supported
        [MarshalAs(UnmanagedType.U1)] public bool SystemS5;
        [MarshalAs(UnmanagedType.U1)] public bool HiberFilePresent; // Hiberfil.sys present
        // Other fields omitted for brevity
    }

    [DllImport("powrprof.dll")]
    private static extern uint CallNtPowerInformation(
        int informationLevel,
        IntPtr lpInputBuffer,
        int nInputBufferSize,
        out SYSTEM_POWER_CAPABILITIES lpOutputBuffer,
        int nOutputBufferSize);

    public static bool IsHibernationEnabled()
    {
        var result = CallNtPowerInformation(
            SystemPowerCapabilities,
            IntPtr.Zero,
            0,
            out SYSTEM_POWER_CAPABILITIES spc,
            Marshal.SizeOf<SYSTEM_POWER_CAPABILITIES>());

        if (result == 0) // STATUS_SUCCESS
        {
            // Check if Hibernate is supported AND hiberfil.sys is present
            return spc.SystemS4 && spc.HiberFilePresent;
        }

        return false; // Could not retrieve info
    }
}