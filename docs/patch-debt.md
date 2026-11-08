# 补丁分级与合并计划 / Patch Debt Inventory

> 允许“主体 + 补丁”，但不是所有补丁都值得合并。
> 原则：**能变成模型/参数/服务的补丁才合并；平台怪癖、第三方格式兼容、经验性视觉常量保持独立适配层。**

## 1. 判定标准

### 合并到主体

- 是产品行为，不是某个设备/驱动的怪癖；
- 可以表达成状态、模型、参数或服务；
- 合并后能减少条件分支，而不是把补丁搬到另一个文件；
- 有手动回归或自动测试覆盖；
- 不影响 OBS、覆盖层穿透、输入延迟、热键。

### 保持独立

- 依赖 Win32 / DWM / D3D / DComp / 显卡驱动 / OS 版本；
- 是对第三方 bug、旧格式、旧配置的兼容；
- 是经过实测的视觉/几何常量，合并会污染主逻辑；
- 合并后会把平台条件散落到业务代码。

### 直接删除

- 没有调用方的旧实现；
- 已被新路径替代的死分支；
- 重复词条、重复分支、不可达代码。

## 2. 建议合并到主体

| # | 补丁点 | 位置 | 合并目标 | 阶段 |
|---|---|---|---|---|
| 1 | KPS/Total 字体 baseline 对齐 | `Render/KeyRenderer.cs`、`Graphics/TransparentRenderer.cs` | `Rendering/Text/TextLayout`（字体 metrics + 共用 baseline） | P4 |
| 2 | 计数格式化/千分位/区域格式 | `KeyRenderer.FormatCount` | `Rendering/Text/CountFormatter` | P4 |
| 3 | 配置缺失数组/颜色数组修复 | `Config/AppConfig.cs` 的 `Ensure*`/`FillDefaultKeys` | `Config/Normalization/ConfigNormalizer` | P5 |
| 4 | 旧配置键迁移（Language / SettingsHotkey / OverlayMode） | `Config/GlobalSettings.cs`、`AppConfig.Load` | `Config/Migration/ConfigMigration`（显式版本迁移） | P5 |
| 5 | 窗口尺寸兜底/位置记忆/安全边距 | `Program.cs` | `App/WindowStateService` | P2 |
| 6 | 渲染后端选择与失败回退 | `Program.CreateRenderer` | `Rendering/RendererFactory` | P2/P4 |
| 7 | 覆盖层切换、窗口重建、置顶/位置锁定 | `Program.RebuildWindowForOverlay` 等 | `App/OverlayController` | P2 |
| 8 | .jkv 关联、拖放、导入胶水 | `Program.cs` | `App/PackageService` | P2 |
| 9 | 本地化前缀兜底 | `UI/UiLanguage.cs` | `UI/Localization/LocalizationService` | P3 |
| 10 | VK ↔ Unity KeyCode 转换 | `Program.ApplyCapture` / `Core/UnityKeyCode.cs` | `Core/Input/KeyCodeMapper` | P2/P5 |
| 11 | 每键颜色回退/数组越界保护 | `KeyRenderer.PerKeyColorAt`、`AppConfig` | `Config/Models` 的规范化或 `KvPalette` | P5 |

合并要求：先补测试/回归项，再删除原补丁分支；不要保留两份行为。

## 3. 建议保持独立

| # | 补丁点 | 位置 | 保留方式 |
|---|---|---|---|
| 1 | OBS WGC 的普通 HWND 交换链契约（`Discard` + `Unspecified` + `Present(1)`） | `Graphics/TransparentRenderer.cs` | `Rendering/Backends/D3DHwndBackend`，注释说明 OBS 约束 |
| 2 | 覆盖层 layered + DComp + composition swapchain | `Graphics/GpuDCompOverlayRenderer.cs` | `Rendering/Backends/DCompOverlayBackend`，独立文件 |
| 3 | composition buffer 不能直接作 D2D target，需离屏 + GPU copy | `GpuDCompOverlayRenderer` | 同上，保留为平台 workaround |
| 4 | Win32 窗口样式、单实例、热键、IME、置顶、TOOLWINDOW | `Program.cs`、`Interop` | `Interop/Platform` / `App` 平台服务 |
| 5 | GDI+ 文字 oversampling / 描边 / 阴影 / 字体缓存 | `TransparentRenderer` 文本部分 | `Rendering/Text/GdiTextRasterizer`，保留视觉实现 |
| 6 | 内嵌字体回退/字体集合重建 | `FontManager`、`TransparentRenderer.TextFontFamily` | `Rendering/Text/FontService` |
| 7 | 原 Mod ProfileData / 旧版 `.jkv` 兼容导入 | `ProfileImport`、`PackageImporter` | `Config/Import` 适配器，只做输入转换 |
| 8 | 键雨运动/淡出/溢出几何严格对齐原 Mod | `Core/Rain.cs`、`KeyRenderer.DrawRainLayer` | `Core/Rain` + `Rendering/Painting/RainPainter` |
| 9 | 设置 UI 的像素级微调（7.5/10px 等） | `UI/SettingsWindowWpf` | UI 样式常量/布局资源，不进入业务逻辑 |
| 10 | 旧配置/旧分享包的失败回滚 | 导入器 | 适配器内部保留，不扩散到主模型 |

## 4. 已删除/待删除

- 旧 `UI/SettingsWindow.cs`：已删除；
- `TransparentRenderer` 旧 Composition 分支：已删除；
- `Diagnostics/MemoryStats.cs` 与 `[MEM]` 日志：已删除；
- `UiTextTable` 重复键：已删除；
- 后续 P3 继续清理设置窗中一次性事件补丁/重复逻辑。

## 5. 阶段安排

### P1.3 补丁清单与标记（P2 前，低风险）

- 用本文件作为基线，逐项标注 `Merge / Keep / Delete`；
- 在每个补丁点加简短引用（如 `// PATCH-KEEP: OBS WGC`），不改变行为；
- 为合并项准备回归清单。

### P2 应用外壳合并

重点合并：窗口状态、RendererFactory、OverlayController、PackageService、KeyCodeMapper。
平台补丁移到 `Interop/Platform`，不进入业务服务。

### P3 设置 UI 合并

重点合并：本地化前缀兜底、颜色/方案/导入事件中的重复补丁。
UI 像素常量保留在样式层。

### P4 渲染合并

重点合并：字体 baseline、计数格式、布局/绘制分层。
**不合并**：DComp 覆盖层、OBS 交换链契约、GDI+ 文字栅格化、离屏 copy workaround。

### P5 配置/兼容合并

重点合并：配置规范化、旧键迁移、每键颜色回退。
**保持独立**：原 Mod ProfileData、旧 `.jkv` 导入适配器。

### P6 最终审计

- 业务代码中不再出现平台特判；
- `Rendering/Backends`、`Config/Import`、`Interop/Platform` 是唯一允许保留 workaround 的位置；
- 每个保留项有注释、回归项和删除条件。

## 6. 结论

不是“全部融入主体”，而是：

- **主体**：产品行为、状态、布局、配置规范化、服务编排；
- **独立适配层**：OBS/DComp/Win32/GDI+/旧格式兼容；
- **删除**：死代码和重复补丁。

这样既减少补丁堆积，又不会为了“纯净”牺牲已经验证过的平台兼容性。


## 7. P1.3 执行状态（2026-11-08）

- 已建立本清单与 `docs/regression-checklist.md`；
- 已在关键点加代码标记（不改行为）：
  - `TransparentRenderer`：`PATCH-KEEP(P4): OBS WGC`（Discard/Unspecified/Present(1)）；
  - `GpuDCompOverlayRenderer`：`PATCH-KEEP(P4)`（layered+DComp、离屏 copy workaround）；
  - `KeyRenderer.DrawInfoText`：`PATCH-MERGE(P4): TextLayout`；
  - `AppConfig.Load`：`PATCH-MERGE(P5): ConfigMigration`；
  - `UiLanguage`：`PATCH-MERGE(P3): LocalizationService`；
  - `ProfileImport`：`PATCH-KEEP(P5): 第三方格式适配器`。
- 构建：0 警告 / 0 错误。
- 下一步：P2 拆 `Program.cs`，按本清单合并 shell 级补丁；平台补丁移入 `Interop/Platform`。
