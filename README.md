<div align="center">

# 🧹 Unbloat

**One tiny tray app that replaces Armoury Crate · Logitech G HUB · Creative App · MSI Center — for the things you actually use.**

![Platform](https://img.shields.io/badge/Platform-Windows%2010%2F11-0078D6?logo=windows)
![Language](https://img.shields.io/badge/Language-C%23-239120?logo=csharp)
![Built on](https://img.shields.io/badge/Built%20on-Equalizer%20APO-orange)
![License](https://img.shields.io/badge/License-MIT-green)

**English** | [简体中文](README.zh-CN.md)

</div>

Unbloat started as a way to stop running two heavyweight vendor suites just to "see the
mouse battery" and "auto-switch headset EQ for games". It ends up replacing four of them
with a single ~16 KB tray executable, all driven by the open-source
[Equalizer APO](https://sourceforge.net/projects/equalizerapo/).

> ⚠️ It is tuned around an **ASUS ROG Pelta** headset, a **Creative T60** speaker and a
> **Logitech G Pro Wireless** mouse — but everything is plain text config + two well-known
> protocols, so it's easy to adapt. See [Configuration](#-configuration).

## 🎯 Features

- **🎮 Per-game EQ auto-switching** — when a game from your list is the foreground window,
  the headset switches to a footstep-emphasis "FPS" EQ; otherwise a "Default" music/movie EQ.
- **🎧 Per-device profiles** — different EQ for the headset and the speakers, routed by
  Equalizer APO's `Device:` directive. The headset's EQ curves were extracted 1:1 from
  Armoury Crate's `.rog` files.
- **🖱️ Mouse battery, no G HUB** — reads the Logitech Lightspeed mouse battery directly over
  **HID++ 2.0** (BatteryVoltage feature).
- **🔊 Live volume readout** — current default-device name + volume %, via Windows Core Audio.
- **🪶 Lightweight & self-contained** — one C# WinForms tray app, no installer, no .NET
  beyond the Framework that ships with Windows.

## 🚀 Quick Start

### Prerequisites

- Windows 10 / 11
- [Equalizer APO](https://sourceforge.net/projects/equalizerapo/) installed and enabled on
  the playback device(s) you want to EQ (run its Configurator, tick your headset / speaker,
  reboot)
- .NET Framework 4.x (preinstalled on Windows)

### Build & set up

```bat
:: 1. compile the tray app
build.bat

:: 2. copy EQ profiles into EqualizerAPO + grant write access (one-time, asks for admin)
setup-eqapo.bat

:: 3. run it
PeltaTool.exe
```

A tray icon appears. Right-click it for status (EQ / volume / mouse battery) and
**Auto / Force FPS / Force Default**.

### Auto-start on login

Drop a shortcut to `PeltaTool.exe` into your Startup folder
(`Win+R` → `shell:startup`).

## 📂 Project Structure

```
PeltaTool/
├── src/
│   └── PeltaTool.cs          # the tray app: EQ switch + volume + mouse battery
├── eq/
│   ├── config.txt            # EqualizerAPO entry — routes EQ per device
│   ├── pelta-fps.txt         # headset: footstep / FPS EQ
│   ├── pelta-default.txt     # headset: music & movie EQ
│   ├── t60.txt               # Creative T60 speaker EQ
│   └── games.txt             # apps that trigger the FPS profile
├── scripts/
│   └── kill-armoury.bat      # stop Armoury Crate (handy before uninstalling)
├── docs/
│   └── reverse-engineering.md # how the headset/mouse/EQ were figured out
├── build.bat                 # compile with the built-in .NET Framework csc
├── setup-eqapo.bat           # copy EQ profiles + grant write access (admin, one-time)
└── LICENSE
```

## ⚙️ Configuration

| What | Where | Notes |
|---|---|---|
| Games that use the FPS EQ | `eq/games.txt` | one `exe` name per line; tray → *Reload game list* |
| Headset EQ curves | `eq/pelta-fps.txt`, `eq/pelta-default.txt` | standard Equalizer APO filter syntax |
| Speaker EQ | `eq/t60.txt` | — |
| Device routing | `eq/config.txt` | `Device:` does a **substring** match (no wildcards) — `Device: PELTA`, not `*PELTA*` |
| EqualizerAPO path | `src/PeltaTool.cs` (`ApoDir`) | defaults to `C:\Program Files\EqualizerAPO\config` |
| Mouse VID/PID | `src/PeltaTool.cs` (`Bat.VID/PID`) | defaults to the Logitech Lightspeed receiver `046D:C539` |

> 💡 Virtual surround ("听声辨位") is provided by **Windows Sonic for Headphones** — enable it
> once per device (right-click the volume icon → *Spatial sound*). PeltaTool only handles the EQ.

## 🛠️ How It Works

PeltaTool writes Equalizer APO's `config.txt` on the fly (EQ APO auto-reloads it), pointing
the headset `Device:` block at the FPS or Default EQ depending on the foreground process.
Volume comes from Core Audio (`IMMDeviceEnumerator` / `IAudioEndpointVolume`); the mouse
battery comes from HID++ 2.0 over the Logitech receiver. Everything runs in one WinForms
tray process; the battery poll lives on a background thread so the UI never blocks.

## 🔬 Reverse Engineering

The interesting bits — why the ROG Pelta's DSP can't be moved off Armoury Crate by HID alone,
the `.rog` / Creative JSON profile formats, the HID++ battery sequence, and the Equalizer APO
`Device:` substring gotcha — are written up in
[`docs/reverse-engineering.md`](docs/reverse-engineering.md).

## 🤝 Contributing

Issues and PRs welcome — especially configs/curves for other headsets, speakers and Logitech
devices. Keep it dependency-free (Framework + Win32) so it stays a single small exe.

## 📄 License

[MIT](LICENSE)

## 🙏 Acknowledgments

- [Equalizer APO](https://sourceforge.net/projects/equalizerapo/) — the open-source audio engine that makes this possible
- [LGSTrayBattery](https://github.com/andyvorld/LGSTrayBattery) & [Solaar](https://github.com/pwr-Solaar/Solaar) — references for Logitech HID++ battery
- ASUS, Creative and Logitech — for shipping software heavy enough to make this worth building 😄

<div align="center"><sub><a href="#-unbloat">↑ Back to top</a></sub></div>
