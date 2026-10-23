# Milestone 2 — Hook + Capture + Decode Pipeline

## Completed

### MouseHookService (`src/Tunkoz/Services/MouseHookService.cs`)
Low-level right-click detection using WH_MOUSE_LL hook on a dedicated thread.

**Features:**
- Detects WM_RBUTTONUP events system-wide
- Ignores injected mouse events (LLMHF_INJECTED flag)
- Debounces rapid clicks at same position (< 100 ms window)
- Posts clicks to async Channel<T> without blocking the hook callback (critical for OS stability)
- Event-based API: `RightClickDetected` event fires with `(X, Y, Timestamp)`

**Key Implementation Details:**
- Hook callback returns immediately (zero work in callback)
- Maintains queue of recent clicks for debounce logic
- Uses Thread with dedicated message loop for hook operation
- Proper cleanup on Dispose()

### CaptureService (`src/Tunkoz/Services/CaptureService.cs`)
GDI-based screenshot capture of a region around the cursor.

**Features:**
- Captures square region centered on cursor (default 500×500 px, configurable)
- Uses GDI BitBlt for speed and memory efficiency
- Clamps region to virtual screen bounds (handles multi-monitor)
- Retry logic: if first pass fails, attempts at 2× size
- Timing instrumentation via Debug output
- Returns Bitmap in-memory only (never written to disk)

**Methods:**
- `CaptureAroundCursor(x, y)`: Single capture attempt
- `CaptureAroundCursorWithRetry(x, y)`: Capture with fallback to 2× size
- `CaptureSize` property: Get/set region size (clamped to ≥100 px)

### QrDetectionPipeline (`src/Tunkoz/Services/QrDetectionPipeline.cs`)
Orchestrates the complete flow: hook → capture → decode.

**Features:**
- Listens to `MouseHookService.RightClickDetected`
- On each right-click, asynchronously:
  1. Captures region (measures timing)
  2. Decodes with `QrDecoderService` (measures timing)
  3. Emits `QrDetected` event with results
- Emits only when QR(s) are found (no noise if nothing detected)
- Logs timings to Debug output: total ms, capture ms, decode ms
- Includes full `QrDetectionResult` with:
  - Cursor position
  - Capture and decode timings
  - List of `QrResult` (from Tunkoz.Core)
  - Detection timestamp

### WPF Main Application

**MainWindow.xaml / MainWindow.xaml.cs:**
- Simple UI with startup message
- "Open Debug Console" button
- "Exit" button with proper cleanup
- Initializes and starts `QrDetectionPipeline` on load
- Disposes pipeline on exit

**DebugConsoleWindow.xaml / DebugConsoleWindow.xaml.cs:**
- Real-time log of QR detection results
- Displays for each QR found:
  - Detection timestamp
  - Cursor position
  - Capture and decode timings
  - Payload type and content
  - Safety analysis (if URL)
  - Bounding box coordinates
- Pause/Resume button (stops logging without stopping detection)
- Clear button (clears log)
- Dark theme console for readability

## Architecture Diagram

```
MouseHookService (WH_MOUSE_LL on dedicated thread)
    ↓ RightClickDetected event
QrDetectionPipeline
    ├→ CaptureService.CaptureAroundCursorWithRetry()
    │   ↓ returns Bitmap
    ├→ QrDecoderService.DecodeFromBitmap() [from Tunkoz.Core]
    │   ├→ PayloadParser.Parse()
    │   ├→ LinkSafetyAnalyzer.Analyze() [for URLs]
    │   ↓ returns List<QrResult>
    └→ QrDetected event → DebugConsoleWindow logs results
```

## Testing (Windows Only)

**Prerequisites:**
- Windows 10 21H2 or later
- .NET 8 SDK installed
- Display scaling can be 100%, 150%, or 200%

**Build:**
```bash
dotnet build
```

**Run:**
```bash
dotnet run --project src/Tunkoz/Tunkoz.csproj
```

**Manual Testing:**
1. Launch the app (MainWindow appears)
2. Click "Open Debug Console"
3. Right-click a QR code visible on any part of the screen
   - Chrome: QR code in Google Search
   - Photos app: Any QR-encoded image
   - Test images: Copy a QR PNG to desktop, right-click it
4. Debug console shows detection results within ~150 ms
5. Check timings:
   - Capture should be < 50 ms
   - Decode should be < 30 ms
   - Total < 80 ms on typical laptop

**Expected Output:**
```
[14:32:15.042] QR Detection Result
Position: (512, 384)
Timings: Capture=28ms, Decode=12ms
Found: 1 QR code(s)
  - Type: Url
    Content: https://github.com/anthropics/tunkoz
    Safety: Safe (null)
    Domain: github.com
    Bounds: (450, 320) 120x120
```

**What NOT to Expect (Milestone 3+):**
- No result card next to native context menu (CardWindow → Milestone 3)
- No QR highlight overlay (HighlightOverlay → Milestone 3)
- No tray icon or settings (TrayService → Milestone 4)

## Known Limitations

1. **Hook requires Windows**: The WH_MOUSE_LL hook is Windows-only. Tunkoz.Core tests
   build on Linux, but this WPF app only runs on Windows.

2. **Non-elevated limitations**: The hook from a non-elevated process cannot see clicks
   on elevated (admin) windows. This is a Windows OS limitation, not a bug.

3. **Console-only feedback**: Debug console is the only output. Users see nothing visual
   until Milestone 3's CardWindow is implemented.

4. **No pause/resume of hook**: Once running, you can't pause detection from the UI.
   Pause button in debug console only pauses logging.

## Performance Metrics

Target: Capture + decode < 80 ms on typical laptop.

**Measured on reference hardware** (will update after Windows testing):
- [Pending Windows run]

**Optimization notes:**
- Bitmap operations are GDI (fast, native)
- ZXing.Net decode is optimized with TryHarder + TryInverted hints
- Async/await used to avoid blocking UI thread
- Hook callback does zero work (posts and returns immediately)

## Files Changed This Milestone

- `src/Tunkoz/` — New WPF app project
  - `Tunkoz.csproj` — Project file
  - `App.xaml`, `App.xaml.cs` — WPF application entry point
  - `MainWindow.xaml`, `MainWindow.xaml.cs` — Launcher window
  - `Services/MouseHookService.cs` — Low-level hook
  - `Services/CaptureService.cs` — GDI screenshot
  - `Services/QrDetectionPipeline.cs` — Orchestration
  - `Views/DebugConsoleWindow.xaml` — Debug UI
  - `Views/DebugConsoleWindow.xaml.cs` — Debug logic
- `Tunkoz.sln` — Added Tunkoz project

## Next Steps (Milestone 3)

- **Theming Infrastructure**: Tokens.Light.xaml, Tokens.Dark.xaml (from design artifact)
- **CardWindow**: Borderless, topmost result card with actions
- **HighlightOverlay**: Click-through window with accent outline around detected QR
- **Placement Logic**: Detect native context menu or anchor to QR bounds
- **Settings Window**: UI for general/detection/card/privacy settings
- **Tray Menu**: Pause/Resume, Settings, About, Exit

---

**Status:** Milestone 2 ✅ Complete — Ready for Milestone 3 review.
