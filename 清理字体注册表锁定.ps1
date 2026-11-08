# ============================================================
# JipperKeyViewer 字体注册表清理脚本（PowerShell）
# 用途：清除注册表中"路径指向 JipperKeyViewer/fonts 文件夹"的字体条目，
#       解除 Windows 字体缓存服务（System 进程）对该文件夹文件的锁定。
# 影响范围：只删除"值数据指向 ...\JipperKeyViewer\fonts\..." 的注册表项；
#           不删除任何字体文件；系统字体（C:\Windows\Fonts）条目不受影响。
# 用法：右键"使用 PowerShell 运行"，或管理员 PowerShell 执行本脚本。
# ============================================================

$targets = @(
    "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts",
    "HKCU:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts"
)

# ---- 第一步：只读扫描，列出指向 JipperKeyViewer/fonts 的字体条目 ----
$found = @()
foreach ($root in $targets) {
    if (Test-Path $root) {
        $props = Get-ItemProperty $root
        foreach ($name in $props.PSObject.Properties.Name) {
            $val = $props.$name
            if ($val -is [string] -and $val -match "JipperKeyViewer\\fonts") {
                $found += [PSCustomObject]@{ Root = $root; Name = $name; Path = $val }
            }
        }
    }
}

if ($found.Count -eq 0) {
    Write-Host "未发现指向 JipperKeyViewer/fonts 的注册表字体条目，无需清理。"
    return
}

Write-Host "发现以下字体条目指向 JipperKeyViewer/fonts 文件夹："
$found | Format-Table -AutoSize

# ---- 第二步：用户确认后删除 ----
$ans = Read-Host "将从注册表删除上述条目（不删除任何字体文件）。继续？(y/n)"
if ($ans -ne "y") {
    Write-Host "已取消，未做任何修改。"
    return
}

foreach ($f in $found) {
    Remove-ItemProperty -Path $f.Root -Name $f.Name -ErrorAction SilentlyContinue
    Write-Host "已删除: $($f.Root) \ $($f.Name)"
}

Write-Host ""
Write-Host "完成。下一步（二选一）："
Write-Host "  1) 重启电脑；或"
Write-Host "  2) 管理员命令行执行:  net stop FontCache && net start FontCache"
Write-Host "之后即可正常删除 fonts 文件夹内的字体文件。"
