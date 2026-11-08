# 源码最终审计（P6.1）

日期：2026-11-08  
范围：`JipperKeyViewerApp-键雨前` 源码；不含许可/文档定稿。

## 1. 构建与交付

| 项 | 结果 |
|---|---|
| `dotnet build -c Release --no-incremental` | **0 警告 / 0 错误** |
| `dotnet publish -c Release -r win-x64` | 成功 |
| 桌面 exe | `C:\Users\XueBaoT\Desktop\JipperKeyViewer.exe` |
| 大小 | 79,154,667 B |
| 时间 | 2026-11-08 08:07:25 |
| SHA256 | `C82BA2C3B36D2BE6F045BE0B32069864D0D7713D44895BBABE2754624ED80782` |

## 2. 结构结果

| 文件 | 行数 | 说明 |
|---|---:|---|
| `Program.cs` | 438 | 入口/字段/Win32/剩余胶水 |
| `UI/SettingsWindowWpf.xaml.cs` | 255 | 设置窗核心（构造/主题/IME/Toast） |
| `Render/KeyRenderer.cs` | 1,223 | 渲染主体 + slot 构建 |
| `Graphics/TransparentRenderer.cs` | 1,453 | CPU 光栅 + HWND 交换链 + 文本精灵 |
| `Render/LayoutGeometry.cs` | 167 | 108K/75K/60K + 8–24K 几何 |
| `Config/AppConfig.cs` | 582 | 配置模型 |
| `Config/Import/*` | 2 文件 | 旧 Mod / `.jkv` 兼容适配 |

已拆分：

- App：`Bootstrap`、`WindowProc`、`Startup`、`RenderLoop`、`RendererFactory`、`WindowState`、`Overlay`、`Package`、`HotkeyService`、`TrayService`、`Settings`、`SettingsCallbacks`；
- 设置窗 14 个 partial；
- 渲染：`CountFormatter`、`TextLayout`、`IRenderBackend`、`LayoutGeometry`、`KeySlot`、`DrawKeys`、`DrawInfoBars`、`UpdateFrameState`；
- 配置：`ConfigMigration`、`AppConfig.PerKey`、`Config/Import`。

## 3. 边界与 workaround

应保留在独立位置的：

- OBS WGC 交换链契约：`Graphics/TransparentRenderer.cs`；
- DComp 覆盖层 + 离屏 copy：`Graphics/GpuDCompOverlayRenderer.cs`；
- 旧 Mod / `.jkv` 适配：`Config/Import/`；
- Win32 P/Invoke：`Program.cs`、`App/`、`Graphics/`；
- GDI+ 文本精灵/LRU：`Graphics/TransparentRenderer.cs`。

未强行合并：OBS/DComp/Win32/GDI+/旧格式/视觉常量。

## 4. 本次清理

- 删除未使用的 `WS_EX_NOREDIRECTIONBITMAP`；
- 删除根目录 stale `*_wpftmp.csproj`；
- 删除已完成阶段的 `PATCH-MERGE` 注释；
- 保留 `PATCH-KEEP` 平台标记。

## 5. 冒烟结果

| 模式 | 结果 |
|---|---|
| 普通 GPU 窗口 | 启动正常 |
| 覆盖层 DComp | 启动正常 |
| `JKV_GPU=0` CPU | 启动正常 |
| 旧配置（无布局数组） | 启动正常并补默认 |
| 设置热键 | 此前验证可打开设置 |
| 右键菜单 | `ContextMenuStrip` 回归已修复 |

未覆盖：

- 真实 OBS 采集（需人工复测）；
- 144/240Hz 高刷（无硬件）；
- 长时间 10 分钟内存曲线（此前基线，未在本次重跑）；
- 多显示器/DPI 缩放/游戏独占全屏组合。

## 6. 剩余风险

1. `TransparentRenderer`（1,453）和 `KeyRenderer`（1,223）仍偏大；继续拆收益递减，建议以性能/回归数据驱动。
2. Win32 P/Invoke 未集中到 `Interop/Platform`；当前分散但可编译、可运行。
3. 根目录仍保留阶段说明/覆盖层方案/字体清理脚本等旧文档；按用户要求文档暂缓。
4. 字体许可（MapleStory 等）未定稿，公开发布前必须处理。
5. 无本地 Git，仓库部署由用户手动完成。

## 7. 结论

P0–P5 的结构目标已完成；P6 审计未发现构建/启动阻断问题。  
建议冻结结构性重构，进入真实场景回归（OBS、覆盖层穿透、输入延迟、高刷、长时内存）后再决定是否继续拆 `TransparentRenderer`/`KeyRenderer`。
