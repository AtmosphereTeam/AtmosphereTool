using System.Security.Principal;
using Microsoft.Win32;

namespace AtmosphereTool.Helpers
{

    public static class RegistryHelper
    {
        public static void AddOrUpdate(string hive, string subKey, string name, object data, string type)
        {
            var baseKey = ParseHive(hive) ?? throw new ArgumentException("Invalid registry hive");
            var valueKind = ParseValueKind(type);

            using var key = baseKey.CreateSubKey(subKey, writable: true);
            if (key != null)
            {
                try
                {
                    var parsedValue = ParseData(data, valueKind);
                    key.SetValue(name, parsedValue, valueKind);
                }
                catch (Exception e)
                {
                    LogHelper.LogCritical($"[RegistryHelper]: Failed to add value: {hive}\\{subKey}\\{name}  Data: {data}  Type: {type} \nError: {e.Message}");
                }
            }
        }

        public static object? Read(string hive, string subKey, string name)
        {
            var baseKey = ParseHive(hive) ?? throw new ArgumentException("Invalid registry hive");
            try
            {
                using var key = baseKey.OpenSubKey(subKey);
                return key?.GetValue(name);
            }
            catch (Exception e)
            {
                LogHelper.LogCritical($"[RegistryHelper]: Failed to read value: {hive}\\{subKey}\\{name} \nError: {e.Message}");
                return null;
            }
        }

        public static bool Exists(string hive, string subKey, string? name = null)
        {
            var baseKey = ParseHive(hive) ?? throw new ArgumentException("Invalid registry hive");
            try
            {
                using var key = baseKey.OpenSubKey(subKey);
                if (key == null) { return false; }
                if (string.IsNullOrEmpty(name)) { return true; }
                var valueNames = key.GetValueNames();
                return valueNames.Contains(name);
            }
            catch (Exception e)
            {
                LogHelper.LogCritical($"[Registry]: Failed to check if registry value: {hive + "\\" + subKey + "\\" + name} \nException: {e.Message}");
                return false;
            }
        }
        public static object? ReadEx(string fullPath, string? name = null)
        {
            // Splits the full path into 3 strings and returns value data
            if (string.IsNullOrWhiteSpace(fullPath))
            {
                LogHelper.LogCritical("[Registry ReadEx]: Registry path is null or empty.");
                return null;
            }
            var firstSlashIndex = fullPath.IndexOf('\\');
            if (firstSlashIndex == -1)
            {
                LogHelper.LogCritical("[Registry ReadEx]: Path is missing a hive and a subkey.");
                return null;
            }
            var hive = fullPath[..firstSlashIndex];
            var rest = fullPath[(firstSlashIndex + 1)..];
            string subKey;
            string valueName;
            if (name == null)
            {
                var lastSlashIndex = rest.LastIndexOf('\\');
                if (lastSlashIndex == -1)
                {
                    LogHelper.LogCritical("[Registry ReadEx]: Path is missing a subkey and a value name.");
                    return null;
                }
                subKey = rest[..lastSlashIndex];
                valueName = rest[(lastSlashIndex + 1)..];
            }
            else
            {
                subKey = rest;
                valueName = name;
            }
            var baseKey = ParseHive(hive) ?? throw new ArgumentException("Invalid registry hive");
            try
            {
                using var key = baseKey.OpenSubKey(subKey);
                return key?.GetValue(valueName);
            }
            catch (Exception e)
            {
                LogHelper.LogError($"[RegistryHelper]: Failed to read value: {hive}\\{subKey}\\{valueName} \nError: {e.Message}");
                return null;
            }
        }
        public static void Delete(string hive, string subKey, string name)
        {
            var baseKey = ParseHive(hive) ?? throw new ArgumentException("Invalid registry hive");
            try
            {
                using var key = baseKey.OpenSubKey(subKey, writable: true);
                key?.DeleteValue(name, throwOnMissingValue: false);
            }
            catch (Exception e)
            {
                LogHelper.LogError($"[RegistryHelper]: Failed to delete value: {hive}\\{subKey}\\{name} \nError: {e.Message}");
            }
        }

        public static void DeleteKey(string hive, string subKey)
        {
            var baseKey = ParseHive(hive) ?? throw new ArgumentException("Invalid registry hive");
            // Split the subKey into its parent path and the key name to be deleted
            var lastBackslashIndex = subKey.LastIndexOf('\\');
            string parentPath;
            string keyNameToDelete;
            if (lastBackslashIndex == -1)
            {
                parentPath = string.Empty;
                keyNameToDelete = subKey;
            }
            else
            {
                parentPath = subKey[..lastBackslashIndex];
                keyNameToDelete = subKey[(lastBackslashIndex + 1)..];
            }
            try
            {
                using var parentKey = baseKey.OpenSubKey(parentPath, writable: true);
                parentKey?.DeleteSubKey(keyNameToDelete, throwOnMissingSubKey: false);
            }
            catch (Exception e)
            {
                LogHelper.LogError($"[RegistryHelper]: Failed to delete key: {hive}\\{subKey} \nError: {e.Message}");
            }
        }
        public static List<string> Search(string hive, string subPath, string valueName)
        {
            var results = new List<string>();
            try
            {
                using var baseKey = ParseHive(hive) ?? throw new ArgumentException("Invalid hive");
                void RecursiveSearch(RegistryKey? key, string[] parts, int index, string currentPath)
                {
                    if (key == null || index >= parts.Length)
                    {
                        return;
                    }
                    var part = parts[index];
                    if (index == parts.Length - 1)
                    {
                        if (part == "*")
                        {
                            foreach (var subName in key.GetSubKeyNames())
                            {
                                using var subKey = key.OpenSubKey(subName);
                                var fullPath = $"{currentPath}\\{subName}";
                                if (subKey != null)
                                {
                                    if (valueName != null)
                                    {
                                        if (subKey.GetValue(valueName) != null)
                                        {
                                            results.Add(fullPath);
                                        }
                                    }
                                    else
                                    {
                                        results.Add(fullPath);
                                    }
                                }
                            }
                        }
                        else
                        {
                            using var subKey = key.OpenSubKey(part);
                            var fullPath = $"{currentPath}\\{part}";
                            if (subKey != null)
                            {
                                if (valueName != null)
                                {
                                    if (subKey.GetValue(valueName) != null)
                                    {
                                        results.Add(fullPath);
                                    }
                                }
                                else
                                {
                                    results.Add(fullPath);
                                }
                            }
                        }
                        return;
                    }
                    if (part == "*")
                    {
                        foreach (var subName in key.GetSubKeyNames())
                        {
                            using var subKey = key.OpenSubKey(subName);
                            RecursiveSearch(subKey, parts, index + 1, $"{currentPath}\\{subName}");
                        }
                    }
                    else
                    {
                        using var subKey = key.OpenSubKey(part);
                        RecursiveSearch(subKey, parts, index + 1, $"{currentPath}\\{part}");
                    }
                }
                var pathParts = subPath.Split('\\', StringSplitOptions.RemoveEmptyEntries);
                RecursiveSearch(baseKey, pathParts, 0, hive); // start path with hive for full path output
            }
            catch (Exception ex)
            {
                LogHelper.LogError("SearchWildcard error: " + ex.Message);
            }
            return results;
        }

        // Helper to map hive strings like "HKCU" to RegistryKey
        private static RegistryKey? ParseHive(string hive) => hive.ToUpper() switch
        {
            "HKCU" or "HKEY_CURRENT_USER" => Registry.CurrentUser,
            "HKLM" or "HKEY_LOCAL_MACHINE" => Registry.LocalMachine,
            "HKCR" or "HKEY_CLASSES_ROOT" => Registry.ClassesRoot,
            "HKU" or "HKEY_USERS" => Registry.Users,
            "HKCC" or "HKEY_CURRENT_CONFIG" => Registry.CurrentConfig,
            _ => null
        };

        // Helper to parse RegistryValueKind from string
        private static RegistryValueKind ParseValueKind(string type) => type.ToUpper() switch
        {
            "REG_SZ" => RegistryValueKind.String,
            "REG_DWORD" => RegistryValueKind.DWord,
            "REG_QWORD" => RegistryValueKind.QWord,
            "REG_BINARY" => RegistryValueKind.Binary,
            "REG_MULTI_SZ" => RegistryValueKind.MultiString,
            "REG_EXPAND_SZ" => RegistryValueKind.ExpandString,
            _ => RegistryValueKind.String
        };

        // Converts string data into the appropriate type
        private static object ParseData(object data, RegistryValueKind kind) => kind switch
        {
            RegistryValueKind.MultiString => data as string[] ?? Array.Empty<string>(),
            RegistryValueKind.DWord => data switch
            {
                int i => i,
                string s when int.TryParse(s, out var i) => i,
                _ => 0
            },
            RegistryValueKind.QWord => data switch
            {
                long l => l,
                string s when long.TryParse(s, out var l) => l,
                _ => 0L
            },
            RegistryValueKind.Binary => data as byte[] ?? Array.Empty<byte>(),
            _ => data
        };

        public static string GetCurrentUserSid()
        {
            if (!AdminHelper.IsAdministrator)
            {
                var user = WindowsIdentity.GetCurrent();
                return user?.User?.Value ?? throw new Exception("Cannot get current user SID");
            }
            else
            {
                var sid = UserSidHelper.GetLoggedInUserSidFromExplorer();
                // Shut up the compiler
                return sid == null ? throw new Exception("Cannot get current user SID") : sid.ToString();
            }
        }
    }
}
