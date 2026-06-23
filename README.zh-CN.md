<div align="center">

# 🧹 Unbloat

**一个迷你托盘程序,替代 Armoury Crate · 罗技 G HUB · Creative App · MSI Center —— 只保留你真正在用的功能。**

![Platform](https://img.shields.io/badge/Platform-Windows%2010%2F11-0078D6?logo=windows)
![Language](https://img.shields.io/badge/Language-C%23-239120?logo=csharp)
![Built on](https://img.shields.io/badge/Built%20on-Equalizer%20APO-orange)
![License](https://img.shields.io/badge/License-MIT-green)

[English](README.md) | **简体中文**

</div>

Unbloat 的起因很简单:不想为了"看个鼠标电量"和"打游戏自动切耳机 EQ",就常驻两套又重又占资源的厂商全家桶。最终它用**一个约 16 KB 的托盘程序**替代了四套,全部基于开源的
[Equalizer APO](https://sourceforge.net/projects/equalizerapo/)。

> ⚠️ 它是围绕 **华硕 ROG Pelta 耳机**、**Creative T60 音箱** 和 **罗技 G Pro Wireless 鼠标** 调的,
> 但所有配置都是纯文本 + 两个通用协议,很容易改成你自己的设备,见 [配置](#-配置)。

## 🎯 功能

- **🎮 按游戏自动切 EQ** —— 当列表里的游戏处于前台时,耳机切到强调脚步的 "FPS" EQ;否则用 "Default" 影音 EQ。
- **🎧 分设备配置** —— 耳机和音箱用不同 EQ,靠 Equalizer APO 的 `Device:` 分流;耳机的 EQ 曲线是从 Armoury Crate 的 `.rog` 文件 **1:1 提取**出来的。
- **🖱️ 免 G HUB 看鼠标电量** —— 直接走 **HID++ 2.0** 协议读罗技无线鼠标电压。
- **🔊 实时音量** —— 通过 Windows Core Audio 显示当前默认设备名 + 音量百分比。
- **🪶 轻量自包含** —— 一个 C# WinForms 托盘程序,无需安装、不需要额外 .NET(用 Windows 自带的 Framework)。

## 🚀 快速开始

### 前置条件

- Windows 10 / 11
- 安装 [Equalizer APO](https://sourceforge.net/projects/equalizerapo/),并在要做 EQ 的播放设备上启用(运行它的 Configurator,勾选你的耳机/音箱,重启)
- .NET Framework 4.x(Windows 自带)

### 编译 & 配置

```bat
:: 1. 编译托盘程序
build.bat

:: 2. 把 EQ 配置拷进 EqualizerAPO + 授予写权限(一次性,会请求管理员)
setup-eqapo.bat

:: 3. 运行
PeltaTool.exe
```

托盘会出现一个图标。右键查看状态(EQ / 音量 / 鼠标电量),以及 **Auto / Force FPS / Force Default**。

### 开机自启

把 `PeltaTool.exe` 的快捷方式丢进启动文件夹(`Win+R` → `shell:startup`)。

## 📂 项目结构

```
PeltaTool/
├── src/
│   └── PeltaTool.cs          # 托盘程序:EQ 切换 + 音量 + 鼠标电量
├── eq/
│   ├── config.txt            # EqualizerAPO 入口 —— 按设备分流 EQ
│   ├── pelta-fps.txt         # 耳机:脚步 / FPS EQ
│   ├── pelta-default.txt     # 耳机:音乐 & 影视 EQ
│   ├── t60.txt               # Creative T60 音箱 EQ
│   └── games.txt             # 触发 FPS 配置的程序列表
├── scripts/
│   └── kill-armoury.bat      # 停止 Armoury Crate(卸载前测试用)
├── docs/
│   └── reverse-engineering.md # 耳机/鼠标/EQ 的逆向过程
├── build.bat                 # 用 Windows 自带的 .NET Framework csc 编译
├── setup-eqapo.bat           # 拷 EQ 配置 + 授权(管理员,一次性)
└── LICENSE
```

## ⚙️ 配置

| 项目 | 位置 | 说明 |
|---|---|---|
| 触发 FPS EQ 的游戏 | `eq/games.txt` | 每行一个 `exe` 名;托盘 → *Reload game list* 重载 |
| 耳机 EQ 曲线 | `eq/pelta-fps.txt`、`eq/pelta-default.txt` | 标准 Equalizer APO 滤波器语法 |
| 音箱 EQ | `eq/t60.txt` | —— |
| 设备分流 | `eq/config.txt` | `Device:` 是**子串匹配**(不支持通配符)—— 写 `Device: PELTA`,不是 `*PELTA*` |
| EqualizerAPO 路径 | `src/PeltaTool.cs`(`ApoDir`) | 默认 `C:\Program Files\EqualizerAPO\config` |
| 鼠标 VID/PID | `src/PeltaTool.cs`(`Bat.VID/PID`) | 默认罗技 Lightspeed 接收器 `046D:C539` |

> 💡 虚拟环绕(听声辨位)用 **Windows Sonic for Headphones** 实现 —— 每个设备开一次即可(右键音量图标 → *空间音效*)。PeltaTool 只负责 EQ。

## 🛠️ 工作原理

PeltaTool 实时改写 Equalizer APO 的 `config.txt`(EQ APO 会自动重载),根据前台进程把耳机的 `Device:` 块指向 FPS 或 Default 的 EQ。音量来自 Core Audio(`IMMDeviceEnumerator` / `IAudioEndpointVolume`);鼠标电量来自罗技接收器上的 HID++ 2.0。一切都在一个 WinForms 托盘进程里运行,电量轮询放在后台线程,界面不卡顿。

## 🔬 逆向工程

最有意思的部分 —— 为什么 ROG Pelta 的 DSP 没法只靠 HID 脱离 Armoury Crate、`.rog` / Creative 的 JSON 配置格式、HID++ 读电量的流程,以及 Equalizer APO `Device:` 子串匹配的坑 —— 都写在
[`docs/reverse-engineering.md`](docs/reverse-engineering.md) 里。

## 🤝 贡献

欢迎 Issue / PR —— 尤其是其他耳机、音箱、罗技设备的配置和 EQ 曲线。请保持零依赖(Framework + Win32),让它始终是一个小巧的单 exe。

## 📄 许可证

[MIT](LICENSE)

## 🙏 致谢

- [Equalizer APO](https://sourceforge.net/projects/equalizerapo/) —— 让这一切成为可能的开源音频引擎
- [LGSTrayBattery](https://github.com/andyvorld/LGSTrayBattery) & [Solaar](https://github.com/pwr-Solaar/Solaar) —— 罗技 HID++ 电量的参考
- 华硕、创新、罗技 —— 感谢它们的软件臃肿到值得我自己造一个 😄

<div align="center"><sub><a href="#-unbloat">↑ 回到顶部</a></sub></div>
