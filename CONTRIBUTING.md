# Contributing to Tünköz

Thank you for your interest in contributing! We welcome bug reports, feature ideas, code contributions, and documentation improvements.

## Before You Start

- Read [README.md](README.md) and [PRIVACY.md](PRIVACY.md)
- Review the [project specification](https://github.com/anthropics/tunkoz/wiki/Architecture) (milestones, tech stack, acceptance criteria)
- Check [open issues](https://github.com/anthropics/tunkoz/issues) to avoid duplication

## Reporting Bugs

Open a [GitHub issue](https://github.com/anthropics/tunkoz/issues/new?template=bug_report.md) with:
1. **Windows version** (11 22H2, 10 21H2, etc.)
2. **Display scaling** (100%, 150%, 200%)
3. **Exact app** where the click happened (Chrome, Edge, WhatsApp, PDF, etc.)
4. **Steps to reproduce**
5. **Expected vs. actual behavior**
6. **Screenshot or GIF** (if relevant)

Example:
> **Windows 11 22H2, 150% scaling, Chrome**
> 
> Right-click a QR code in a GitHub issue → Card appears but text is cut off.
> Expected: Full card width on 150% DPI.

## Submitting Code

### Setup

```bash
git clone https://github.com/anthropics/tunkoz
cd tunkoz
dotnet build
dotnet test
```

### Process

1. **Create a branch** from `main`:
   ```bash
   git checkout -b fix/card-truncation
   ```

2. **Make changes** following these rules:
   - Edit existing files; don't add new files unless necessary
   - Write no comments (well-named identifiers say what code does; explain WHY only when non-obvious)
   - No error handling for scenarios that can't happen
   - No feature flags or backwards-compat shims
   - Keep it minimal—don't refactor unrelated code

3. **Update tests** if you change logic:
   ```bash
   dotnet test
   ```

4. **Commit with a clear message**:
   ```bash
   git commit -m "Fix card truncation on 150% DPI

   CardWindow was not respecting DPI-scaled width calculation.
   Constrained layout to logical pixels instead of physical."
   ```

5. **Push and open a PR**:
   ```bash
   git push origin fix/card-truncation
   ```

### Pull Request Checklist

- [ ] Describe what changed and why (not just "update code")
- [ ] Reference related issues (#123)
- [ ] Tests pass locally (`dotnet test`)
- [ ] Code follows the style (C# naming conventions, 4-space indent, no tabs)
- [ ] No new dependencies (ask first if needed)
- [ ] Windows version + scaling + repro app mentioned (for UI changes)

## Code Style

- **Language**: English in code, comments, and commit messages
- **Naming**: PascalCase for classes/methods, camelCase for locals, CONSTANT_CASE for constants
- **Formatting**: 4-space indent, no trailing whitespace, max 120 chars per line (soft)
- **Nullability**: Use `nullable enable` and `ArgumentNullException.ThrowIfNull`
- **No comments** unless the WHY is non-obvious (e.g., a workaround for a specific bug)

## Milestones & Feature Requests

Tünköz follows a phased roadmap. New features are welcome but may be deferred to a later milestone:

1. **Milestone 1**: Core parsing, safety, decoder + tests ✓
2. **Milestone 2**: Hook, capture, CardWindow
3. **Milestone 3**: Theming, tray, settings, i18n
4. **Milestone 4**: CI/CD, release, winget
5. **Milestone 5**: Code signing

If you want to work on a feature across multiple milestones, open a [discussion](https://github.com/anthropics/tunkoz/discussions) first to align on scope and priority.

## Documentation

- README updates: keep it short; link to wiki for deep dives
- PRIVACY.md: update if code changes what data is collected/sent
- Code comments: only for non-obvious logic
- Commit messages: one line summary, blank line, then details

## Testing

- Unit tests for all core logic (PayloadParser, LinkSafetyAnalyzer, QrDecoderService)
- Integration tests for complete pipelines
- Use xUnit fixtures for shared setup (sample images, etc.)
- Run `dotnet test` before pushing

## Performance & Correctness

- Capture + decode must complete in **< 80 ms** on a typical laptop
- No memory leaks: test with large batches of right-clicks
- Multi-DPI setups: test on 100%, 150%, 200% scaling if possible
- No hard-coded paths; use `%AppData%\Tunkoz` and temp directories

## Questions?

- **Usage**: Open a [discussion](https://github.com/anthropics/tunkoz/discussions)
- **Architecture**: Check the [wiki](https://github.com/anthropics/tunkoz/wiki)
- **Security**: Email tunkoz-security@example.com (private)

---

Thank you for contributing! 🎉
