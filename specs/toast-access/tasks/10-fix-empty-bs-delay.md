# Task 10: Fix toasts that don't auto-dismiss never showing (defect D1). STOPPED, UNVERIFIED

**Status:** the user stopped the build on 2026-09-12 because they ran out of time. The task stays unchecked.

## Background
Task 9 found defect D1: `data-bs-delay="@(autoHide ? milliseconds : null)"` renders `data-bs-delay=""`. Razor doesn't drop `data-*` attributes whose value is null. Bootstrap 5.3.8 then throws `TOAST: Option "delay" provided type "null"…`, so every toast that doesn't auto-dismiss is hidden, along with every toast after it in the container. Evidence is in `tasks/09-e2e-acceptance.md`. The plan's strand 4 text was corrected to match.

## What's in the working tree
The task subagent was stopped partway through, after changing both `src/OrchardCore.Themes/TheAdmin/Views/Message.cshtml` and `src/OrchardCore.Themes/TheTheme/Views/Message.cshtml`. The two files are still identical (`diff` shows no differences).
- The toast root `<div>` is now built with a `TagBuilder` and written out with `@toast.RenderStartTag()` / `@toast.RenderEndTag()`.
- `data-bs-delay` is added only when `autoHide` is true, using the invariant-culture milliseconds.
- The role, `aria-live`, `aria-atomic`, `data-bs-autohide`, CSS classes, `T["Close"]` and body markup are unchanged from task 2.
- There's a short comment explaining why.

## NOT done
- **Build:** I haven't confirmed that the theme projects compile with the change.
- **Rendering test:** none was added, and nobody has looked into whether a theme `.cshtml` can be rendered in a unit test. Task 9 noted the gap: no test renders `Message.cshtml`.
- **Live check:** AC3 and AC11 have not been re-checked in a browser. Every toast should show with the delay at 0 and no console error, and a success toast at 30 should still render `data-bs-delay="30000"`.
- **AC16 and the suite:** `rg 5000` on both views and the full `OrchardCore.Tests` run have not been redone. Task 9's baseline was 2897 total, 2896 passed, 1 skipped.

## To resume
- **Scratch site:** the task 9 site can be reused. Its data is in `ORCHARD_APP_DATA=/private/tmp/claude-501/-Users-pete-Documents-Repos-OrchardCore/87c38e0b-69e9-47dd-9382-da58e70671ce/scratchpad/oc-e2e-appdata` (it will be lost if the scratchpad is cleaned). Log in as `admin` / `<redacted>` and run it on port 5123. Its Default tenant's success dismissal delay may not be 5, so reset it.
- **Steps:** review the diff, build, do the live check, run the suite, then check this task off. After that, `/test` against the spec's ACs.
