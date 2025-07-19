using System.Diagnostics;
using System.Runtime.InteropServices;
using AtmosphereTool.Helpers;
using AtmosphereTool.Views;
using Microsoft.Win32;

namespace AtmosphereTool.Uninstall
{
    public partial class Deameliorate
    {

        [LibraryImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool TerminateProcess(IntPtr hProcess, uint uExitCode);

        private static string? _mountedPath;
        private static string? _winVer;
        private static string? _isoPath;
        private static readonly bool _win11 = Environment.OSVersion.Version.Build >= 22000;

        private readonly string UserSID = RegistryHelper.GetCurrentUserSid() ?? string.Empty;

        public async Task<bool> DeameliorateCore(UninstallProgressPage progressPage, string? path = null)
        {
            if (path != null)
            {
                if (File.Exists(path))
                {
                    var startInfo = new ProcessStartInfo
                    {
                        CreateNoWindow = false,
                        UseShellExecute = false,
                        FileName = "PowerShell.exe",
                        WindowStyle = ProcessWindowStyle.Hidden,
                        Arguments = $"-NoP -C \"(Mount-DiskImage '{path}' -PassThru | Get-Volume).DriveLetter + ':\'\"",
                        RedirectStandardOutput = true
                    };

                    var proc = Process.Start(startInfo);
                    if (proc == null)
                    {
                        return false;
                    }
                    proc.WaitForExit();

                    _mountedPath = proc.StandardOutput.ReadLine();
                }
                else
                {
                    _mountedPath = path;
                }
            }
            else
            {
                LogHelper.LogInfo("Checking Iso");
                progressPage.AddStatus("Checking Iso");
                var swi = new SelectWindowsImage();
                (_mountedPath, _isoPath, _winVer, _, _) = await swi.GetMediaPath();
                if (_mountedPath == null)
                {
                    return false;
                }
            }
            progressPage.AddStatus("Restoring Defender");
            LogHelper.LogInfo("[UninstallAtmosphere]: Restoring Defender");
            await CommandHelper.RunProcess("DISM.exe", $"/Online /Remove-Package /PackageName:\"Z-AME-NoDefender-Package~31bf3856ad364e35~amd64~~1.0.0.0\" /NoRestart");
            await CommandHelper.RunProcess("DISM.exe", $"/Online /Remove-Package /PackageName:\"Z-Atmosphere-NoTelemetry-Package31bf3856ad364e35amd645.0.0.0.cab\" /NoRestart");
            progressPage.SetProgress(15);
            var progress = 15;

            progressPage.AddStatus("Uninstalling Software");
            LogHelper.LogInfo("[UninstallAtmosphere]: Uninstalling software");
            var options = RegistryHelper.Read("HKLM", "SOFTWARE\\AME\\Playbooks\\Applied\\{8bbb362c-858b-41d9-a9ea-83a4b9669c43}", "SelectedOptions") as string[] ?? Array.Empty<string>();
            if (options != null)
            {
                // Appx
                if (options.Contains("install-files"))
                {
                    progressPage.AddStatus("Uninstalling Files");
                    LogHelper.LogInfo("[UninstallAtmosphere]: Uninstalling Files");
                    await CommandHelper.StartInPowershell("Get-AppxPackage *Files* | Remove-AppxPackage");
                    progress += 5;
                    progressPage.SetProgress(progress);
                }
                if (options.Contains("install-notepads"))
                {
                    progressPage.AddStatus("Uninstalling Notepads");
                    LogHelper.LogInfo("[UninstallAtmosphere]: Uninstalling Notepads");
                    await CommandHelper.StartInPowershell("Get-AppxPackage *JackieLiu.Notepads* | Remove-AppxPackage");
                    progress += 5;
                    progressPage.SetProgress(progress);
                }
                if (options.Contains("install-fluentterminal"))
                {
                    progressPage.AddStatus("Uninstalling Fluent Terminal");
                    LogHelper.LogInfo("[UninstallAtmosphere]: Uninstalling Fluent Terminal");
                    await CommandHelper.StartInPowershell("Get-AppxPackage *Apps.FluentTerminal* | Remove-AppxPackage");
                    progress += 5;
                    progressPage.SetProgress(progress);
                }
                if (options.Contains("install-unigetui"))
                {
                    progressPage.AddStatus("Uninstalling UniGetUi");
                    LogHelper.LogInfo("[UninstallAtmosphere]: Uninstalling UniGetUi");
                    if (RegistryHelper.Read("HKU", $"{UserSID}\\Volatile Environment", "LOCALAPPDATA") is string appdata)
                    {
                        var unigetui = Path.Combine(appdata, "Programs\\UniGetUI");
                        var uninstaller = Path.Combine(unigetui, "unins000.exe");
                        if (Path.Exists(uninstaller))
                        {
                            await CommandHelper.RunProcess(uninstaller, " /SILENT");
                        }
                    }
                    progress += 5;
                    progressPage.SetProgress(progress);
                }
                // Software
                if (options.Contains("openshell"))
                {
                    progressPage.AddStatus("Uninstalling Open-Shell");
                    LogHelper.LogInfo("[UninstallAtmosphere]: Uninstalling Open-Shell");
                    try
                    {
                        string? openShellId = null;
                        var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
                        if (key != null) // Shut up the compiler
                        {
                            foreach (var item in key.GetSubKeyNames())
                            {
                                try
                                {
                                    if ((key.OpenSubKey(item)?.GetValue("DisplayName") as string) == "Open-Shell")
                                    {
                                        openShellId = item;
                                    }
                                }
                                catch
                                {
                                    // do nothing
                                }
                            }
                            if (openShellId != null)
                            {

                                foreach (var process in Process.GetProcessesByName("explorer"))
                                {
                                    try
                                    {
                                        TerminateProcess(process.Handle, 1);
                                    }
                                    catch
                                    {
                                    }
                                }
                                Process.Start("MsiExec.exe", $"/X{openShellId} /quiet")?.WaitForExit();
                                if (UserSID != null)
                                {
                                    var appData = RegistryHelper.Read("HKU", $"{UserSID}\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Shell Folders", "AppData") as string;
                                    if (Directory.Exists(Path.Combine(appData ?? "NULL:", "OpenShell")))
                                    {
                                        Directory.Delete(Path.Combine(appData!, "OpenShell"), true);
                                    }
                                }
                                progress += 5;
                                progressPage.SetProgress(progress);
                            }
                            else
                            {
                                LogHelper.LogCritical("[UninstallAtmosphere]: No Open-Shell Uninstall Registry Key");
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        LogHelper.LogError("[UninstallAtmosphere]: Error while uninstalling Open-Shell: " + e.Message);
                    }
                }
                if (options.Contains("mitigations-disable"))
                {
                    progressPage.AddStatus("Enabling Windows Default Mitigations");
                    LogHelper.LogInfo("[UninstallAtmosphere]: Enabling Windows Default Mitigations");
                    RegistryHelper.Delete("HKLM", "SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management", "FeatureSettingsOverride");
                    RegistryHelper.Delete("HKLM", "SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management", "FeatureSettingsOverrideMask");
                    RegistryHelper.Delete("HKLM", "SYSTEM\\CurrentControlSet\\Control\\Session Manager\\kernel", "DisableExceptionChainValidation");
                    RegistryHelper.Delete("HKLM", "SYSTEM\\CurrentControlSet\\Control\\Session Manager\\kernel", "MitigationAuditOptions");
                    RegistryHelper.Delete("HKLM", "SYSTEM\\CurrentControlSet\\Control\\Session Manager\\kernel", "MitigationOptions");
                    RegistryHelper.Delete("HKLM", "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Virtualization", "MinVmVersionForCpuBasedMitigations");
                    RegistryHelper.AddOrUpdate("HKLM", "HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager", "ProtectionMode", 1, "REG_DWORD");
                    await CommandHelper.RunProcess("bcdedit", "bcdedit /set nx OptIn");
                }
            }
            progressPage.SetProgress(40);
            progressPage.AddStatus("Undoing Registry Edits");
            // Automatic Updates
            progressPage.AddStatus("Enabling Automatic Updates");
            LogHelper.LogInfo("[UninstallAtmosphere]: Enabling Automatic Updates");
            RegistryHelper.Delete("HKLM", "SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU", "AUOptions");

            // Power Schemes and Hibernation
            progressPage.AddStatus("Restting Power Schemes");
            LogHelper.LogInfo("[UninstallAtmosphere]: Resetting Power schemes");
            await CommandHelper.RunProcess("powercfg", "-restoredefaultschemes");
            await CommandHelper.RunProcess("powercfg", "/hibernate on");

            // Core Isolation
            // If statements checking if the key exists
            progressPage.AddStatus("Resetting VBS");
            LogHelper.LogInfo("[UninstallAtmosphere]: Resetting VBS");
            if (RegistryHelper.Exists("HKLM", "SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\\Scenarios\\HypervisorEnforcedCodeIntegrity"))
            {
                RegistryHelper.AddOrUpdate("HKLM", "SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\\Scenarios\\HypervisorEnforcedCodeIntegrity", "Enabled", 1, "REG_DWORD");
                RegistryHelper.AddOrUpdate("HKLM", "SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\\Scenarios\\HypervisorEnforcedCodeIntegrity", "WasEnabledBy", 2, "REG_DWORD");
            }
            if (RegistryHelper.Exists("HKLM", "SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\\Scenarios\\KernelShadowStacks"))
            {
                RegistryHelper.AddOrUpdate("HKLM", "SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\\Scenarios\\KernelShadowStacks", "Enabled", 1, "REG_DWORD");
                RegistryHelper.AddOrUpdate("HKLM", "SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\\Scenarios\\KernelShadowStacks", "WasEnabledBy", 2, "REG_DWORD");
            }
            if (RegistryHelper.Exists("HKLM", "SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\\Scenarios\\CredentialGuard"))
            {
                RegistryHelper.AddOrUpdate("HKLM", "SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\\Scenarios\\CredentialGuard", "Enabled", 1, "REG_DWORD");
                RegistryHelper.AddOrUpdate("HKLM", "SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\\Scenarios\\CredentialGuard", "WasEnabledBy", 2, "REG_DWORD");
            }
            progressPage.SetProgress(45);
            // Automatic Maintenance
            progressPage.AddStatus("Enabling Automatic Maintenance");
            LogHelper.LogInfo("[UninstallAtmosphere]: Enabling Automatic Maintenance");
            RegistryHelper.AddOrUpdate("HKLM", "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Schedule\\Maintenance", "MaintenanceDisabled", 0, "REG_DWORD");
            RegistryHelper.AddOrUpdate("HKLM", "SOFTWARE\\Microsoft\\Windows\\ScheduledDiagnostics", "EnabledExecution", 1, "REG_DWORD");
            // New Context Menu
            progressPage.AddStatus("Restoring Context Menus");
            LogHelper.LogInfo("[UninstallAtmosphere]: Restoring Context Menus");
            RegistryHelper.DeleteKey("HKU", UserSID + "\\Software\\Classes\\CLSID\\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}");
            // Disable Enhanced Security
            progressPage.AddStatus("Disabling Enhanced Security");
            LogHelper.LogInfo("[UninstallAtmosphere]: Disabling Enhanced Security");
            await SecurityHelper.ElevateAsync();
            // UI Modifications
            progressPage.AddStatus("Removing UI Modifications");
            LogHelper.LogInfo("[UninstallAtmosphere]: Removing UI Modifications");
            await CommandHelper.StartInCmd("regsvr32 /u \"C:\\Windows\\AtmosphereDesktop\\4. Interface Tweaks\\File Explorer Customization\\Mica Explorer\\ExplorerBlurMica.dll\"");
            await CommandHelper.RunProcess("Rundll32.exe", "\"C:\\Windows\\AtmosphereModules\\Tools\\TranslucentFlyouts\\TFMain64.dll,Main /stop\"");
            await CommandHelper.StartInPowershell("Unregister-ScheduledTask -TaskName \"AccentColorizer\" -Confirm:$false -ErrorAction SilentlyContinue");
            // OEM Information
            progressPage.AddStatus("Removing OEM Information");
            LogHelper.LogInfo("[UninstallAtmosphere]: Removing OEM Information");
            RegistryHelper.Delete("HKLM", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\OEMInformation", "Model");
            RegistryHelper.Delete("HKLM", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\OEMInformation", "Manufacturer");
            RegistryHelper.Delete("HKLM", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\OEMInformation", "SupportURL");
            RegistryHelper.Delete("HKLM", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\OEMInformation", "SupportPhone");
            RegistryHelper.Delete("HKLM", "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion", "RegisteredOrganization");
            var (OSName, _) = LogHelper.GetWinVersion();
            OSName ??= "Windows";
            await CommandHelper.RunProcess("bcdedit", $"/set description \"{OSName.Trim()}\"");
            progressPage.SetProgress(55);
            // Un-Deprovision packages
            progressPage.AddStatus("Un-Deprovisioning Packages");
            LogHelper.LogInfo("[UninstallAtmosphere]: Un-Deprovisioning Packages");
            RegistryHelper.DeleteKey("HKLM", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Appx\\AppxAllUserStore\\Deprovisioned");
            // Remove shortcuts
            progressPage.AddStatus("Removing Shortcuts");
            LogHelper.LogInfo("[UninstallAtmosphere]: Removing Shortcuts");
            var users = Directory.GetDirectories("C:\\Users");
            foreach (var user in users)
            {
                var startupdir = Path.Combine(user, "AppData\\Roaming\\Microsoft\\Windows\\Start Menu\\Programs\\Startup");
                var desktopdir = Path.Combine(user, "Desktop");
                if (Directory.Exists(startupdir))
                {
                    var tflink = Path.Combine(startupdir, "TranslucentFlyouts.lnk");
                    var tkcdrlnk = Path.Combine(startupdir, "Taskkill_CDR.lnk");
                    var atmuserlnk = Path.Combine(startupdir, "AtmosphereUser.lnk");
                    if (File.Exists(tflink)) { File.Delete(tflink); }
                    if (File.Exists(tkcdrlnk)) { File.Delete(tkcdrlnk); }
                    if (File.Exists(atmuserlnk)) { File.Delete(atmuserlnk); }
                }
                if (Directory.Exists(desktopdir))
                {
                    var atmdesklnk = Path.Combine(desktopdir, "Atmosphere.lnk");
                    if (File.Exists(atmdesklnk)) { File.Delete(atmdesklnk); }
                }
            }
            progressPage.SetProgress(60);
            // all policies are cleared as a user that's de-ameliorating is unlikely to have their own policies in the first place
            progressPage.AddStatus("Clearing policies");
            LogHelper.LogInfo("[UninstallAtmosphere]: Clearing policies");
            RegistryHelper.DeleteKey("HKLM", "SOFTWARE\\Atmosphere");
            foreach (var keyPath in new[]
                     {
                         $@"HKU\{UserSID}\Software\Microsoft\Windows\CurrentVersion\Policies",
                         $@"HKU\{UserSID}\Software\Policies",
                         @"HKLM\Software\Microsoft\Windows\CurrentVersion\Policies",
                         @"HKLM\Software\Policies",
                         @"HKLM\Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Policies",
                         @"HKLM\Software\AME\Playbooks\Applied\{9010E718-4B54-443F-8354-D893CD50FDDE}",
                     })
            {
                var hive = RegistryHive.LocalMachine;
                if (keyPath.StartsWith("HKU"))
                {
                    hive = RegistryHive.Users;
                }
                var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Default);
                var subKeyPath = keyPath[(keyPath.IndexOf('\\') + 1)..];
                var key = baseKey.OpenSubKey(subKeyPath, true);
                if (key == null)
                {
                    continue;
                }
                try
                {
                    baseKey.DeleteSubKeyTree(subKeyPath);
                }
                catch
                {
                    // do nothing - some values might fail, but almost all are deleted
                }

                key.Close();
            }
            progressPage.SetProgress(80);

            await CommandHelper.RunProcess("explorer");
            // Restore Services
            progressPage.AddStatus("Restoring Services");
            LogHelper.LogInfo("[UninstallAtmosphere]: Restoring Services");
            await CommandHelper.StartInCmd("reg import \"C:\\Windows\\AtmosphereModules\\Other\\winServices.reg\"", true, true, true);

            // Reset Network
            progressPage.AddStatus("Resetting Network Config");
            LogHelper.LogInfo("[UninstallAtmosphere]: Restting IP Config");
            await CommandHelper.RunProcess("netsh", "int ip reset");
            await CommandHelper.RunProcess("netsh", "interface ipv4 reset");
            await CommandHelper.RunProcess("netsh", "interface ipv6 reset");
            await CommandHelper.RunProcess("netsh", "interface tcp reset");
            await CommandHelper.RunProcess("netsh", "winsock reset");
            await CommandHelper.StartInPowershell("foreach ($dev in Get-PnpDevice -Class Net -Status 'OK') { pnputil /remove-device $dev.InstanceId }");
            await CommandHelper.RunProcess("pnputil", "/scan-devices");
            progressPage.AddStatus("Initiating Windows setup for file restoration");
            progressPage.SetProgress(90);
            try
            {
                if (_mountedPath != null) // Shut up the compiler
                {
                    Process.Start(Path.Combine(_mountedPath, "setup.exe"), $"/Auto Upgrade /DynamicUpdate Disable");
                }

            }
            catch (Exception e)
            {
                LogHelper.LogError($"There was an error when trying to run the Windows Setup: {e}");
                progressPage.ShowErrorDialog("Error", $"There was an error when trying to run the Windows Setup: {e}\nTry running the Windows Setup manually from File Explorer.");
                return false;
            }
            // Cleanup script
            var appdatapth = RegistryHelper.Read("HKU", $"{UserSID}\\Volatile Environment", "APPDATA") as string ?? string.Empty;
            if (appdatapth != string.Empty)
            {
                var roamingappdata = Path.Combine(appdatapth, "Microsoft\\Windows\\Start Menu\\Programs\\Startup\\Cleanup.cmd");
                var cleanupscript = "C:\\Windows\\AtmosphereModules\\AtmosphereTool\\Uninstall\\Cleanup.cmd";
                if (File.Exists(cleanupscript)) 
                {
                    File.Move(cleanupscript, roamingappdata);
                }
                else
                {
                    try { File.Move(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Uninstall\\Cleanup.cmd"), roamingappdata); } catch { /* ignore */ }
                }
            }
            progressPage.AddStatus("Windows setup has begun, accept the license to begin restoring system files. Your system will restart.");
            progressPage.SetProgress(100);
            LogHelper.LogInfo("[UninstallAtmosphere]: Windows Setup started");

            return true;
        }
    }
}