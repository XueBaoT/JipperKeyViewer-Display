# CPU 异常占用问题完整说明（自包含版）

> 用途：提供给不读取本地文件、只能依据本文推断的新模型。  
> 本文已包含全部关键背景、测量数据、代码行为和待确认项。

## 1. 背景

- 项目：JipperKeyViewer 独立版，Windows 桌面键位显示工具。
- 技术栈：C# 12 / .NET 8（`net8.0-windows`），WPF 设置窗 + WinForms 托盘，自建 Win32 消息循环。
- 渲染：
  - 普通模式：D3D11 + DXGI HWND 交换链 + D2D1 绘制；
  - 覆盖层：分层窗口（`WS_EX_LAYERED | TRANSPARENT | TOPMOST | TOOLWINDOW | NOACTIVATE`）+ DComp composition swapchain；
  - 文本：GDI+ 光栅化文字 sprite → D3D 纹理；
  - 默认 GPU；`JKV_GPU=0` 强制 CPU；初始化失败自动回退 CPU。
- 输入：`WH_KEYBOARD_LL` 全局低级键盘钩子，安装在独立线程，配专用 `GetMessage` 循环。
- 目标：60fps、CPU/GPU 低占用、高速输入无输入延迟；普通模式必须兼容 OBS 窗口/游戏捕获。

历史测试机（如无特别说明）：

- CPU：i5-6200U，4 线程；
- GPU：HD 520；
- OS：Windows 10 19045。

如果新环境不同，CPU 百分比含义会变化，需要重新确认。

## 2. 问题现象

### 2.1 有游戏时

游戏运行，同时运行本程序，高速输入（KPS > 100）时：

- CPU 接近 **100%**；
- GPU 稳定在 **50–60%**；
- 出现明显输入延迟，影响游戏操作。

### 2.2 没有游戏时

只运行本程序，进行高速输入：

- CPU 随 KPS 上升；
- KPS 0 时空载约 **4.1–4.4%**；
- KPS 40 时约 **8.4%**；
- KPS 73–139 时约 **11.6–14.6%**；
- GPU 正常；
- 没有明显输入延迟。

### 2.3 不运行本程序时

游戏 + 高速输入：

- 没有明显 CPU 升高；
- 说明游戏本身可以承受该输入强度。

### 2.4 已排除的方向

- 关闭键雨后，CPU **不回落**；
- 隐藏 KPS/Total 统计文本后，CPU **不回落**；
- 因此“雨滴绘制”和“KPS/Total 文本 sprite 重建”不是本次 100% 的主因。

### 2.5 内存

高速输入时：

- 工作集约 **250–258MB**；
- 私有约 **127–135MB**；
- 无明显持续增长；
- 设置窗未打开。

内存正常，问题集中在 CPU。

## 3. 测量数据

### 3.1 程序内 `[PERF]` 日志

当前版本每 5 秒写一条：

```text
[PERF] up=30.0s cpu=14.6% cpu_ms=2922 ws=252.2MB private=129.3MB managed=10.16MB handles=419 threads=16 fps=59.8 kps=139 total=50134
```

字段：

- `cpu`：按逻辑核心归一化的进程 CPU，接近任务管理器口径；
- `cpu_ms`：该 5 秒区间消耗的 CPU 时间；
- `ws` / `private`：工作集 / 私有提交；
- `managed`：托管堆；
- `handles` / `threads`：句柄/线程；
- `fps`：该区间帧率；
- `kps` / `total`：输入 KPS 与总计数。

注意：第一条样本包含启动/JIT，通常偏高，至少看连续 3 条。

### 3.2 无游戏实测序列

| 场景 | KPS | CPU | 工作集 | 私有 |
|---|---:|---:|---:|---:|
| 空载 | 0 | 4.1–4.4% | 241–242MB | 119–120MB |
| 输入上升 | 40 | 8.4% | 255MB | 133MB |
| 高频 | 73–139 | 11.6–14.6% | 250–258MB | 127–135MB |

### 3.3 合成输入 A/B（无游戏，普通模式）

| 版本 | 30 次/秒 CPU | 120 次/秒 CPU（单次） |
|---|---:|---:|
| P1.2（结构优化前） | 3.12% | 7.68% |
| P4（含渲染接口/KeySlot） | 3.12% | 7.45% |
| P6（当前结构） | 3.59% | 8.98% |

判断：

- P4 没有比 P1.2 高；
- P6 的 +1.3% 可能是一次测量噪声；
- 暂无证据表明 P2–P6 的结构拆分是主要原因；
- 但尚未做 3 次中位数，不能完全排除。

## 4. 当前版本与备份

### 当前版本

- 桌面程序：`JipperKeyViewer.exe`
- 大小：79,155,552 B
- 时间：2026-11-08 08:38:07
- SHA256：`F443E9D19C288E65EFAB605D700F6AC3E24CB72111922A451C2C792B02D134C4`
- 含 `[PERF]` 日志。

### 结构优化前备份

- 桌面程序：`JipperKeyViewer-结构优化前-P1.2.exe`
- 大小：79,153,243 B
- 时间：2026-11-08 05:32:34
- SHA256：`88BD7F845EB1FBBED95068EEBABE6F53995A245AA31CBD46CC35228FBD676961`
- 不含 `[PERF]` 日志，只能用任务管理器观察。

说明：P1.2 是 P2 结构拆分前的版本，已包含 P1.1 死代码清理和 P1.2 警告清理；渲染路径同为 GPU/DComp，适合做 CPU 对照。

## 5. 关键代码行为

### 5.1 主循环

主循环逻辑：

```csharp
while (true)
{
    while (PeekMessageW(...)) { ... }

    RenderFrame();       // KeyRenderer.Render + renderer.Present
    frameCount++;

    Perf.Frame(_hook.Kps, _hook.TotalCount);
}
```

关键点：

- **没有软件帧率限制**；
- 完全依赖 `Present(1)` 阻塞来跟随屏幕刷新率；
- 如果 `Present` 提前返回/不阻塞，循环会以极高帧率空转。

### 5.2 呈现

普通模式：

```csharp
_swapChain.Present(1, PresentFlags.None);
```

覆盖层：

```csharp
Context.EndDraw();
ctx.CopyResource(buffer, _offscreen);
SwapChain.Present(1, PresentFlags.None);
```

关键点：

- 两者都没有检查 `Present` 的 occluded/提前返回状态；
- 也没有“Present 没有阻塞时补帧”的兜底；
- 覆盖层使用 layered + DComp，游戏全屏/独占全屏时可能被遮挡。

### 5.3 输入

- 使用 `WH_KEYBOARD_LL` 全局低级键盘钩子；
- 钩子独立线程 + `GetMessage` 循环；
- 回调内做：键码映射、字典查找、按下状态、KPS 队列、计数、事件分发；
- **Windows 会等钩子回调返回后，才把按键交给游戏**；
- 100+ KPS 时，每个按键都经过这条同步链。

### 5.4 渲染内容

- 键帽、渐变、描边、光效；
- KPS/Total 统计条；
- 键雨：每键最多 64 滴，按下触发，松手淡出；
- 文本使用 GDI+ sprite + D3D 纹理。

雨和统计文本已通过关闭测试排除为主因。

### 5.5 结构优化内容

P2–P6 主要做的是：

- 把 `Program.cs` 拆成 `App/Bootstrap`、`WindowProc`、`RenderLoop`、`Overlay` 等；
- 设置窗按页拆 partial；
- 抽 `IRenderBackend`、`TextLayout`、`CountFormatter`、`LayoutGeometry`、`KeySlot`；
- 配置迁移/兼容层收尾。

这些改动没有在每次按键路径上增加明显工作；接口/KeySlot 的 A/B 也没有显示 CPU 升高。

## 6. 主要假设

### A. `Present(1)` 失去 vsync，主循环空转（最高嫌疑）

如果游戏全屏/独占全屏，或 DComp 覆盖层被遮挡：

- `Present(1)` 可能立即返回，不阻塞；
- 主循环无限空转；
- CPU 接近 100%；
- GPU 只有 50–60%（每帧内容轻）；
- 键盘钩子线程被 CPU 抢占，产生输入延迟；
- 关闭雨/文本不会改善，因为空转与绘制内容无关。

**判别方法：游戏内看 `[PERF]` 的 `fps`。**

- `fps` 远高于显示器刷新率（例如 150–1000）→ 基本确认空转；
- `fps` 约等于 60 → 不是空转。

### B. 游戏 + 程序 CPU 叠加饱和

游戏本身占 80–90% CPU，程序再占 12–17%，系统总 CPU 到 100%，输入线程被挤压。

**判别方法：**

- 游戏运行，程序挂着但完全不输入：CPU 是否仍接近 100；
- 不启动游戏，只跑程序高速输入：CPU 是否只有 12–14%；
- 如果“游戏 + 程序 idle”也 100%，更偏 A；
- 如果只有高速输入才 100%，更偏 B/输入路径。

### C. 低级键盘钩子延迟

`WH_KEYBOARD_LL` 会给所有键盘输入增加一次同步回调。高 KPS + CPU 饱和时，钩子线程调度延迟会放大为输入延迟。

它可能解释“输入延迟”，但单独解释不了 CPU 100%；需要结合 A/B 看。

### D. 文本 sprite / 键雨（已基本排除）

关闭雨、隐藏 KPS/Total 后 CPU 不回落，因此不是本次 100% 的主因。

### E. 结构优化 / 接口 dispatch / KeySlot（低）

P4 已经包含这些改动，但 P4 没有比 P1.2 高；暂不作为主因。

### F. GC / 托管分配

`managed` 在 10–25MB 波动；没有 GC 次数/暂停时间日志，不能完全排除短时 GC 卡顿，但不太像持续 100%。

### G. 游戏/驱动/反作弊/覆盖层兼容性

游戏全屏模式、GPU 驱动、反作弊对全局钩子/覆盖层的处理都可能影响输入延迟；目前没有排除。

## 7. 最重要的待确认项

1. 用户说的“CPU 接近 100”是**系统总 CPU**，还是**任务管理器详细信息里当前进程的 CPU**。
   - 之前提到“详细页面 CPU 17 左右”；
   - 如果是进程 17%，则游戏 + 程序叠加饱和更合理；
   - 如果是进程自身 100%，则程序热路径/空转更合理。
2. 游戏内 `[PERF]` 的 `fps`。
3. 游戏 + 程序 **不输入** 时 CPU。
4. 游戏 + 高速输入、**覆盖层关闭**时 CPU/延迟。
5. 同场景运行 P1.2 备份的 CPU。
6. 显示器刷新率与实际 fps。

## 8. 建议的诊断顺序

1. 确认 CPU 口径（系统 vs 进程）。
2. 游戏 + 程序挂着，不输入，观察 CPU。
3. 游戏 + 高速输入，抓 `[PERF]` 的 `fps`。
4. 如果 `fps` 远高于刷新率：查 Present/occluded/空转。
5. 如果 `fps` 约 60：查游戏+程序叠加、钩子线程、CPU 饱和。
6. 覆盖层关闭/全屏改窗口化，重复测试。
7. 用 P1.2 备份做同场景对照。
8. 如需更细：记录 Present 前后耗时、钩子回调耗时、渲染线程/钩子线程 CPU 时间、GC 次数。

## 9. 修复约束

不能破坏：

- 普通模式 OBS WGC 捕获（`Present(1)`、Discard、Unspecified）；
- 覆盖层逐像素透明、置顶、不抢焦点、整窗穿透；
- 无 CPU 回读/ULW；
- 60fps 与输入延迟目标。

## 10. 结论

目前必须先区分：

```text
Present 空转（fps 远高于刷新率）
   vs
游戏 + 程序 CPU 叠加饱和（fps 约 60，但系统 CPU 100）
```

在拿到游戏内 `[PERF]` 的 `fps` 前，不应继续改渲染架构；两种情况的修复方向完全不同。


## 11. 2026-11-08 追加发现：累计输入后空闲 CPU 升高

用户进一步观察：刚启动空载 CPU 约 6%，经过一段时间输入后，空载也升到约 10%。  
这不是“输入瞬间的负载”，而是状态随时间累积。

### 已确认的一处无界增长

KeyboardHook 每次按下都会写每键 KPS 时间戳：

`csharp
_keyPressTimes.GetOrAdd(vk, _ => new ConcurrentQueue<long>())
    .Enqueue(Environment.TickCount64);
`

但只有 PerKeyKps == true 时，渲染端才会调用 GetPerKeyKps 清理。  
当前用户配置 PerKeyKps = false，因此这些队列只写不读、无上限增长。

影响：

- 每个按键新增一个 long；
- 每个 VK 一条 ConcurrentQueue；
- 长时间输入后队列持续变大；
- GC/托管堆压力上升，可能导致空载 CPU 升高。

同类的 _kpsTimes 只依赖 Kps getter 清理；当前 PERF 每帧调用，所以暂时没有泄漏，但不应该依赖调用方。

### 已做修复

- PerKeyKps=false 时不再写每键时间戳；
- 关闭时清空已有队列；
- KeyRenderer.RefreshLayout 同步该开关；
- [PERF] 增加 kps_q / perkey_q，可直接观察队列是否增长。

修复后交付：
- 79,155,974 B
- 2026-11-08 10:10:41
- SHA256 4F7B34F446B6731823878BFC2E5CDFCF881760DF9EC3E0663840D6E1E12C426E

待验证：长时间输入后空载 CPU 是否稳定；perkey_q 应恒为 0（PerKeyKps 关闭时）。
