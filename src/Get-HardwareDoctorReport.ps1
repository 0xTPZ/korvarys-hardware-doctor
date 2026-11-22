[CmdletBinding()]
param(
    [string]$OutputPath = (Join-Path (Get-Location) 'hardware-report.json')
)

$ErrorActionPreference = 'SilentlyContinue'

function Get-SafeCimInstances {
    param([string]$ClassName, [string]$Namespace = 'root/cimv2')
    try { @(Get-CimInstance -ClassName $ClassName -Namespace $Namespace) } catch { @() }
}

$os = Get-SafeCimInstances 'Win32_OperatingSystem' | Select-Object -First 1
$computer = Get-SafeCimInstances 'Win32_ComputerSystem' | Select-Object -First 1
$gpus = Get-SafeCimInstances 'Win32_VideoController' | ForEach-Object {
    [ordered]@{
        name = $_.Name
        adapterRamBytes = $_.AdapterRAM
        driverVersion = $_.DriverVersion
        driverDate = $_.DriverDate
        status = $_.Status
        pnpDeviceId = $_.PNPDeviceID
        videoMode = $_.VideoModeDescription
        currentResolution = if ($_.CurrentHorizontalResolution) { "$($_.CurrentHorizontalResolution)x$($_.CurrentVerticalResolution)" } else { $null }
    }
}

$displayEvents = Get-WinEvent -FilterHashtable @{ LogName = 'System'; ProviderName = 'Display'; StartTime = (Get-Date).AddDays(-7) } -MaxEvents 30 |
    ForEach-Object { [ordered]@{ time = $_.TimeCreated; id = $_.Id; level = $_.LevelDisplayName; message = $_.Message } }

$report = [ordered]@{
    schemaVersion = 1
    generatedAtUtc = (Get-Date).ToUniversalTime().ToString('o')
    computer = [ordered]@{ manufacturer = $computer.Manufacturer; model = $computer.Model; systemType = $computer.SystemType }
    operatingSystem = [ordered]@{ caption = $os.Caption; version = $os.Version; build = $os.BuildNumber; architecture = $os.OSArchitecture }
    graphics = @($gpus)
    displayEventsLast7Days = @($displayEvents)
    collection = [ordered]@{ machineName = $env:COMPUTERNAME; powershell = $PSVersionTable.PSVersion.ToString() }
}

$parent = Split-Path -Parent $OutputPath
if ($parent -and -not (Test-Path -LiteralPath $parent)) { New-Item -ItemType Directory -Path $parent -Force | Out-Null }
$report | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $OutputPath -Encoding UTF8
Write-Output "Relatório salvo em: $OutputPath"
