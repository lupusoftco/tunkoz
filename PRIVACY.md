# Privacy Policy — Tünköz

## Summary

Tünköz respects your privacy. We collect nothing, store nothing remotely, and make no network calls by default.

## What Tünköz Does

### On Your Device (Local Only)

1. **Right-click observation**: When you right-click, Tünköz records the cursor position and timestamp. This data is processed immediately and discarded.
2. **Screenshot capture**: A 500×500 px region around your cursor is captured into memory, decoded, and destroyed.
3. **QR decoding**: The decoded text is parsed locally using ZXing.Net and our payload parser.
4. **URL safety analysis**: URLs are checked against offline pattern lists (no network queries).
5. **Settings storage**: Your preferences (currently just the UI language) are saved to `%AppData%\Tunkoz` as JSON.

### What Leaves Your Computer

**By default: nothing.**

**Optional features**: none yet. A planned "Expand short links" option would make a HEAD request to follow a shortened link's redirects; it will be off by default and clearly labelled in Settings.

## What Tünköz Does NOT Do

- ✗ Does not log or upload images, QR text, or decoded content
- ✗ Does not access your microphone, camera, or location
- ✗ Does not inject keyboard hooks (only observes right-clicks)
- ✗ Does not phone home with analytics or telemetry
- ✗ Does not sell or share your data
- ✗ Does not require an internet connection
- ✗ Does not require admin rights
- ✗ Does not modify files outside %AppData%\Tunkoz (except a `.vcf` contact you explicitly choose to save)

## Hook Behavior

The WH_MOUSE_LL hook used for right-click detection:
- Observes **right-button events only**
- Records cursor position (screen coordinates) and timestamp
- **Does not see keyboard input** (no keyboard hook installed)
- **Does not see clicks on elevated (admin) windows** from a non-elevated process (expected Windows limitation)
- Returns immediately to the OS (zero blocking)

## Threat Model

Tünköz assumes:
- Your Windows device is not compromised
- Your %AppData%\Tunkoz folder is readable only by your user account
- Network requests from optional features (if enabled) use a standard HTTPS connection

Tünköz does not protect against:
- Malicious QR code content (e.g., phishing URLs)—safety warnings help, but user judgment is final
- Malware on your device (no sandboxing)

## Settings & Data Location

All persistent data lives in:
```
%AppData%\Tunkoz\settings.json
```

Contents:
```json
{
  "Language": "system"
}
```

You can delete this file at any time to reset to defaults.

## Third-Party Code

- **ZXing.Net**: QR decoding library (Apache 2.0 license)
  - No network calls, local decode only
  - No telemetry
  - Source: https://github.com/micjahn/ZXing.Net

## Contact & Transparency

If you have privacy concerns or discover a privacy issue:
1. Open an issue on [GitHub](https://github.com/lupusoftco/tunkoz/issues) (public) or
2. Email tunkoz-security@example.com (private)

We will acknowledge and fix credible issues promptly.

## Changes to This Policy

This policy reflects Tünköz's design. If we change code in ways that affect privacy, we will update this document and release a new version. You can review the source code anytime at https://github.com/lupusoftco/tunkoz.

---

Last updated: October 2026
