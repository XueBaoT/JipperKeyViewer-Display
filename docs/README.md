# Documentation Index

Project documentation entry point. Start here before reading individual files.

## Status

- Source optimization: **P0–P6 complete**
- Current phase: maintenance / real-device regression
- Build: **0 warnings / 0 errors**
- Latest desktop build: `JipperKeyViewer.exe` (2026-11-08 10:10:41)
- Known follow-ups: OBS/overlay/high-refresh regression; license and font audit.

## Start Here

1. [README.md](../README.md) — English project overview and build commands.
2. [README.zh-CN.md](../README.zh-CN.md) — Chinese overview.
3. [source-optimization-summary.md](source-optimization-summary.md) — what changed in P0–P6 and maintenance cost.
4. [final-audit.md](final-audit.md) — final structural/build audit.

## Architecture and Maintenance

| Document | Purpose |
|---|---|
| [源码优化规划.md](源码优化规划.md) | Original P0–P6 optimization plan and module boundaries |
| [source-optimization-summary.md](source-optimization-summary.md) | Results, metrics, and maintainability assessment |
| [final-audit.md](final-audit.md) | Final audit: build, structure, workaround boundaries, residual risks |
| [patch-debt.md](patch-debt.md) | Merge / Keep / Delete patch inventory and P1.3–P6 schedule |
| [regression-checklist.md](regression-checklist.md) | Manual regression checklist for OBS, overlay, input, settings, performance |

## Performance and CPU Investigation

| Document | Purpose |
|---|---|
| [cpu-issue-report.md](cpu-issue-report.md) | Self-contained CPU/input-latency problem report for external analysis |
| [cpu-high-input-investigation.md](cpu-high-input-investigation.md) | High-input CPU measurement method and A/B results |

## Build, Release, and Assets

| Document | Purpose |
|---|---|
| [../CHANGELOG.md](../CHANGELOG.md) | Change history |
| [../CONTRIBUTING.md](../CONTRIBUTING.md) | Contribution rules |
| [../THIRD_PARTY_NOTICES.md](../THIRD_PARTY_NOTICES.md) | Third-party attributions |
| [assets-license-audit.md](assets-license-audit.md) | Font/image license gate (do not publish unverified assets) |
| [../LICENSE](../LICENSE) | Project license |

## Historical / Legacy Notes

These are kept for context but are not the primary entry points:

- [../README-阶段0-OBS透明验证.md](../README-阶段0-OBS透明验证.md)
- [../README-阶段1-测试版.md](../README-阶段1-测试版.md)
- [../覆盖层方案-透明窗口.md](../覆盖层方案-透明窗口.md)
- [../版权与许可信息.md](../版权与许可信息.md)

## Build Commands

Self-contained (includes .NET runtime, larger):

```powershell
dotnet publish .\JipperKeyViewerApp.csproj -c Release -r win-x64 -o .\dist
```

Framework-dependent (does not include .NET runtime, requires .NET 8 Desktop Runtime):

```powershell
dotnet publish .\JipperKeyViewerApp.csproj -c Release -r win-x64 --self-contained false `
  -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=false -o .\dist-fx
```

## Source Layout

```text
App/        bootstrap, window proc, render loop, tray, hotkey, settings host, overlay, package
Core/       keyboard hook, rain, easing, Unity key mapping
Config/     config model, migration, profiles, layouts
Config/Import/  legacy Mod and .jkv adapters
Graphics/   CPU renderer, GPU renderer, DComp overlay, DirectComposition interop
Render/     key rendering, layout geometry, text layout, count formatting, key slot
UI/         WPF settings window (partials), theme, localization, dialogs
docs/       this documentation set
```

## Notes

- New comments should be written in English.
- Corrupted/mojibake comments were removed; source remains UTF-8.
- Workarounds for OBS, DComp, GDI+, and legacy formats should stay in their existing boundary modules.
