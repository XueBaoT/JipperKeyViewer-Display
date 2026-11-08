# 资源许可证审计 / Asset License Audit

> 发布门槛：本文件中的每一项必须在公开 Release 前有明确结论。
> 未确认的资源默认按“不可再分发”处理。

## 当前状态

| 资源 | 用途 | 当前结论 | 发布前动作 |
|---|---|---|---|
| `Assets/MAPLESTORY_OTF_BOLD.OTF` | 默认按键字体 | **不可确认可再分发**；版权归 NEXON/原字体权利人 | 从公开仓库移除，改为读取用户自备字体；或替换为 OFL 字体 |
| `Assets/fonts/hurme-geometric-bold.ttf` | 关于页/自定义字体 | 未确认 | 找到许可证或替换 |
| `Assets/fonts/shiliuzi-laihu2.ttf` | 关于页/自定义字体 | 未确认 | 找到许可证或替换 |
| `Assets/fonts/noto-sans-sc.ttf` | 关于页 CJK | 通常为 OFL-1.1 | 附上游 OFL 文本与版权行 |
| `Assets/app.ico` | 程序/托盘图标 | 项目自绘或需确认 | 确认作者/来源 |
| `jkv.ico` | `.jkv` 文件关联图标 | 项目自绘或需确认 | 确认作者/来源 |
| `Assets/about-banner.png` | 关于页横幅 | 项目自绘或需确认 | 确认作者/来源 |
| `Assets/info.png` / `warning.png` / `error.png` | 弹窗图标 | 项目自绘或需确认 | 确认作者/来源 |

## 代码许可

- 本项目新增/改造代码：MIT，见 `LICENSE`。
- 上游 Jipper Key Viewer：MIT，Copyright (c) 2026 HitMargin。
- 更上游 KeyViewer：BSD 3-Clause，Copyright (c) 2024 Jongyeol。
- 完整文本见 `THIRD_PARTY_NOTICES.md`。

## 建议方案

1. **首选**：公开仓库不包含 `MAPLESTORY_OTF_BOLD.OTF`；程序启动时从
   `JipperKeyViewer/fonts/` 读取用户自备字体，缺失时回退系统字体。
2. 替换为 OFL 字体（如 Noto Sans SC）作为默认字体，并保留 OFL 声明。
3. 所有图片/图标补充来源与作者；无法确认的从公开仓库移除。
