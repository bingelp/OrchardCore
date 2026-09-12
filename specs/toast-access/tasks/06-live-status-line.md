# Task 6 — Live status line and `aria-describedby`

## What changed
- `src/OrchardCore.Modules/OrchardCore.Settings/Views/ToastSettings.Edit.cshtml`
  - Ids derived from `Html.IdFor(m => m.SuccessDismissalDelaySeconds)` (field prefix is `ISite.ToastSettings`, so ids render as `ISite_ToastSettings_SuccessDismissalDelaySeconds_Hint` / `..._Status`). The Site, Resources and Cache tabs use `asp-for` ids on their own view models plus `pageSizeOptionsWrapper`, so there is no collision.
  - Input gets `aria-describedby="{hintId} {statusId}"` and a `data-toast-delay-input` hook.
  - New second `<span class="hint">` status line (`data-toast-delay-status`), below the static hint. No `aria-live`, no `role`.
  - Server rendering: calls the driver's existing `internal static ToastSettingsDisplayDriver.TryParseSuccessDismissalDelay` (views compile into the same assembly), so the rule is not duplicated. 0 → "never" text, 5–60 → `T["...after {0} seconds...", seconds]`, anything else → empty span (element stays so `aria-describedby` resolves). Because the model holds the saved value on edit and the posted-back string on a failed update (task 5), this covers both cases.
  - `data-text-never` / `data-text-after` carry the localized templates (`T[...].Value`, the unformatted translation, so `{0}` survives), attribute-encoded by Razor. Both status strings go through `@T` (UX7).
  - Page-local `<script at="Foot">` (same pattern as `Settings-Site.Edit.cshtml`): on `input`, `/^\d+$/` then 0 → never, 5–60 → after-template `.replace('{0}', seconds)`, else `''`; sets `textContent`. No other string building, no new CSS or dependencies (UX6).
- `src/OrchardCore.Modules/OrchardCore.Settings/Drivers/ToastSettingsDisplayDriver.cs` — scope addition from the coordinator (plan decision 10 / amended UX7): `.Location($"Content:1#{S["Accessibility"]};15")` → `.Location("Content:1#Accessibility;15")`, matching Site/Resources/Cache. No test asserted the location, so no test changes.

## Decisions / deviations
- **No new helper / no new unit tests.** The status decision reuses the driver's parse helper; the remaining mapping (0 vs 5–60) is two branches in the view. The parse helper's cases (0, 5, 60 accepted; 4, 61, "", "2.5", "abc", etc. rejected) are already covered by the 17 driver tests. `OrchardCore.Settings` has no `InternalsVisibleTo` for the tests, so a separately testable helper would have needed a csproj change — not worth it for this.
- Server/client parity on edge cases: `"05"` → 5 on both (NumberStyles.None accepts leading zeros; parseInt too). Overflowing digit strings → empty on both (TryParse fails; JS number > 60).

## Verification
- `dotnet build test/OrchardCore.Tests/OrchardCore.Tests.csproj` → 0 errors (after both the view change and the Location change). The compiled `OrchardCore.Settings.dll` contains the new `data-toast-delay` markup.
- `OrchardCore.Tests -class OrchardCore.Tests.Modules.OrchardCore.Settings.ToastSettingsDisplayDriverTests` → **Total: 17, Failed: 0** (after the Location change).
- Regression: `-class ...ToastSettingsTests -class ...ToastSettingsDeploymentTests` → Total: 4, Failed: 0.
- Script logic run with node against the exact branch code: `0`→never text; `5`,`10`,`15`,`60`→"after N seconds"; `4`,`61`,`""`,`2.5`,`abc`,`-1`,`+5`,` 5`, 20-digit → `""`; `05`→after 5.
- **Deferred to task 9:** in-app AC1 (status text "after 5 seconds" on a fresh site), AC6 (JS disabled, 0 saved), AC7 (10→0→15 live), AC8 (DOM attributes). Not run: no known admin credentials for the existing App_Data site.
