![JipperKeyViewer Display](https://github.com/XueBaoT/JipperKeyViewer-Display/releases/download/Picture/background.png)

# JipperKeyViewer Display

JipperKeyViewer Display is a key-mapping display application based on a rebuilt and extended version of the third-party **A Dance of Fire and Ice** mod. This version adds more features and is designed to serve a wider range of users.

If you need a simple, easy-to-use, and versatile key display for **streaming**, **recording**, or an **in-game overlay**, everything you need is here!

[简体中文](README.md) | [English](README-English.md) | [한국어](README-Korean.md)

## Table of Contents

- [System Requirements and Running the Program](#system-requirements-and-running-the-program)
- [Main Features](#main-features)
  - [1. Key Display and Source Capture](#1-key-display-and-source-capture)
  - [2. Settings](#2-settings)
  - [3. Key Rain](#3-key-rain)
  - [4. Personalization](#4-personalization)
  - [5. Importing and Exporting Configurations](#5-importing-and-exporting-configurations)
  - [6. Global Overlay](#6-global-overlay)
  - [7. Remembering Window Position and Size](#7-remembering-window-position-and-size)
  - [8. Full-Size Keyboard Layouts](#8-full-size-keyboard-layouts)
  - [9. Specially Supported Layouts](#9-specially-supported-layouts)
- [Performance and Compatibility](#performance-and-compatibility)
- [Important Notes](#important-notes)
- [Development Team](#development-team)
- [Copyright and License](#copyright-and-license)
- [Third-Party Resources](#third-party-resources)

---

## System Requirements and Running the Program

The program requires the following runtime environment:

- **Operating system:** `Windows 10/11`
- **GPU:** A graphics card and driver supporting `Direct3D 11` / `Direct2D`
- **Framework:** `.NET 8 Desktop Runtime (x64)`

If GPU initialization or `Present` fails while the program is running, it will automatically fall back to a CPU-based compatibility rendering mode.

Two program builds are available in `Release`:

- `JipperKeyViewer.exe`
- `JipperKeyViewer(.NET).exe`

If the required .NET framework is not installed, `JipperKeyViewer.exe` will display a prompt to download the required runtime.

If you download `JipperKeyViewer(.NET).exe`, which bundles the .NET framework, you can run it directly using the included .NET runtime.

> [!NOTE]
> This build is generally provided as a compatibility option for users who cannot download the .NET runtime normally. It has higher performance overhead.

## Main Features

### 1. Key Display and Source Capture

Key display is the core and most fundamental feature of this program.

Except for the full-size keyboard layouts, the other preset layouts are primarily designed for rhythm games, including the Key Rain feature described below.

**Source Capture:**

You can easily capture the key display window in any recording or streaming application that supports scenes (such as OBS Studio), show it in your scene, and use native transparency.

For example, to capture it in OBS Studio:

Create a **Game Capture** source in your scene, set the mode to **Capture specific window**, and select the `Jipper Key Viewer Diaplsy` window. Then enable **Allow Transparency** in the options below, as shown in the image:

![OBS Studio game capture settings](https://github.com/XueBaoT/JipperKeyViewer-Display/releases/download/Picture/OBS-1.png)

Click **OK**. The KeyViewer window will now appear transparently in your scene, as shown below:

![Transparent capture result in OBS Studio](https://github.com/XueBaoT/JipperKeyViewer-Display/releases/download/Picture/OBS-2.png)

---

### 2. Settings

Right-click the KeyViewer display window to open the context menu and access the settings page.

![KeyViewer settings page](https://github.com/XueBaoT/JipperKeyViewer-Display/releases/download/Picture/setting-main.png)

You can also configure a global hotkey under `Settings` > `Key Settings` to open the settings page, making it easier to adjust options quickly.

The default hotkey is `F8`.

---

### 3. Key Rain

Key Rain is a feature that lets you view and monitor the way keys are triggered and their current states in real time.

![Key Rain effect](https://github.com/XueBaoT/JipperKeyViewer-Display/releases/download/Picture/rainkeys.png)

All layouts except the full-size keyboard layouts (60K/75K/108K) support Key Rain, and their key mappings can be customized.

For ADOFAI, Key Rain also provides a **GhostRain** option, which can be enabled in the settings panel.

---

### 4. Personalization

The settings provide many options for personalizing the appearance of your key display. A few simple adjustments can make it look unique!

Options range from color customization to text styling. The individual configurable items are not listed here.

---

### 5. Importing and Exporting Configurations

On the `Settings` > `General` page, select the custom configuration you want to export and save it as a `.jkv` configuration file.

After the program has been launched for the first time, restarting the system will automatically associate `.jkv` files with the program and register the file icon.

You can double-click a configuration file to apply it, or drag the file directly onto the KeyViewer display window to apply the configuration.

---

### 6. Global Overlay

If you want to view key states on your screen in real time, this feature can create a transparent, global KeyViewer overlay on your display.

See the example below:

![Global overlay effect](https://github.com/XueBaoT/JipperKeyViewer-Display/releases/download/Picture/global_layered.png)

> [!WARNING]
> When the global overlay feature is enabled, the window becomes fully click-through and cannot receive mouse interaction.
>
> If you need to adjust the overlay's size or position, do so before enabling this feature.
>
> To disable the global overlay, use the global hotkey configured in Settings to open the settings page, or right-click the application icon in the system tray and disable it there.
>
> In addition, enabling this feature will immediately invalidate the **Game Capture** source in OBS Studio because the captured window is no longer available.

*Special thanks to @到处乱跑的隐藏者 for suggesting this feature.*

---

### 7. Remembering Window Position and Size

Whenever you move the key display window or resize it by dragging its edges, the program remembers the position and size before it closes. It will attempt to restore them the next time it starts, so you do not have to adjust them repeatedly.

~~(Unless you have tried deleting the configuration file.)~~

---

### 8. Full-Size Keyboard Layouts

The current version supports 60K, 75K, and 108K full-size keyboard layouts.

~~(Since the 108K and 87K layouts are almost identical, an 87K layout is not currently planned. It may be added if there is sufficient demand.)~~

---

### 9. Specially Supported Layouts

Based on the characteristics of falling-note rhythm games and the player population, we have created a dedicated **osu!mania** layout.

**Supported layouts:** 4K–9K

Key display for two-player mode may become possible in a future version after multi-process support is added.

---

## Performance and Compatibility

### Performance

The program has not undergone extensive testing during the Alpha and Beta stages. In most cases, performance estimates are based only on tests conducted on the developer's own devices.

At present, `JipperKeyViewer.exe` uses a reasonable amount of system resources and should run smoothly on most devices. However, if your device has limited hardware resources, running this program alongside other applications may cause:

- Reduced frame rates
- High CPU usage
- Noticeable input latency

This is not necessarily caused by a code implementation defect. The program uses a low-level hook to capture keyboard input. When a low-performance CPU is frequently under heavy load, the hook may compete for processing time with other tasks, resulting in increased input latency. This is especially noticeable during rapid, multi-key input.

### Compatibility

The program's compatibility has not yet been fully evaluated.

It is primarily developed for `Windows 10/11 x64`.

Other environments, such as `Windows 7/8` or `ARM64`, may not be supported. If you have tested the program in these environments and found that it cannot run reliably or correctly, but still choose to use it in an unsupported environment, **the developer accepts no responsibility for any resulting consequences, including but not limited to system crashes or data loss.**

*If you really need to use it, you can try asking an AI to clone the source repository, modify it, and compile a compatible version.*

## Important Notes

> [!IMPORTANT]
> ⚠️ Please read this entire `README.md` guide!
>
> ⚠️ Please read this entire `README.md` guide!
>
> ⚠️ Please read this entire `README.md` guide!

(AI was used to assist with approximately half of the program's development; the project was not developed entirely by AI.)

## Development Team

**Program Development:**

- XueBaoT

**Development Assistance:**

- DeepSeek (Agent)

**Artwork and UI Design:**

- Tsukyoze
- nainai奈

**Special Thanks:**

- 蘸料先生ZhanLiao
- 到处乱跑的隐藏者
- JipperKeyViewer Mod Development Team
- Team.XueBaoT

**Testing:**

- 渣渣
- 到处乱跑的隐藏者

## Copyright and License

**JipperKeyViewer Display**

Copyright © 2026 XueBaoT — MIT License

This program is released under the MIT License.

This program is an independent Windows desktop port and reconstruction based on the third-party mod `JipperKeyViewer` for the game **A Dance of Fire and Ice**:

**Third-party Mod:** `JipperKeyViewer`

Copyright © 2026 HitMargin — MIT License

[Repository](https://github.com/2228293026/JipperKeyViewer)

This program is not affiliated with, sponsored by, or endorsed by the original mod author, 7 Beat Games, or Microsoft. The name `JipperKeyViewer` is used solely to indicate its origin.

The MapleStory OTF Bold font bundled with this program is proprietary and is not covered by the MIT License. The copyright and redistribution terms for the font belong to its original rights holders (NEXON / FONTRIX). Before commercial distribution, please verify the license or replace it with a font that may be freely redistributed.

### Disclaimer

This program is provided based on the `JipperKeyViewer` mod, without any express or implied warranties. The developer accepts no responsibility for any direct or indirect losses resulting from the use of this program, including but not limited to streaming display issues, frame drops, or configuration loss. Please back up your configuration before use.

## Third-Party Resources

**The code's `MIT License` does not automatically cover bundled fonts, images, icons, or other resources.**
