# Tünköz — Project Specification

## Project: Tünköz — right-click any QR code on Windows

### Name
- Display name (UI, README title, tray tooltip): **Tünköz**
- Identifiers (repo, namespaces, executable, paths): **Tunkoz**
- Meaning: Old Turkic *tün* (night) + *köz* (eye) → "night-eye", the eye that sees in the dark.
  Mention this in one line in the README.

### Context
On macOS, right-clicking a QR code in Safari/Preview shows "Open link / Copy link" via Live Text.
Windows has nothing equivalent. The only built-in option is Snipping Tool → Text actions (4+ steps).
Existing open-source tools (QuickScan, Snapboard, etc.) all use "hotkey + drag a region".

Tünköz brings the macOS experience to Windows, system-wide: the user right-clicks on a QR code
anywhere on screen (any app, any image, video frame, PDF, chat window) and a small card appears
next to the app's own context menu with the decoded content and actions.

### Core principle — never interfere with the click
- Observe right-clicks only. Never swallow, delay or modify mouse input.
- The app's native context menu opens normally. Tünköz's card appears beside it.
- If no QR is found, nothing visible happens.

### Tech stack
- C# / .NET 8, WPF for UI, Win32 interop (P/Invoke) for hooks, capture and DWM window styling.
- QR decoding: ZXing.Net (enable TryHarder, TryInverted, multiple-results).
- No UI component libraries (no WPF-UI, MahApps, etc.). Fluent look is built with our own
  styles + DWM APIs. Ask me before adding any dependency beyond ZXing.Net and the Public Suffix List.
- Single tray application, no admin rights, no network access by default.
- Per-Monitor DPI Aware v2 (app.manifest). Must work correctly on mixed-DPI multi-monitor setups.
- Project must build on Linux with `<EnableWindowsTargeting>true</EnableWindowsTargeting>`
  (for compile checks), but real runs happen on Windows. I test on Windows and report back.

## Architecture (modules)

### 1. MouseHookService
   - WH_MOUSE_LL hook on a dedicated thread with its own message loop.
   - On WM_RBUTTONUP: record cursor position (physical pixels) + timestamp, post to a channel,
     call CallNextHookEx and return immediately. Zero work inside the callback
     (Windows silently removes slow LL hooks).
   - Debounce rapid clicks; ignore injected events (LLMHF_INJECTED).

### 2. CaptureService
   - Capture a square region centered on the cursor (default 500×500 physical px,
     configurable), clamped to the containing monitor's bounds, via GDI BitBlt.
   - If the first pass finds a partial/no QR, do a second pass at 2× region size.
   - Images exist only in memory, never written to disk.

### 3. QrDecoderService
   - Decode with ZXing.Net. Return all results with their bounding boxes
     (converted back to screen coordinates).
   - If multiple QRs are found, pick the one whose bounds contain or are closest to the cursor.
   - Performance budget: capture + decode < 80 ms on a typical laptop. Log timings in debug builds.

### 4. PayloadParser — classify decoded content:
   - URL (http/https/other schemes), WIFI:, BEGIN:VCARD / MECARD, tel:, mailto:, SMSTO:, geo:, plain text.
   - Each type maps to actions (see Card).

### 5. LinkSafetyAnalyzer (offline, pure logic)
   - Extract and display the registrable domain prominently (use a bundled Public Suffix List).
   - Warn on: punycode/IDN homoglyph domains (show decoded form), raw IP hosts, plain http,
     known URL shorteners (bundled list), unusually long/obfuscated URLs, non-http schemes
     (javascript:, data:, file:, ms-* protocols) → those must never be "opened", only copied.
   - Output a severity level: Safe / Warning / Blocked, plus localized reason keys.
   - Optional, OFF by default: "Expand short links" (HEAD request following redirects, shows final domain).

### 6. CardWindow (WPF) — see Design section for visuals
   - Borderless, topmost, WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW so it never steals focus
     and never dismisses the native context menu.
   - Placement: try to detect the native popup menu (window class "#32768") ~120 ms after the click
     and dock beside it; if not found (Chrome/Electron/WinUI menus), anchor to the QR's
     bounding box (above or below, whichever fits the monitor). Never cover the cursor position.
   - Briefly outline the detected QR on screen (separate click-through overlay window,
     ~300 ms fade) so the user knows what was read.
   - Actions by type:
     - URL: Open link · Copy link
     - WiFi: Copy password · Copy network name
     - vCard: Copy name/phone/email · Save as .vcf
     - tel/mailto/sms: Copy · Open with default app
     - Text: Copy text
   - Dismiss on: outside click, Esc, another right-click, or timeout (default 8 s, paused on hover).

### 7. TrayService & Settings
   - Tray icon (Active / Paused states) with: Pause/Resume, Settings, Start with Windows, About, Quit.
   - Settings (JSON in %AppData%\Tunkoz): language, theme (system/light/dark), region size,
     card timeout, disable in fullscreen apps, excluded processes list, expand short links (off).
   - Skip detection when foreground window is fullscreen (games/presentations) if enabled.

## Design & UI

Visual assets and the design system are being produced separately in Claude Design.
Your job is to build the UI so those assets drop in cleanly.

### Brand direction (use until real tokens arrive)
- Personality: quiet, precise, trustworthy, a bit mysterious. "An eye that sees in the dark."
- Palette: deep night indigo / near-black base, one warm amber "eye glow" accent, neutral greys.
- Subtle Orkhon-runic / QR-finder-pattern geometry in the logo only, never in UI chrome.
- UI feels native to Windows 11 Fluent: Segoe UI Variable (fallback Segoe UI),
  8 px corner radius, 4 px spacing grid, soft shadows.

### Theming architecture (mandatory)
- All colors, brushes, font sizes, spacing, radii and shadows live in
  `src/Tunkoz/Themes/Tokens.Light.xaml` and `Tokens.Dark.xaml` (semantic names, e.g.
  `Brush.Surface`, `Brush.TextPrimary`, `Brush.Accent`, `Brush.WarningBg`, `Brush.DangerBg`,
  `Radius.Card`, `Space.M`). Control styles in `Themes/Controls.xaml` reference only tokens.
- No hard-coded colors or sizes anywhere in views.
- Follow Windows app theme (registry AppsUseLightTheme) live; user can override in Settings.
- Use DWM: DWMWA_WINDOW_CORNER_PREFERENCE for rounded corners, DWMWA_USE_IMMERSIVE_DARK_MODE
  for title bars, DWMWA_SYSTEMBACKDROP_TYPE (Mica) for Settings/About on Windows 11 22H2+,
  solid token background as fallback (Windows 10 and the card window).
- Respect Windows "reduce animations" setting and high-contrast mode.

### Design hand-off (Claude Design artifact)
Design artifact: https://claude.ai/artifact/5HnBgw3JyjPGMmWvuJdCpa

I will give you a Claude Design artifact link (claude.ai) containing the design system and mockups.
When I share it:
- Read the artifact and extract: color tokens (light/dark), type scale, spacing, radius, shadows,
  logo and icon SVGs, and the reference screens (card states, settings, about, tray menu).
- Commit what you extracted into the repo so the project is self-contained and doesn't depend
  on the link: `/design/tokens.json`, `/design/logo/*.svg`, `/design/icons/*.svg`.
  Then map tokens.json → Tokens.Light.xaml / Tokens.Dark.xaml, generate `Tunkoz.ico` (16–256)
  and tray icons (.ico, light/dark taskbar, active/paused), and match views to the mockups.
- If you cannot open the link (private, auth required, unsupported), tell me immediately
  instead of guessing the design; I'll make it accessible or paste the content.
- When I send an updated artifact link later, diff it against /design and update only what changed.

Until the link arrives, use placeholder tokens and a simple placeholder icon, keeping the
theming structure ready.

### Screens to implement
1. **Result card** (~320 px wide): type icon, registrable domain large & bold, full content
   truncated below, primary + secondary action buttons, inline "Copied ✓" micro-state,
   optional thin auto-dismiss progress line. States: Safe URL, Warning URL (amber banner with
   reason), Blocked scheme (red, copy-only, explanation), WiFi, Contact (vCard rows with copy
   buttons), Plain text. Warnings always pair icon + text, never color alone (WCAG AA).
2. **QR highlight overlay**: click-through, accent outline around detected QR bounds.
3. **Tray menu**: native-feeling Windows 11 style.
4. **Settings window**: single page with sections General / Detection / Card / Privacy;
   Privacy section states Tünköz works offline and stores nothing, and explains that
   "Expand short links" makes a network request.
5. **About window**: logo, version, name story in one line, GitHub link, MIT license.

### Localization
- All UI strings in .resx (`Strings.resx` English default, `Strings.tr.resx` Turkish).
  Default to Windows UI language; switchable in Settings without restart if feasible.

## Privacy & trust (put this in README too)
- No network calls by default, no telemetry, no image or content logging.
- Hook only observes right-click position; it never reads keystrokes and never registers a keyboard hook.
- Note in docs: hooks from a non-elevated process don't see clicks on elevated (admin) windows — expected.

## Repo structure
```
/src/Tunkoz              (WPF app, output: Tunkoz.exe)
  /Themes                (Tokens.Light.xaml, Tokens.Dark.xaml, Controls.xaml)
  /Views                 (CardWindow, HighlightOverlay, SettingsWindow, AboutWindow)
  /Resources             (Strings.resx, Strings.tr.resx, icons)
/src/Tunkoz.Core         (PayloadParser, LinkSafetyAnalyzer, QrDecoder — no UI, no Win32)
/tests/Tunkoz.Core.Tests (xUnit: parser, safety analyzer, decoder with sample images)
/tests/samples           (QR images: normal, inverted, tiny, rotated, multiple, low contrast)
/design                  (tokens + SVGs extracted from the Claude Design artifact, committed)
/docs                    (README images: hero banner, social preview, feature illustrations)
/.github/workflows       (build + test on windows-latest, release artifacts on tag)
README.md, LICENSE, CONTRIBUTING.md, PRIVACY.md
```

## Release
- GitHub Actions on windows-latest: build, test, publish self-contained win-x64 single-file exe + zip.
- Prepare for code signing via SignPath (free for OSS) but don't block on it.
- Add a winget manifest template (package id: Tunkoz.Tunkoz).
- README: hero banner from /docs, tagline "Right-click any QR code on Windows.",
  demo GIF placeholder, feature list, comparison with Snipping Tool / region-select tools,
  privacy section, install (exe, winget), build from source.

## Milestones — implement in this order, stop after each for review

1. **Milestone 1** ✅ — Tunkoz.Core: PayloadParser + LinkSafetyAnalyzer + decoder with unit tests and sample images.
2. **Milestone 2** — Hook + capture + decode pipeline, results printed to a debug console window.
3. **Milestone 3** — Theming infrastructure (tokens, Controls.xaml, DWM helpers) + CardWindow with all states,
   placement logic, highlight overlay and actions.
4. **Milestone 4** — Tray, settings, about, autostart, localization; wire in the Claude Design artifact if shared.
5. **Milestone 5** — CI, release workflow, README, PRIVACY.md.

## Acceptance criteria
- Right-clicking a QR in Chrome, Edge, WhatsApp Desktop, Photos, a PDF viewer and a paused
  YouTube video shows the card within ~150 ms; native menu still works untouched.
- Right-clicking anywhere without a QR has no visible effect and no noticeable lag.
- javascript:/data:/file: payloads can never be opened, only copied.
- Card never steals focus; native menu stays open while the card is visible.
- Light/dark switch follows Windows live; all text meets WCAG AA contrast.
- Works on 100%, 150%, 200% DPI and multi-monitor setups.
- Idle CPU ≈ 0%, memory < 60 MB.

## Working rules
- Code, comments, commit messages and README in English.
- When I report a bug from Windows, ask for Windows version, display scaling and the app
  where the click happened before changing code.
- When changing code later, show only the changed files.

## Current Status

**Milestone 1** ✅ Complete
- Tunkoz.Core library fully implemented
- PayloadParser handles all 8 payload types
- LinkSafetyAnalyzer with comprehensive URL safety detection
- QrDecoderService with ZXing.Net integration
- 29 unit and integration tests with programmatically-generated sample QR codes
- Complete documentation (README, PRIVACY, CONTRIBUTING)

**Milestone 2** ✅ Complete
- MouseHookService: WH_MOUSE_LL right-click detection on dedicated thread
- CaptureService: GDI BitBlt region capture with retry logic
- QrDetectionPipeline: Full hook → capture → decode orchestration
- Main WPF app with debug console showing real-time detections
- Performance instrumentation (capture time, decode time, total time)
- Ready to test on Windows; prints results to console

**Next:** Proceed to Milestone 3 (CardWindow, theming from design artifact, tray, settings)
