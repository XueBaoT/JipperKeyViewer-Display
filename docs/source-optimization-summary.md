# 源码优化总结（P0–P6）

日期：2026-11-08  
版本：源码优化后最终快照 `checkpoints/2026-11-08_0946-Post-Optimization/`

## 1. 结论

本次工作完成了：

- 语法/分析器基线：188 条唯一警告 → **0 警告 / 0 错误**；
- `Program.cs`：**1,339 → 438 行**；
- `SettingsWindowWpf.xaml.cs`：**2,167 → 255 行**；
- 渲染：加入 `IRenderBackend`、`TextLayout`、`CountFormatter`、`LayoutGeometry`、`KeySlot`，并把状态更新/逐键绘制/统计绘制分段；
- 配置：`ConfigMigration`、`AppConfig.PerKey`，兼容层集中到 `Config/Import`；
- 最终构建：0 warning / 0 error，普通/覆盖层/CPU/旧配置冒烟正常。

但本次是**结构整理**，不是性能优化；CPU 异常问题（游戏 + 100KPS 接近 100%）仍未解决，只增加了每 5 秒 `[PERF]` 日志用于排查。

## 2. 结构变化

### App

`Program` 拆为：

- `Bootstrap`、`WindowProc`、`Startup`、`RenderLoop`
- `RendererFactory`、`WindowState`
- `Overlay`、`Package`
- `HotkeyService`、`TrayService`
- `Settings`、`SettingsCallbacks`
- `PerformanceMonitor`

### 设置窗

`SettingsWindowWpf` 拆为 15 个 partial：

- Profiles、Keys、PerKeyColors、Localization、Sliders、Rain、Colors、Save、Events、Layouts、Package、Fonts、Dialogs、Load 等。

### 渲染

- `IRenderBackend`
- `TextLayout`
- `CountFormatter`
- `LayoutGeometry`（108K/75K/60K 统一）
- `KeySlot`
- `UpdateFrameState` / `DrawKeys` / `DrawInfoBars`

### 配置

- `ConfigMigration`
- `AppConfig.PerKey`
- `Config/Import/ProfileImport`、`PackageImporter`

## 3. 人工维护难度：改善与代价

### 改善

1. 单文件体积显著下降，定位问题更快；
2. 职责边界更清楚：启动、窗口、托盘、热键、设置、覆盖层、导入、渲染分离；
3. 平台/格式 workaround 有明确归属：
   - OBS 交换链：`TransparentRenderer`
   - DComp/离屏 copy：`GpuDCompOverlayRenderer`
   - 旧 Mod / `.jkv`：`Config/Import`
4. 分析器基线干净，新增代码更容易保持 0 警告；
5. 渲染布局/文本/计数格式集中，减少重复逻辑。

### 代价与难度

1. **仍是静态 partial + 共享状态**
   - `Program` 的拆分主要是物理分片，不是实例化服务/DI；
   - 各 partial 仍共享 `_cfg/_hwnd/_renderer/_hook` 等静态字段；
   - 文件更多，但隐式依赖仍在，新人需要跨文件追踪。

2. **渲染热路径仍然集中**
   - `KeyRenderer` 1,223 行；
   - `TransparentRenderer` 1,453 行；
   - 几何、状态、绘制、文本/精灵仍高度耦合，继续拆收益递减且视觉回归风险高。

3. **设置窗仍是事件驱动 code-behind**
   - 15 个 partial 降低单文件复杂度，但没有 ViewModel/数据绑定；
   - 控件名、事件、字段仍强耦合；
   - 真正的 MVVM 化会是一次大改，不适合在当前阶段做。

4. **行为敏感、缺少自动化视觉回归**
   - DComp 覆盖层、OBS、GDI+ 文本、像素级圆角/光晕/基线；
   - 只能人工看画面/穿透/采集；
   - 结构改动必须逐项回归，维护成本高。

5. **性能问题与结构问题被混在一起**
   - 本次拆分没有降低 CPU/内存，也没有解决游戏内 CPU 100%；
   - 继续拆文件不会自动提升性能，必须靠 profiling/测量。

6. **兼容层需要长期保留**
   - 旧 Mod 配置、`.jkv`、旧字段迁移、全键盘布局；
   - 这些 workaround 不能为了“结构干净”而删除；
   - `Config/Import` 与 `ConfigMigration` 需要长期维护。

7. **工程流程仍是手工**
   - 无本地 Git；部署靠人工复制；
   - 没有自动 UI/性能回归；
   - 许可/文档定稿未完成。

## 4. 建议

1. **暂停继续拆文件**，除非有明确维护痛点或测量数据支持；
2. 优先解决 CPU 100%：先确认 `[PERF]` 的 `fps` 是否远高于刷新率，区分 Present 空转与 CPU 叠加饱和；
3. 若继续结构工作，优先做“有边界收益”的两件事：
   - 把 `TransparentRenderer` 的文本/精灵缓存独立；
   - 把 `KeyRenderer` 的布局/绘制进一步分层；
4. 不建议现在引入完整 DI/MVVM；收益不确定，回归面太大；
5. 每次结构改动仍保持：构建 0 警告、普通/覆盖层/设置/导入冒烟。

## 5. 当前交付

- 桌面：`C:\Users\XueBaoT\Desktop\JipperKeyViewer.exe`
- 大小：79,155,552 B
- 时间：2026-11-08 09:46:20
- SHA256：`F443E9D19C288E65EFAB605D700F6AC3E24CB72111922A451C2C792B02D134C4`
- 结构优化前备份：`JipperKeyViewer-结构优化前-P1.2.exe`（保留用于 CPU 对照）
- 快照：`checkpoints/2026-11-08_0946-Post-Optimization/`
