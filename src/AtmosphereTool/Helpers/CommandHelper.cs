using System.Diagnostics;
using System.Security.Principal;

namespace AtmosphereTool.Helpers
{
    public static class CommandHelper
    {
        public static async Task<(bool Success, string? Log, string? Error)> StartInCmd(string command, bool wait = true, bool file = false, bool hidden = true)
        {
            var mode = file == true ? "file: " : "process: ";
            LogHelper.LogInfo("[RunInCmd]: Running " + mode + command);
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = file ? $"/c \"{command}\"" : $"/c {command}",
                    UseShellExecute = false,
                    CreateNoWindow = hidden,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                using var process = Process.Start(psi);
                if (process == null)
                {
                    LogHelper.LogWarning($"[StartInCmd]: Failed to run command: \"{command}\" .");
                    return (false, null, $"Failed to run command: \"{command}\" .");
                }
                if (wait == true)
                {
                    var output = await process.StandardOutput.ReadToEndAsync();
                    var error = await process.StandardError.ReadToEndAsync();
                    await process.WaitForExitAsync();
                    if (process.ExitCode == 0)
                    {
                        LogHelper.LogInfo("[RunProcess]: Process finishised with exit code 0");
                        LogHelper.LogInfo("[RunProcess]: Log: " + output.Trim() + "Errors: " + error.Trim());
                        return (true, string.IsNullOrWhiteSpace(output) ? null : output.Trim(), null);
                    }
                    else
                    {
                        LogHelper.LogWarning("[RunProcess]: Process exited with code: " + process.ExitCode + " . \nErrors: " + error.Trim() + " . \nOutput" + output.Trim());
                        return (false, null, error?.Trim() ?? $"Process exited with code {process.ExitCode}");
                    }
                }
                return (true, null, null);
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"[RunInCmd]: Failed to run command: {ex.Message}");
                return (false, null, ex.Message);
            }
        }

        public static bool IsRunningAsAdministrator()
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        public static async Task<(bool Success, string? Error)> StartInPowershell(string command, bool hidden = true, bool file = false, bool RunAsTI = false)
        {
            var mode = file == true ? "file: " : "process: ";
            var privilege = RunAsTI == true ? " As Trusted Installer" : " As Administrator";
            LogHelper.LogInfo("[RunInPowershell]: Running " + mode + command + privilege);
            if (RunAsTI == true)
            {
                if (file == true)
                {
                    RunAsTi.RunAsTrustedInstaller($"powershell.exe -NoProfile -ExecutionPolicy Bypass -File \"{command}\"");
                    return (true, null);
                }
                if (file == false)
                {
                    RunAsTi.RunAsTrustedInstaller($"powershell.exe -Command {command}");
                    return (true, null);
                }
            }
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-Command {command}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = hidden,
                };
                if (file == true)
                {
                    psi.Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{command}\"";
                }
                using var process = Process.Start(psi);
                // Shut up the compiler
                if (process == null)
                {
                    return (false, "[RunInPowershell]: Failed to start PowerShell process.");
                }
                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();
                if (process.ExitCode == 0)
                {
                    return (true, string.IsNullOrWhiteSpace(output) ? null : output.Trim());
                }
                else
                {
                    LogHelper.LogError("[RunInPowershell]: Failed to run " + mode + command + privilege);
                    return (false, error?.Trim() ?? $"Process exited with code {process.ExitCode}");
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"[RunInPowershell]: error: {ex.Message}");
                return (false, ex.Message);
            }
        }
        public static async Task<(bool Success, string? Log, string? Error)> RunProcess(string path, string? args = null, bool wait = true)
        {
            LogHelper.LogInfo("[RunProcess]: Running process: " + path + " With args: " + args + " Wait = " + wait);
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = path,
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                using var process = Process.Start(psi);
                if (process == null)
                {
                    LogHelper.LogWarning($"[RunProcess]: Failed to start \"{path}\" process.");
                    return (false, null, $"Failed to start \"{path}\" process.");
                }
                // Log and wait
                if (wait == true)
                {
                    var output = await process.StandardOutput.ReadToEndAsync();
                    var error = await process.StandardError.ReadToEndAsync();
                    await process.WaitForExitAsync();
                    if (process.ExitCode == 0)
                    {
                        LogHelper.LogInfo("[RunProcess]: Process finishised with exit code 0");
                        LogHelper.LogInfo("[RunProcess]: Log: " + output.Trim() + "Errors: " + error.Trim());
                        return (true, string.IsNullOrWhiteSpace(output) ? null : output.Trim(), null);
                    }
                    else
                    {
                        LogHelper.LogWarning("[RunProcess]: Process exited with code: " + process.ExitCode + " . \nErrors: " + error.Trim() + " . \nOutput" + output.Trim());
                        return (false, null, error?.Trim() ?? $"Process exited with code {process.ExitCode}");
                    }
                }
                return (true, null, null);
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"[RunProcess]: error: {ex.Message}");
                return (false, null, ex.Message);
            }
        }
    }
}

