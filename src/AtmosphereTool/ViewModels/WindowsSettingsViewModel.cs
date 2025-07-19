// ViewModels/WindowsSettingsViewModel.cs (Ensure this file name and class name match)
using System.Drawing.Imaging;
using AtmosphereTool.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;
using Windows.Storage;
using Drawing = System.Drawing;
using IO = System.IO;
using WinResLoader = Windows.ApplicationModel.Resources.ResourceLoader;


namespace AtmosphereTool.ViewModels;

// Ensure this matches the ViewModel name you are using in your Page and App.xaml.cs
public partial class WindowsSettingsViewModel : ObservableObject
{
    public WindowsSettingsViewModel()
    {

    }
}
public class WindowsSettingsLocalizationHelper
{
    private readonly WinResLoader _resourceLoader = WinResLoader.GetForViewIndependentUse("WindowsSettingsPage");

    public string GetLocalizedString(string key)
    {
        var localized = _resourceLoader.GetString(key);
        return string.IsNullOrEmpty(localized) ? key : localized;
    }
}
public static class ProfileImageHelper
{
    public static bool IsValidImageFile(string filePath)
    {
        try
        {
            using var img = Drawing.Image.FromFile(filePath);
            return true;
        }
        catch (OutOfMemoryException) // Often thrown for invalid image formats or corrupted files
        {
            return false;
        }
        catch (ArgumentException) // Could be thrown for invalid path or other issues
        {
            return false;
        }
        catch (Exception ex)
        {
            LogHelper.LogError($"Error checking image file '{filePath}': {ex.Message}");
            return false;
        }
    }

    public static async Task<bool> ChangeImageAsync(StorageFile file)
    {
        if (file != null)
        {
            var tempPath = await CopyToTempFileAsync(file);
            return await Task.Run(() => ProcessImage(tempPath));
        }
        return false;
    }

    private static async Task<string> CopyToTempFileAsync(StorageFile sourceFile)
    {
        var tempPath = IO.Path.Combine(IO.Path.GetTempPath(), sourceFile.Name);
        using var sourceStream = await sourceFile.OpenReadAsync();
        using var destStream = IO.File.Open(tempPath, IO.FileMode.Create, IO.FileAccess.Write);
        await sourceStream.AsStreamForRead().CopyToAsync(destStream);
        return tempPath;
    }

    private static async Task<bool> ProcessImage(string imagePath)
    {
        try
        {
            var userSid = RegistryHelper.GetCurrentUserSid();
            // Shut up the compiler
            if (userSid == null)
            {
                return false;
            }
            var pfpDir = IO.Path.Combine(Environment.ExpandEnvironmentVariables(@"%PUBLIC%\AccountPictures"), path2: userSid);
            if (IO.Directory.Exists(pfpDir))
            {
                try
                {
                    IO.Directory.Delete(pfpDir, true);
                }
                catch
                {
                    await CommandHelper.StartInCmd($"takeown /f \"{pfpDir}\" /r /d y");
                    await CommandHelper.StartInCmd($"icacls \"{pfpDir}\" /grant:F Administrators /T");

                }
            }
            IO.Directory.CreateDirectory(pfpDir);

            using var baseImage = Drawing.Image.FromFile(imagePath);

            int[] sizes = [32, 40, 48, 64, 96, 192, 208, 240, 424, 448, 1080];
            RegistryHelper.DeleteKey("HKLM", $@"SOFTWARE\Microsoft\Windows\CurrentVersion\AccountPicture\Users\{userSid}");

            foreach (var res in sizes)
            {
                using var bitmap = new Drawing.Bitmap(res, res);
                using var graphics = Drawing.Graphics.FromImage(bitmap);
                graphics.DrawImage(baseImage, 0, 0, res, res);

                var saveLoc = IO.Path.Combine(pfpDir, $"{res}x{res}.png");
                bitmap.Save(saveLoc, ImageFormat.Png);

                RegistryHelper.AddOrUpdate("HKLM",
                    $@"SOFTWARE\Microsoft\Windows\CurrentVersion\AccountPicture\Users\{userSid}",
                    $"Image{res}",
                    saveLoc,
                    "REG_SZ");
            }

            RegistryHelper.AddOrUpdate("HKLM",
                $@"SOFTWARE\Microsoft\Windows\CurrentVersion\AccountPicture\Users\{userSid}",
                "UserPicturePath",
                IO.Path.Combine(pfpDir, "448x448.png"),
                "REG_SZ");

            try
            {
                var proc = new System.Diagnostics.Process();
                proc.StartInfo.FileName = "gpupdate.exe";
                proc.StartInfo.Arguments = "/force";
                proc.StartInfo.CreateNoWindow = true;
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                proc.Start();
                proc.WaitForExit(20000);
            }
            catch
            {
                // ignore errors
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}