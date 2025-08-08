Write-Host "Downloading AtmosphereTool..."
$windir = [Environment]::GetFolderPath('Windows')
& "$windir\AtmosphereModules\initPowerShell.ps1"
function Remove-TempDirectory { Pop-Location; Remove-Item -Path $tempDir -Force -Recurse -EA 0 }
$tempDir = Join-Path -Path $(Get-SystemDrive) -ChildPath $([System.Guid]::NewGuid())
New-Item $tempDir -ItemType Directory -Force | Out-Null
Push-Location $tempDir
try {
        if (-not ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
        Write-Host "Requesting Administrator Access" -ForegroundColor Yellow
        Start-Process powershell -ArgumentList "-ExecutionPolicy Bypass -File `"$PSCommandPath`"" -Verb RunAs
        exit
    }
    $githubApi = Invoke-RestMethod "https://api.github.com/repos/AtmosphereTeam/AtmosphereTool/releases" -ErrorAction Stop
    $zipUrl = $githubApi.assets.browser_download_url | Where-Object { $_ -like "*.zip" } | Select-Object -First 1
    if (-not $zipUrl) {
        throw "Failed to find a .zip asset in the release."
    }
    $zipPath = Join-Path $tempDir "AtmosphereTool.zip"
    & curl.exe -L $zipUrl -o $zipPath
    Stop-Process -Name "AtmosphereTool" -Force -ErrorAction SilentlyContinue
    while (Get-Process -Name "AtmosphereTool" -ErrorAction SilentlyContinue) {
        Start-Sleep -Milliseconds 200
    }
    $atmospherePath = "C:\Program Files\AtmosphereTool"
    Write-Host "Installing AtmosphereTool..."
    if (Test-Path $atmospherePath) {
        Remove-Item -Path $atmospherePath -Force -Recurse -ErrorAction Stop
    }
    Expand-Archive -Path $zipPath -DestinationPath $atmospherePath -Force
    Write-Host "AtmosphereTool installation complete."
}
catch {
    Write-Error "An error occurred: $_"
}
finally {
    Remove-TempDirectory
    Read-Host "Press any key to exit..."
}