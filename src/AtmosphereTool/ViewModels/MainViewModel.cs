using System.Diagnostics;
using System.Management;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Windows.ApplicationModel.Resources;

namespace AtmosphereTool.ViewModels;

public partial class MainViewModel : ObservableRecipient
{
    private readonly ResourceLoader resourceLoader;
    private readonly DispatcherTimer _timer = new();
    public string CpuName => GetCpuName();
    public string GpuName => GetGpuName();
    public string InstalledMemory => GetInstalledMemory();
    public string DiskName => GetDiskName();
    private string _cpuUsage;
    public string CpuUsage
    {
        get => _cpuUsage;
        set => SetProperty(ref _cpuUsage, value);
    }

    private string _ramUsage;
    public string RamUsage
    {
        get => _ramUsage;
        set => SetProperty(ref _ramUsage, value);
    }
    private string _systemUptime;
    public string SystemUptime
    {
        get => _systemUptime;
        set => SetProperty(ref _systemUptime, value);
    }
    private string _gpuUsage;
    public string GpuUsage
    {
        get => _gpuUsage;
        set => SetProperty(ref _gpuUsage, value);
    }
    private string _diskUsage;
    public string DiskUsage
    {
        get => _diskUsage;
        set => SetProperty(ref _diskUsage, value);
    }

    private float LastCpuUsage;
    private float LastGpuUsage;
    private float LastRamUsage;
    private float NewCpuUsage;
    private float NewGpuUsage;
    private float NewRamUsage;

    private readonly PerformanceCounter? _cpuCounter = new("Processor", "% Processor Time", "_Total");

    public MainViewModel()
    {
        resourceLoader = ResourceLoader.GetForViewIndependentUse();
        var loadingText = resourceLoader.GetString("Loading");
        _cpuUsage = _ramUsage = _systemUptime = _gpuUsage = _diskUsage = loadingText;
        SystemUptime = GetSystemUptime();
        DiskUsage = GetDiskUsage();
        OnPropertyChanged(nameof(SystemUptime));
        OnPropertyChanged(nameof(DiskUsage));
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += (s, e) => UpdateStats();
        _timer.Start();
    }
    private async void UpdateStats()
    {
        LastCpuUsage = GetCpuUsage();
        LastGpuUsage = await GetGpuUsageAsync();
        LastRamUsage = GetRamUsage();
        NewCpuUsage = GetCpuUsage();
        NewGpuUsage = await GetGpuUsageAsync();
        NewRamUsage = GetRamUsage();
        CpuUsage = $"{GetCpuUsage():0.0}%";
        var gpu = await GetGpuUsageAsync();
        GpuUsage = $"{gpu}%";
        RamUsage = $"{GetRamUsage():0.0}%";
        // SystemUptime = GetSystemUptime();
        // DiskUsage = GetDiskUsage();
        // OnPropertyChanged(nameof(DiskUsage));
        if (LastCpuUsage != NewCpuUsage) { OnPropertyChanged(nameof(CpuUsage)); }
        if (LastGpuUsage != NewGpuUsage) { OnPropertyChanged(nameof(GpuUsage)); }
        if (LastRamUsage != NewRamUsage) { OnPropertyChanged(nameof(RamUsage)); }
        // OnPropertyChanged(nameof(SystemUptime));

    }
    public static string GetCpuName()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("select Name from Win32_Processor");
            foreach (var obj in searcher.Get())
                return obj["Name"]?.ToString()?.Trim() ?? "Unknown CPU";
        }
        catch { }
        return "N/A";
    }
    public static string GetGpuName()
    {
        try
        {
            var searcher = new ManagementObjectSearcher("select Name, AdapterCompatibility from Win32_VideoController");
            string? preferredGpu = null;

            foreach (ManagementObject mo in searcher.Get().Cast<ManagementObject>())
            {
                var name = mo["Name"]?.ToString() ?? "";
                var vendor = mo["AdapterCompatibility"]?.ToString() ?? "";

                // Skip known virtual adapters by keywords
                if (name.Contains("Virtual") || name.Contains("Display") && name.Contains("Adapter"))
                    continue;

                // Prefer NVIDIA, AMD, Intel
                if (vendor.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase) ||
                    vendor.Contains("AMD", StringComparison.OrdinalIgnoreCase) ||
                    vendor.Contains("Intel", StringComparison.OrdinalIgnoreCase))
                {
                    preferredGpu = name;
                    break;
                }

                // If no preferred vendor found yet, fallback to first physical card found
                preferredGpu ??= name;
            }

            return preferredGpu ?? "Unknown GPU";
        }
        catch
        {
            return "Unknown GPU";
        }
    }
    public static string GetInstalledMemory()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("select Capacity from Win32_PhysicalMemory");
            ulong total = 0;
            foreach (var obj in searcher.Get())
                if (ulong.TryParse(obj["Capacity"]?.ToString(), out var cap))
                    total += cap;

            return $"{total / (1024 * 1024 * 1024)} GB";
        }
        catch { }
        return "N/A";
    }
    public static string GetDiskName()
    {
        try
        {
            var searcher = new ManagementObjectSearcher("SELECT Model, MediaType FROM Win32_DiskDrive WHERE MediaType != NULL");
            foreach (ManagementObject disk in searcher.Get().Cast<ManagementObject>())
            {
                var model = disk["Model"]?.ToString() ?? "";
                var mediaType = disk["MediaType"]?.ToString() ?? "";

                // Skip virtual disks or CD-ROM drives by MediaType
                if (mediaType.Contains("Fixed", StringComparison.OrdinalIgnoreCase))
                {
                    return model; // Return the first fixed physical disk found
                }
            }
        }
        catch
        {
            // Handle exceptions if needed
        }
        return "Unknown Disk";
    }
    private float GetCpuUsage()
    {
        return _cpuCounter?.NextValue() ?? 0;
    }
    private static float GetRamUsage()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT TotalVisibleMemorySize, FreePhysicalMemory FROM Win32_OperatingSystem");
            foreach (ManagementObject obj in searcher.Get().Cast<ManagementObject>())
            {
                var total = Convert.ToUInt64(obj["TotalVisibleMemorySize"]);
                var free = Convert.ToUInt64(obj["FreePhysicalMemory"]);
                return 100f - ((float)free / total * 100f);
            }
        }
        catch { }
        return 0;
    }
    public static string GetSystemUptime()
    {
        var uptime = TimeSpan.FromMilliseconds(Environment.TickCount64);
        return $"{(int)uptime.TotalHours}h {uptime.Minutes}m";
    }
    public static async Task<int> GetGpuUsageAsync()
    {
        try
        {
            var category = new PerformanceCounterCategory("GPU Engine");
            var counterNames = category.GetInstanceNames();
            var gpuCounters = new List<PerformanceCounter>();
            var result = 0f;
            foreach (var counterName in counterNames)
            {
                if (counterName.EndsWith("engtype_3D"))
                {
                    foreach (var counter in category.GetCounters(counterName))
                    {
                        if (counter.CounterName == "Utilization Percentage")
                        {
                            gpuCounters.Add(counter);
                        }
                    }
                }
            }
            // Warm-up read
            gpuCounters.ForEach(x => _ = x.NextValue());
            await Task.Delay(1000); // Allow counter to collect data
            gpuCounters.ForEach(x => result += x.NextValue());
            return (int)result;
        }
        catch
        {
            return 0;
        }
    }
    private static string GetDiskUsage()
    {
        try
        {
            var drive = DriveInfo.GetDrives().FirstOrDefault(d => d.IsReady && d.Name == Path.GetPathRoot(Environment.SystemDirectory));
            if (drive == null) return "N/A";
            var total = drive.TotalSize;
            var free = drive.TotalFreeSpace;
            var used = total - free;
            static string Format(long bytes) => $"{bytes / 1_000_000_000.0:0.##} GB";  // Decimal GB format
            var percent = ((float)used / total) * 100f;
            return $"{Format(used)} / {Format(total)} ({percent:0}%)";
        }
        catch
        {
            return "N/A";
        }
    }


}
