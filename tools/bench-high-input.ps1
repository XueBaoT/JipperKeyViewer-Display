<#
.SYNOPSIS
  高速输入 CPU/内存对照测试。启动指定 exe 的临时副本，聚焦显示窗口，
  以固定频率发送已注册键，采样 Process CPU/工作集/私有内存。
.NOTES
  运行前请关闭正在运行的 JipperKeyViewer，避免单实例互斥导致脚本误判。
#>
param(
    [Parameter(Mandatory = $true)][string]$Exe,
    [string]$ConfigPath = "$env:USERPROFILE\Desktop\JipperKeyViewer\config\config.json",
    [string]$GlobalPath = "$env:USERPROFILE\Desktop\JipperKeyViewer\config\global.json",
    [int]$Seconds = 15,
    [int]$KeysPerSecond = 30,
    [switch]$Overlay
)

$ErrorActionPreference = 'Stop'
if (Get-Process JipperKeyViewer -ErrorAction SilentlyContinue) {
    throw "JipperKeyViewer 正在运行，请先关闭再测试。"
}

Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public static class JkvBenchUser32 {
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern IntPtr FindWindow(string cls, string title);
    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")]
    public static extern void keybd_event(byte vk, byte scan, uint flags, UIntPtr extra);
}
'@

$exePath = (Resolve-Path $Exe).Path
$tmp = Join-Path $env:TEMP ("jkv-bench-" + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path "$tmp\JipperKeyViewer\config" -Force | Out-Null
Copy-Item $exePath $tmp
Copy-Item $ConfigPath "$tmp\JipperKeyViewer\config\config.json"
$global = Get-Content $GlobalPath -Raw | ConvertFrom-Json
$global.OverlayMode = [bool]$Overlay
$global | ConvertTo-Json -Depth 100 | Set-Content "$tmp\JipperKeyViewer\config\global.json" -Encoding UTF8

$p = $null
try {
    $p = Start-Process "$tmp\JipperKeyViewer.exe" -WorkingDirectory $tmp -PassThru
    Start-Sleep -Seconds 4
    if (-not (Get-Process -Id $p.Id -ErrorAction SilentlyContinue)) { throw "进程提前退出。" }

    $hwnd = [JkvBenchUser32]::FindWindow("JipperKeyViewerOverlay", "Jipper Key Viewer Display")
    if ($hwnd -eq [IntPtr]::Zero) { throw "未找到显示窗口。" }
    if (-not [JkvBenchUser32]::SetForegroundWindow($hwnd)) {
        throw "无法聚焦显示窗口，已中止（避免按键发到其它程序）。"
    }
    Start-Sleep -Milliseconds 500

    $p.Refresh()
    $cpu0 = $p.TotalProcessorTime.TotalMilliseconds
    $keys = [byte[]](0x31, 0x32, 0x45, 0x50, 0xBB, 0x08, 0xDC, 0x20, 0x43, 0x4B, 0xBE, 0x0D)
    $delay = [Math]::Max(5, [int](1000 / [Math]::Max(1, $KeysPerSecond)))
    $deadline = [DateTime]::UtcNow.AddSeconds($Seconds)
    $i = 0
    while ([DateTime]::UtcNow -lt $deadline) {
        $vk = $keys[$i % $keys.Length]
        [JkvBenchUser32]::keybd_event($vk, 0, 0, [UIntPtr]::Zero)
        [JkvBenchUser32]::keybd_event($vk, 0, 2, [UIntPtr]::Zero)
        $i++
        Start-Sleep -Milliseconds $delay
    }
    $p.Refresh()
    $cpu1 = $p.TotalProcessorTime.TotalMilliseconds
    $cores = [Environment]::ProcessorCount
    [pscustomobject]@{
        Exe = [IO.Path]::GetFileName($exePath)
        Overlay = [bool]$Overlay
        Seconds = $Seconds
        Keys = $i
        CpuPct = [Math]::Round((($cpu1 - $cpu0) / ($Seconds * 1000.0 * $cores) * 100.0), 2)
        WorkingSetMB = [Math]::Round($p.WorkingSet64 / 1MB, 1)
        PrivateMB = [Math]::Round($p.PrivateMemorySize64 / 1MB, 1)
    } | Format-List
}
finally {
    if ($p -and (Get-Process -Id $p.Id -ErrorAction SilentlyContinue)) {
        Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue
    }
    Start-Sleep -Milliseconds 500
    Remove-Item $tmp -Recurse -Force -ErrorAction SilentlyContinue
}
