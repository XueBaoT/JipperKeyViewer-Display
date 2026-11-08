![图片描述](https://github.com/XueBaoT/JipperKeyViewer-Display/releases/download/Picture/background.png)

# JipperKeyViewer Display
这是一个基于 **冰与火之舞** (A Dance of Fire and Ice) 第三方 Mod 二次构建开发的按键映射显示程序。程序版本添加了更多功能，且更适用于多种用户群体。
如果你需要一个简单、好用，而且且多功能的按键映射程序用于**直播**、**录制**或是**游戏覆盖层**，你需要的一切，都在这里！

[TOC]

## 环境与运行
程序依赖的运行环境：

系统：`Windows 10/11`
GPU : 支持`Direct3D 11` /`Direct2D` 的显卡与驱动
框架：`.NET 8 Desktop Runtime（x64）`

程序运行若 GPU 初始化或 `Present` 失败时将自动回退 CPU 兼容渲染模式。

---

`Release`中提供了两个程序版本：

`JipperKeyViewer.exe`和`JipperKeyViewer(.NET).exe`

其中`JipperKeyViewer.exe`如果系统内没有安装对应.NET框架，程序会默认弹出所需库的下载请求。
如果在`Release`中下载了已封装.NET框架的`JipperKeyViewer(.NET).exe`，则可以直接使用程序自带的.NET库直接运行。
（这一般是为无法正常下载.NET库的用户提供的兼容版本。此版本会造成更大的性能占用开销！）
## 核心功能
### 1.按键显示与源捕获
按键显示是本程序最核心，也是最基础的功能。
除了全键盘配列外，其它的预设配列布局更多是针对音游来设计，包括下文将要介绍的键雨功能。

**源捕获：**
你可以搭配任何支持搭建场景的录制与直播程序（如 OBS Studio ）中轻松捕获按键显示窗口，并使其在场景中显示，且支持原生透明！

以 OBS Studio 捕获举例：
在场景中新建一个“游戏捕获”源，模式调整为“采集特定窗口”并在窗口中选择“Jipper Key Viewer Diaplsy”窗口进行捕获；然后在下方的勾选框选项中启用“允许窗口透明”。如图所示：
![图片描述](https://github.com/XueBaoT/JipperKeyViewer-Display/releases/download/Picture/OBS-1.png)
单击确定，此时在场景中就会透明状态显示 KeyViewer 窗口。如图所示：
![图片描述](https://github.com/XueBaoT/JipperKeyViewer-Display/releases/download/Picture/OBS-2.png)

---

### 2.设置页面
在KeyViewer显示窗口右键打开右键菜单，即可打开设置页面。
![图片描述](https://github.com/XueBaoT/JipperKeyViewer-Display/releases/download/Picture/setting-main.png)
同时，你也可以通过 `设置`>`按键类` 页面下配置全局快捷键来打开设置，便于快速调整功能选项。
（默认按键为F8）

---

### 3.键雨
一个可以实时查看并反映出当前按键触发手法与状态的机制。
![图片描述](https://github.com/XueBaoT/JipperKeyViewer-Display/releases/download/Picture/rainkeys.png)
除了全键盘配列 (60K/75K/108K) ，其它的所有配列全部支持键雨显示，并可自定义修改按键映射。
键雨功能为 ADOFAI 提供了 **补拍键雨**（GhostRain）选项，可在设置面板中启用。

---

### 4.个性化调整
设置中提供了很多自定义个性化调整选项，简单调整即可让你的按键显示与众不同！
从颜色修改到文本样式，具体的可配置项这里不再过多介绍。

---

### 5.导入/导出配置方案
你可以在 `设置`>`通用类` 页面中选择需要导出的自定义配置方案，然后导出为`.jkv`配置文件。
程序在初次启动后，重启系统会自动将`.jkv`后缀文件关联到程序并注册图标。
你可以双击配置文件快速应用配置方案，或将配置文件直接拖动到 KeyViewer 显示窗口快速应用当前配置文件。

---

### 6.全局覆盖层
如果你需要在屏幕上实时查看按键触发状态，此功能可以直接在显示器上映射一个透明的全局KeyViewer覆盖层，用于实时显示按键触发状态。
如图：
![图片描述](https://github.com/XueBaoT/JipperKeyViewer-Display/releases/download/Picture/global_layered.png)
**特别提醒：**
在开启全局覆盖层功能后，窗口会完全穿透鼠标任何交互操作。如果需要调整全局覆盖层在屏幕上的大小和位置，请在启用此功能之前就调整好窗口大小与显示位置，然后再启用功能。
如果需要关闭全局覆盖层功能，可以通过设置中已配置的全局快捷键快速打开设置页面，或使用任务栏的系统托盘右键快速关闭。
另外，启用此功能后，OBS Studio中的“游戏捕获”源会因为丢失窗口而立即失效！
（*特别感谢 @到处乱跑的隐藏者 提供的功能建议* ）

---

### 7.窗口位置与大小记忆
每一次拖动按键显示窗口改变位置，或拖动边缘改变窗口分辨率，在关闭程序前这些数据都会被记忆，并在下一次打开时尝试保持原样，避免经常性的修改。
~~（如果你没有尝试删除配置文件的话）~~

---

### 8.全键盘
当前版本支持60K、75K与108K的全键盘配列。
~~（由于108K配列与87K配列布局几乎相同，所以暂不考虑制作87K配列。如果需求较多的话会考虑将87K配列纳入开发计划）~~

---

### 9.特别支持的配列
根据下落式音游的特点与玩家数量的综合统计，我们制作了专属的 **osu**!**mania** 布局！

⌨支持配列：4K~9K

双人模式下的按键显示待程序的后续版本开放多进程后可实现！

---

## 性能与兼容性
**性能表现：**
程序在 Alpha 与 Beta 测试阶段没有经过广泛测试。大多数多数情况下，统计数据只根据开发设备的测试情况来判断程序的性能占用。
目前，`JipperKeyViewer.exe`程序的性能占用足以满足在绝大多数设备下十分流畅地运行。当然，如果您的设备配置过低，程序有可能会在与其它程序一同使用时出现：
帧率下降、CPU 占用过高、输入延迟显著等一系列问题。
这并非来自代码的编写缺陷，而是程序捕捉按键输入的低级钩子，在面对低性能 CPU 经常满占用的情况下难免会被占用原有的工作进程，导致输入延迟显著增加。这种现象在多按键的高速输入中尤为明显。

**兼容性：**
程序的兼容性有待考究。
程序主要针对 `Windows10 / 11 x64` 环境开发。
其它如`Windows7 /8`或`Arm64`版本的系统环境可能不受支持。如果您已实测这些环境下无法稳定或正常运行，且确实需要在不受支持的环境下使用此程序，**由此造成的一系列后果（包括但不限于系统崩溃，数据丢失），开发者不承担任何责任。**
（*真的需要用的话可以试试让 AI 克隆源码仓库修改并编译一个可用的版本* ）
## 特别说明
⚠此`README.md`教程文档一定要读完！
⚠此`README.md`教程文档一定要读完！
⚠此`README.md`教程文档一定要读完！

（程序中半数使用了 AI 进行辅助开发，而非完全依赖AI。）
## 开发人员
**程序开发组：**
XueBaoT

**辅助开发组：**
DeepSeek (Agent)

**美术与UI设计：**
Tsukyoze
nainai奈

**特别感谢：**
蘸料先生ZhanLiao
到处乱跑的隐藏者
JipperKeyViewer Mod开发组
Team.XueBaoT

**测试人员：**
渣渣
到处乱跑的隐藏者
## 版权声明与许可
JipperKeyViewer Display
Copyright © 2026 XueBaoT — MIT License
本程序使用 MIT License 许可协议发布。

本程序是基于发行游戏“**冰与火之舞** (A Dance of Fire and Ice)”的第三方 Mod "JipperKeyViewer" 制作的独立 Windows 桌面移植与重构版本：

第三方 Mod：JipperKeyViewer
Copyright © 2026 HitMargin — MIT License
[[仓库链接]](github.com/2228293026/JipperKeyViewer)

本程序与原 Mod 作者、7 Beat Games、Microsoft 无隶属、赞助或背书关系；
"JipperKeyViewer" 名称仅用于说明来源。
程序内置的 MapleStory OTF Bold 字体为专有字体，不适用 MIT。字体本身的版权与再分发条件归原始权利人（NEXON / FONTRIX）所有，商用分发前请自行核实授权或替换为可自由分发的字体。

免责声明：
本程序依据Mod"JipperKeyViewer"提供，不附带任何明示或暗示的担保。使用本程序造成的任何直接或间接损失（包括但不限于直播画面异常、掉帧、配置丢失），开发者不承担任何责任。使用前请自行备份配置。
## 第三方资源
**代码的`MIT License`许可不自动覆盖内置字体、图片、图标等资源。**