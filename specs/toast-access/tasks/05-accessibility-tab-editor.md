# Task 5 — Accessibility tab: driver, view model and editor, with server validation

## What changed
- **New** `src/OrchardCore.Modules/OrchardCore.Settings/Drivers/ToastSettingsDisplayDriver.cs` — `public sealed class ToastSettingsDisplayDriver : SiteDisplayDriver<ToastSettings>`.
  - `GroupId = DefaultSiteSettingsDisplayDriver.GroupId` (`general`); location `Content:1#{S["Accessibility"]};15`, shape `ToastSettings_Edit`.
  - `ManageGeneralSettings` checked in both `EditAsync` and `UpdateAsync` (returns `null` when not authorized; nothing bound, nothing written).
  - `context.AddTenantReloadWarningWrapper()` whenever the editor is built (edit and update), like `DefaultSiteSettingsDisplayDriver`/`HttpsSettingsDisplayDriver`.
  - Strict parse `internal static TryParseSuccessDismissalDelay`: `int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out n) && (n == 0 || n is >= 5 and <= 60)`. On failure: ONE `ModelState` error on `SuccessDismissalDelaySeconds`, `S["Enter 0, or a whole number from 5 to 60."]`; the section is not touched. On success: sets `settings.SuccessDismissalDelaySeconds` and calls `IShellReleaseManager.RequestRelease()`.
  - The update result renders the *posted-back* string, so an invalid entry stays visible next to its error (supports decision 8 for task 6).
- **New** `…/OrchardCore.Settings/ViewModels/ToastSettingsViewModel.cs` — `string SuccessDismissalDelaySeconds`.
- **New** `…/OrchardCore.Settings/Views/ToastSettings.Edit.cshtml` — `ocat-limited-wrapper` + `asp-validation-class-for`, `ocat-label` label "Success message dismissal delay (seconds)", `ocat-limited` with `<input asp-for type="number" min="0" max="60" step="1" class="form-control">`, `asp-validation-for` span, and the UX3 hint verbatim in `<span class="hint" id="@Html.IdFor(m => m.SuccessDismissalDelaySeconds)_Hint">`. All strings via `@T`. No status line / `aria-describedby` / script (task 6).
- `…/OrchardCore.Settings/Startup.cs` — one line: `services.AddSiteDisplayDriver<ToastSettingsDisplayDriver>();` directly after `DefaultSiteSettingsDisplayDriver`. No new permission registration needed: `general` is already mapped to `ManageGeneralSettings`. (Task 7's deployment-step lines untouched.)
- **New** `test/OrchardCore.Tests/Modules/OrchardCore.Settings/ToastSettingsDisplayDriverTests.cs` (sibling class, to avoid editing task 4's file concurrently).

## Decisions / deviations
- **Tab position 15 kept.** `DefaultSiteSettingsDisplayDriver` uses Site;10, Resources;20, Cache;30, and no other driver in the repo places tabs on `general`, so 15 gives Site · Accessibility · Resources · Cache. No deviation.
- **Tab label localization (UX7).** The tab name is the placement `#key`, rendered raw by `GroupShapes` (Site/Resources/Cache are *not* localized). To meet UX7 the driver interpolates `S["Accessibility"]` into the location string. Caveat: a translation containing a placement delimiter (`: # @ % |` or `;`) would break parsing; the tab's HTML id (`tab-<key>-…`) also varies by culture. No repo precedent for this; flag for review. Alternative is the literal `Accessibility` (consistent with siblings, not localizable).
- **`RequestRelease()` twice on one POST is harmless.** `DefaultShellReleaseManager.RequestRelease()` sets `_release = true` and only adds the deferred task once (`_deferredTaskAdded` guard); the deferred task releases the shell once. Our call is only made on a valid value.
- **Non-obsolete APIs.** Driver works on the `ToastSettings` section passed in by `SectionDisplayDriverBase` (which writes `site.Properties["ToastSettings"]` only when `ModelState.IsValid`); tests use `GetOrCreate<T>()` rather than obsolete `As<T>()`. ADR 0001 respected (no `ISite` change).
- Extra rejected inputs tested beyond AC5: `null`, `" 5"`, `"+5"`, `"abc"`.

## Verification
- `dotnet build test/OrchardCore.Tests/OrchardCore.Tests.csproj` → Build succeeded (0 errors). Compiled view present in `OrchardCore.Settings.dll` (`AspNetCoreGeneratedDocument.Views_ToastSettings_Edit`, `/Views/ToastSettings.Edit.cshtml`).
- `test/OrchardCore.Tests/bin/Debug/net10.0/OrchardCore.Tests -class OrchardCore.Tests.Modules.OrchardCore.Settings.ToastSettingsDisplayDriverTests` → **Total: 17, Errors: 0, Failed: 0, Skipped: 0**:
  - `Update_ValidValue_SavesSectionAndRequestsRelease` ×3 (`0`, `5`, `60`): ModelState valid, section saved, `RequestRelease` once.
  - `Update_InvalidValue_AddsSingleFieldErrorAndLeavesSectionUnchanged` ×10 (`-1`, `1`, `4`, `61`, `2.5`, `""`, `null`, `" 5"`, `"+5"`, `"abc"`): exactly one ModelState entry keyed `SuccessDismissalDelaySeconds` with exactly one error = the localized message; `site.Properties["ToastSettings"]` JSON identical (20); `RequestRelease` never.
  - `Edit_WithGeneralPermission_ReturnsEditorWithReloadWarning`, `Edit_OtherGroup_ReturnsNull`, `Edit_WithoutGeneralPermission_ReturnsNull`, `Update_WithoutGeneralPermission_DoesNotUpdate` (null result, value stays 20, no release).
- Regression: `-class …ToastSettingsTests -class …SettingsAuthorizationTests` → Total: 13, Failed: 0.
- **Deferred to task 9 (not done here):** in-app checks — tab order Site · Accessibility · Resources · Cache with field showing 5 (AC1 part), save 30 → `data-bs-delay="30000"` without restart (AC4), save 0 → `data-bs-autohide="false"` (AC3), invalid values show the error (AC5 UI), user without `ManageGeneralSettings` can't see/post (AC9 UI). Not run because admin credentials for the existing `App_Data` site are unknown and running the app would lock `bin/obj` while task 7 builds concurrently.

## Superseded decision

- The user chose the plain `Content:1#Accessibility;15` over the localized `S["Accessibility"]` interpolation described above. This is recorded as plan.md decision 10, and UX7 is amended to match. The code change was made while task 6 was running (see tasks/06-live-status-line.md).
