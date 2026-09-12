# Task 2: Severity-appropriate roles, localized close label, no delay fallback

## What changed
- `src/OrchardCore.Themes/TheAdmin/Views/Message.cshtml`
- `src/OrchardCore.Themes/TheTheme/Views/Message.cshtml` (byte-identical copy)

Changes in both files:
- Warning/Error toasts render `role="alert" aria-live="assertive"`. Success/Information render `role="status" aria-live="polite"`. All keep `aria-atomic="true"`.
- The close button now uses `aria-label="@T["Close"]"`.
- `data-bs-delay="@(autoHide ? milliseconds : null)"`: the `5000` fallback is gone. When auto-hide is off, the expression is null, so Razor leaves the attribute out. `data-bs-autohide` is still always emitted (`true`/`false`), which is what AC11 checks.

## Decisions / checks
- `@T` availability: both themes' `_ViewImports.cshtml` use `@inherits OrchardCore.DisplayManagement.Razor.RazorPage<TModel>`, which exposes `IViewLocalizer T` (RazorPage.cs:193). Other views in both themes already use `@T[...]` (e.g. `Pager.cshtml`, `ToggleTheme.cshtml`).
- The role mapping switches on the lower-cased severity string (`warning`/`error` → urgent), which the view already computes.
- The views take the delay only from `Model.Milliseconds`, which `NotifyMessages.cshtml` fills from `entry.DismissalMilliseconds` (resolved by task 1). The views have no fallback.

## Verification
- `dotnet build` TheAdmin.csproj: Build succeeded, 0 warnings, 0 errors. TheTheme.csproj: same. The compiled `TheAdmin.dll` contains the `Views_Message` compiled view type.
- `diff` of the two files: identical.
- `rg 5000` on both files: no matches (exit 1). AC16 met.
- Rendered markup, reasoned from the Razor:
  - Success, auto-hide on: `role="status" aria-live="polite" aria-atomic="true" data-bs-autohide="true" data-bs-delay="<ms>"`.
  - Information/Warning/Error with no explicit duration (null ms): `data-bs-autohide="false"` and no `data-bs-delay`. Information gets status/polite; Warning/Error get alert/assertive (AC11, AC12).
- Live-browser verification (raising one toast of each severity and inspecting the markup, AC2) is deferred to task 9's end-to-end pass.
