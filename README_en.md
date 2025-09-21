# frp Android Client

[![Stars](https://img.shields.io/github/stars/qiuhaotc/frp_android)](https://github.com/qiuhaotc/frp_android)
[![Forks](https://img.shields.io/github/forks/qiuhaotc/frp_android)](https://github.com/qiuhaotc/frp_android)
[![License](https://img.shields.io/github/license/qiuhaotc/frp_android)](https://github.com/qiuhaotc/frp_android)
[![Release](https://img.shields.io/github/v/release/qiuhaotc/frp_android)](https://github.com/qiuhaotc/frp_android/releases)
[![Downloads](https://img.shields.io/github/downloads/qiuhaotc/frp_android/total.svg)](https://github.com/qiuhaotc/frp_android/releases)
[![Issues](https://img.shields.io/github/issues/qiuhaotc/frp_android)](https://github.com/qiuhaotc/frp_android)

English | [中文文档](README.md)

A lightweight .NET MAUI Android app to run and manage frp (both frpc & frps) directly on a phone / TV box.

![Screenshot](FrpInfo/screenshot.jpg)

## Features

- Start / stop frpc and frps (bundled native binaries, no extra download)
- Auto-generate default `frpc.ini` / `frps.ini` on first launch
- In-app config editing & saving
- Real-time log view (ANSI colors / control chars stripped)
- Foreground service to reduce process killing
- No root, small footprint, clean UI

## Quick Start

1. Install & open the app
2. First launch writes default `frpc.ini` / `frps.ini`
3. Edit configs in-app (e.g. expose an intranet service)
4. Start frpc or frps and watch logs to verify

Stop: tap the corresponding stop button. Force closing / system cleanup will terminate tunnels.

## Build

Requirements:

- .NET 10+ (targets include `net10.0-android`)
- Android SDK / build tools (via Visual Studio or CLI)

Build (Release example):

```powershell
dotnet build -c Release
```

## Structure

```text
src/
  Platforms/Android/        # Foreground service & entry Activity
  Services/                 # Runtime management logic
  Resources/Raw/            # Bundled frp binaries & assets
  Components/Pages/         # .NET MAUI / Razor UI
```

## Notes

- Default configs are examples: harden & adjust before exposing services publicly
- System battery / background limits may still terminate the process

## Roadmap (potential)

- Simple graphical config wizard
- Multiple config profiles / templates
- Keep-alive & scheduled restart

## License

Apache License 2.0 (see `LICENSE.txt`).

## Contributing

Issues & PRs are welcome.

---

frp itself belongs to its upstream project; this app only provides a local management UI.
