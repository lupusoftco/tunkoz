# Tünköz

Right-click any QR code on Windows. Decode instantly.

**Tünköz** (Old Turkic: "night-eye") brings macOS Live Text QR detection to Windows—system-wide, non-invasive. Right-click a QR code anywhere (Chrome, Edge, PDF, chat, photos) and a Fluent Design card appears next to your cursor with the decoded content and one-click actions.

![Tünköz hero](docs/hero.png)

## Features

- **Instant QR detection** on right-click, anywhere on screen
- **No invasive hotkeys or region selection**—native right-click workflow
- **Safety-first**: warnings for suspicious URLs, punycode domains, IP addresses, and blocked schemes
- **Multiple payload types**: URLs, WiFi, vCards, phone/email, text
- **Fluent Design** for Windows 11, with light/dark theme and multi-DPI support
- **100% offline**, no telemetry, no image logging
- **Lightweight**: single-file executable, < 60 MB memory, near-zero idle CPU

## Quick Start

### Install

**From executable**  
Download the latest `Tunkoz.exe` from [Releases](https://github.com/anthropics/tunkoz/releases).

**From source**  
```bash
git clone https://github.com/anthropics/tunkoz
cd tunkoz
dotnet build -c Release src/Tunkoz/Tunkoz.csproj
```

### Build & Test

```bash
# Restore and build all projects
dotnet build

# Run tests
dotnet test

# Build release exe
dotnet publish -c Release -o bin/Release src/Tunkoz/Tunkoz.csproj
```

## How It Works

1. **Right-click anywhere** on screen (Chrome, Edge, WhatsApp, PDF viewer, etc.)
2. **Native context menu opens normally** (nothing is swallowed)
3. **Tünköz card appears** within ~150ms if a QR code is detected near the cursor
4. **Choose an action**: Open URL · Copy link · Save contact · etc.
5. **Card auto-dismisses** after 8 seconds (or on next click)

### Safety Analysis

- 🟢 **Safe**: HTTPS URLs to trusted domains
- 🟡 **Warning**: Plain HTTP, punycode domains, shortened links, IP addresses, suspicious patterns
- 🔴 **Blocked**: `javascript:`, `data:`, `file:`, `ms-*` schemes (copy-only, never opened)

## Architecture

### Tunkoz.Core

Platform-agnostic payload parsing, URL safety analysis, and QR decoding.

- **PayloadParser**: Classifies QR content (URL, WiFi, vCard, tel, mailto, sms, geo, text)
- **LinkSafetyAnalyzer**: Detects unsafe URL patterns offline
- **QrDecoderService**: Decodes QR codes using ZXing.Net, returns bounding boxes

### Tunkoz (WPF UI)

Windows-specific components (hooks, capture, UI).

- **MouseHookService**: Right-click detection via WH_MOUSE_LL hook
- **CaptureService**: GDI screenshot of 500×500 px (configurable) region around cursor
- **CardWindow**: Borderless, topmost result card with actions
- **HighlightOverlay**: Accent outline around detected QR bounds (~300 ms fade)
- **Settings**: Theme, language, detection region size, card timeout, fullscreen exclusions
- **Tray**: Pause/Resume, Settings, Start with Windows, About, Quit

## Requirements

- **Windows 10 21H2** or later
- **.NET 8 Runtime** (bundled in single-file exe)
- **No admin rights needed**

## Privacy

- ✓ No network calls (offline by default)
- ✓ No telemetry, no image logging
- ✓ Hook only observes right-click position (no keystroke logging)
- ✓ All settings stored locally in `%AppData%\Tunkoz`
- ⚠ Optional "Expand short links" feature makes HEAD requests to follow redirects

See [PRIVACY.md](PRIVACY.md) for details.

## Roadmap

1. **Milestone 1** ✓ — Core payload parsing, safety analysis, QR decoder with unit tests
2. **Milestone 2** — Hook + capture + decode pipeline, CardWindow with all states
3. **Milestone 3** — Theming, tray, settings, localization, design hand-off
4. **Milestone 4** — CI/CD, release workflow, winget package
5. **Milestone 5** — Code signing via SignPath (free OSS tier)

## Contributing

Contributions welcome! Please read [CONTRIBUTING.md](CONTRIBUTING.md) first.

## License

MIT License — see [LICENSE](LICENSE) for terms.

## Acknowledgments

- **ZXing.Net** for QR decoding
- **Windows 11 Fluent Design System** for UI aesthetics
- Inspired by macOS Live Text and the need for the same on Windows

---

Built with ❤️ by the Tünköz team.  
[GitHub](https://github.com/anthropics/tunkoz) | [Issues](https://github.com/anthropics/tunkoz/issues) | [Discussions](https://github.com/anthropics/tunkoz/discussions)
