# Task 8: Docs and release notes

## What changed
- `src/docs/reference/modules/Settings/README.md`: new `### Accessibility` subsection under `## General settings`, placed after the General settings table and before the Debugging sentence. It covers:
  - the tab's location (after Site) and the `Manage general settings` permission
  - a one-row table for "Success message dismissal delay (seconds)"
  - the 0 / 5–60 rule, 0 meaning no auto-dismiss, and the default of 5 when never saved
  - Success only, with Information/Warning/Error never auto-dismissed
  - takes effect on the next request with no restart
  - storage as `ToastSettings.SuccessDismissalDelaySeconds`, no appsettings equivalent, and the "Accessibility settings" deployment step
  - explicit durations in code (`SuccessAsync(message, milliseconds)` etc.) win over the setting
- `src/docs/releases/4.0.0.md`:
  - `## Breaking Changes` → new `### Toasts` (after Liquid Module) with four bullets:
    - Success/Information now `role="status"`/`aria-live="polite"` in TheAdmin, TheTheme and the CoreShapes fallback (TheBlogTheme, TheAgencyTheme). Warning/Error unchanged. Notes that screen readers announce them differently, and suggests custom `Message.cshtml` overrides adopt the mapping.
    - the close button `aria-label` is localized via `T["Close"]`
    - the `Notifier` ctor now takes `IOptions<ToastOptions>` (hint: `Options.Create(new ToastOptions())`)
    - custom `INotifier` implementations get `DismissalMilliseconds == null` from `SuccessAsync(message)` instead of 5000, while explicit durations pass through unchanged
  - `## New features` → new `### Toast Accessibility` (after Page Size Selection) describing the setting, with cross-links to `#toasts` and to the Settings README `#accessibility` anchor.

## Decisions
- The breaking items went under Breaking Changes and the setting under New features, matching how 4.0.0.md already separates them (e.g. Page Size Selection is a feature). The two sections cross-link.
- Terminology follows CONTEXT.md: "toast" (never "notification") and "auto-dismiss".
- The docs follow the plan's design, not the code at the time of writing. Checked against the code: the `Notifier(ILogger<Notifier>, IOptions<ToastOptions>)` ctor and the `…Async(message, int dismissalMilliseconds)` overloads already exist.
- The internal `UseDefaultSuccessDismissal` flag is not named in the release notes because it is internal. The notes only say that the built-in `Notifier` resolves the default.

## Verification
- The repo has no markdownlint config, and mkdocs isn't installed locally, so no mkdocs build was run. `mkdocs.yml` validation is `warn`-level only; new anchors `#toasts`, `#toast-accessibility` and `#accessibility` are unique headings, so they resolve with the default toc slugify.
- Ran `npx markdownlint-cli2` with MD013 (line length) disabled, since the existing docs use long lines throughout, on the HEAD and working-tree copies of both files. Compared the counts per rule:
  - The first run showed one extra MD060 (table column style) from the new table. I aligned the table columns and re-ran.
  - Final result: the rule counts are IDENTICAL to baseline, so the change adds no new lint findings. All remaining findings (MD024, MD034, MD040, MD049, MD031, MD060) predate this change.
- `git diff --stat -- src/docs`: Settings README +18, 4.0.0.md +23. Nothing else in docs was touched.
