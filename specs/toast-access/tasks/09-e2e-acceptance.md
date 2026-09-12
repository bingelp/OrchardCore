# Task 9: End-to-end acceptance pass

**Result:** 17 of 18 ACs pass: 16 verified live, plus AC10 by unit test and view logic. AC3 FAILS because of defect D1, which also means AC11 passes on markup only. UX1 and UX8 pass.

## Environment
- **App:** `src/OrchardCore.Cms.Web/bin/Debug/net10.0/OrchardCore.Cms.Web.dll` (fresh `dotnet build`, branch working tree). Started with `dotnet <dll>` and `ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://localhost:5123`.
- **App data:** `ORCHARD_APP_DATA=/private/tmp/claude-501/-Users-pete-Documents-Repos-OrchardCore/87c38e0b-69e9-47dd-9382-da58e70671ce/scratchpad/oc-e2e-appdata`. `ShellOptionConstants.OrchardAppData` resolves this value. Confirmed before setup:
  - `tenants.json`, `Sites/` and `logs/` were created in the scratch folder.
  - `lsof` showed the process writing `…/oc-e2e-appdata/logs/orchard-log-2026-09-12.log`.
  - `/` served the Setup page, while the user's site is already set up.
- **User's site:** `src/OrchardCore.Cms.Web/App_Data` was not used. Its log file changed during the session, but the writer was a separate `dotnet run` process (PID 84260, started 16:21 and not by this task).
- **Tenants** (all SQLite):
  - `Default`: SaaS recipe (TheAdmin + TheTheme front end). OrchardCore.ContentTypes, Localization and Deployment were enabled later.
  - `blog` (`/blog`): Blog recipe (TheBlogTheme front end).
  - `target` (`/target`): SaaS recipe, with Deployment enabled for the import.
- **Credentials:** `admin` / `<redacted>` on all three tenants. Extra Default user `limited` / `<redacted>`, with role `LimitedSettings` (AccessAdminPanel + ManageAdminSettings, no ManageGeneralSettings/ManageSettings).
- **Method:**
  - Server-side checks used a stdlib Python cookie-jar client (`scratchpad/oc.py`): real login, antiforgery tokens, form posts. Toast markup was read from the raw HTML of the page rendered after the redirect. Front-end toasts were produced by posting without following the redirect and then GETting the tenant home page.
  - Client-side JS behaviour (AC7, D1) was run in Chrome on `http://localhost:5123/`, using that page's Bootstrap 5.3.8 (same `bootstrap.js?v=G26F…` hash as the admin pages). The markup and script captured from the rendered pages were injected verbatim.
  - Passwords were not typed into the browser, so JS checks ran on captured markup rather than inside a logged-in admin page.
  - The Setup form was filled in the browser once.

## Results

| AC | Result | Evidence |
|---|---|---|
| AC1 | PASS | Fresh Default, `/Admin/Settings/general`: tabs in order `Site`, `Accessibility`, `Resources`, `Cache`. Input `value="5"`. Status span text: "Success messages will be dismissed automatically after 5 seconds (paused while hovered or focused)." |
| AC2 | PASS | Fresh `blog` tenant (DB `Properties.ToastSettings` = none). Saving a content item draft gave toast "Your Taxonomy draft has been saved." with `data-bs-autohide="true" data-bs-delay="5000"`. Also on fresh Default (admin settings save): `data-bs-autohide="true" data-bs-delay="5000"`, and `ToastSettings` was still absent from DB. |
| AC3 | **FAIL** (D1) | After saving 0, the next success toast renders `data-bs-autohide="false"` (markup part OK), but also `data-bs-delay=""`. In Chrome, `bootstrap.Toast.getOrCreateInstance` throws `TOAST: Option "delay" provided type "null" but expected type "number".` The toast stays `display:none` and is never shown, so it cannot "stay visible until closed". |
| AC4 | PASS | Saved 30 (DB `{'SuccessDismissalDelaySeconds': 30}`). Next success toast: `data-bs-autohide="true" data-bs-delay="30000"`. Same app PID (82927) throughout, no restart. See O1 for the save's own confirmation toast. |
| AC5 | PASS | Starting from saved 60, each of `-1`, `1`, `4`, `61`, `2.5` and `""` gave the field error "Enter 0, or a whole number from 5 to 60.", no success toast, posted value echoed back, and DB unchanged at 60. `0`, `5` and `60` each saved (DB 0 / 5 / 60) with a success toast and no field error. |
| AC6 | PASS | With 0 saved, raw server HTML (curl, no JS) status span: "Success messages will not be dismissed automatically. They stay until the user closes them." |
| AC7 | PASS (captured markup) | Chrome, rendered editor fragment + inline script. Value 10 gave "…after 10 seconds…", 0 gave "Success messages will not be dismissed automatically. They stay until the user closes them.", 15 gave "…after 15 seconds…", and 4 gave empty. |
| AC8 | PASS | `aria-describedby="ISite_ToastSettings_SuccessDismissalDelaySeconds_Hint ISite_ToastSettings_SuccessDismissalDelaySeconds_Status"`. Both ids resolve. Hint attrs: `id,class`. Status attrs: `id,class,data-toast-delay-status,data-text-never,data-text-after`. `aria-live`/`role` are null on both. |
| AC9 | PASS | As `limited`: GET `/Admin/Settings/general` redirects to `/Error/403`, the page has no `tab-accessibility`/`ToastSettings`, and the admin menu has no general-settings link. POST `/Admin/Settings/general` with `ISite.ToastSettings.SuccessDismissalDelaySeconds=45` + valid antiforgery token redirects to `/Error/403`; DB stayed 30 before and after. Driver-level unauthorized update is covered by `ToastSettingsDisplayDriverTests.Update_WithoutGeneralPermission_DoesNotUpdate`. |
| AC10 | PASS (unit test + view logic) | No UI path passes an explicit duration (`rg 'SuccessAsync\(.*,\s*\d+\)' src` finds nothing). `NotifierTests.SuccessAsync_ExplicitMilliseconds_ConfiguredZero_KeepsExplicitMilliseconds` (setting 0, `SuccessAsync(msg, 3000)`) sets `DismissalMilliseconds == 3000`. `Message.cshtml` maps `ms > 0` to `data-bs-autohide="true" data-bs-delay="3000"`. |
| AC11 | PASS (markup), see D1 | `data-bs-autohide="false"` for Information (blog tenant, setting 5: DeletePart gave `"E2eToast" has been removed.`), Warning (Default, setting 30: Tenants bulk Disable Default) and Error (Default, setting 30: `Tenants/Disable/Default`). All three also emit `data-bs-delay=""`, so in the browser they are never shown (D1). |
| AC12 | PASS (markup) | **TheAdmin:** Success/Information `role="status" aria-live="polite" aria-atomic="true"`, Warning/Error `role="alert" aria-live="assertive" aria-atomic="true"`. **TheTheme** (Default front end `/`, page assets `/TheTheme/`): the same four severities rendered with the same attributes (Success, Warning, Error in one page, Information separately). The two views are identical (`diff`). |
| AC13 | PASS | `blog` front end (TheBlogTheme): `<div class="message message-success" role="status">Site settings updated successfully.</div>` and `<div class="message message-error" role="alert">Can't find the admin menu.</div>` |
| AC14 | PASS | Added `oc-e2e-appdata/Sites/Default/Localization/fr.po` (msgctxt `TheAdmin.Views.Message`/`TheTheme.Views.Message`, "Close" = "Fermer la notification") and set Default supported cultures `["fr","en-US"]`, default `fr`. Admin page `<html lang="fr">`, toast body "Paramètres du site mis à jour avec succès." with close button `aria-label="Fermer la notification"`. TheTheme front end gave the same label. In fr the tab label stayed `Accessibility` (decision 10). Culture reverted to en-US afterwards. |
| AC15 | PASS | Plan "ToastE2E" on Default, step "Accessibility settings" (`ToastSettings_SiteSettingsPropertyDeploymentStep`). Default value set to 45, then ExportFile/Execute returned `ToastE2E.zip` with `Recipe.json` steps `[{"name":"Settings","ToastSettings":{"SuccessDismissalDelaySeconds":45}}]`. On `target` (before: DB none, tab 5), Import/Json of that recipe showed a success toast and set DB to 45. Tab `value="45"` with status "…after 45 seconds…". Next success toast on target: `data-bs-delay="45000"`. |
| AC16 | PASS | `rg -n 5000` on `NotifierExtensions.cs` and both `Message.cshtml` returns no matches (exit 1). The whole `DisplayManagement/Notify/` folder also has no matches. |
| AC17 | PASS | Full suite: `dotnet test test/OrchardCore.Tests/OrchardCore.Tests.csproj` (no filter) reported total 2897, succeeded 2896, failed 0, skipped 1. Feature classes (NotifierTests, ToastSettingsDisplayDriverTests, ToastSettingsTests, ToastSettingsDeploymentTests) ran 31 tests with 0 failed. `CoreShapesMessageTests` is also included in the full run. Coverage gap: no test renders `Message.cshtml`, which is why D1 was missed. |
| AC18 | PASS | `src/docs/releases/4.0.0.md` has "### Toasts" (breaking changes: Success/Information become `status`/polite, localized close label, `Notifier` ctor) and "### Toast Accessibility" (new tab/setting, 0 or 5–60, success only, deployment step). |
| UX1 | PASS | Tab order Site · Accessibility · Resources · Cache (see AC1). |
| UX8 | PASS | General page shows `<p class="alert alert-warning" role="alert">The website might be restarted upon saving the settings…</p>` once for the form. `ToastSettingsDisplayDriver.cs:81` calls `context.AddTenantReloadWarningWrapper()`, like `DefaultSiteSettingsDisplayDriver`. |

## Defects

### D1 (high): non-auto-hiding toasts are never shown (regression from task 2)
- **Where:** `src/OrchardCore.Themes/TheAdmin/Views/Message.cshtml` and `TheTheme/Views/Message.cshtml`, `data-bs-delay="@(autoHide ? milliseconds : null)"`.
- **Cause:** Razor does not drop `data-*` attributes when the value is null. It renders `data-bs-delay=""`, contrary to plan item 4 and the task 2 note.
- **Bootstrap 5.3.8 behavior:** `Manipulator.normalizeData('')` returns `null`, and `_typeCheckConfig` throws `TOAST: Option "delay" provided type "null" but expected type "number".`
- **Effect in `NotifyMessages.cshtml`:** the loop `bootstrap.Toast.getOrCreateInstance(toastElement)` throws on the first such toast. That toast and every later toast in the same container are never shown (`display:none`).
- **Affected toasts:** every Information, Warning and Error toast, and every Success toast when the setting is 0.
- **Chrome evidence, on the page's own bootstrap.js:**
  - A captured TheTheme page with Error, Warning and Success (30000) toasts: `err: TOAST: Option "delay" provided type "null"…`. All three are `display=none show=false`, so even the success toast is hidden.
  - A toast with no `data-bs-delay` attribute: shown (`display=block`).
- **On `main`:** the fallback `5000` meant the attribute was always numeric.
- **Likely fixes** (orchestrator decides):
  - Emit the attribute only inside an `@if`, or build the attribute conditionally.
  - Or keep a numeric placeholder when `autohide=false`. Bootstrap ignores `delay` then, but FR10 forbids a 5000 fallback, so a neutral value or no attribute is better.
  - Add a rendering test.

## Observations (not AC failures)
- **O1 (FR4 nuance):** the confirmation toast raised by the settings save itself uses the pre-save value. `Notifier` resolves `ToastOptions` during the POST, before the shell release. For example, saving 0 when the old value was 60 produced a confirmation toast with `data-bs-delay="60000"`, and saving 30 when the old value was 0 produced one with `autohide="false"`. The toast after that uses the new value. AC3/AC4 say "next success toast", so this passes, but it reads awkwardly against FR4 ("toasts rendered on the next request after saving").
- **O2:** `<script at="Foot">` in `ToastSettings.Edit.cshtml` is emitted literally inline inside the tab pane (the `at` tag helper is not applied here). The same happens for the existing `Settings-Site.Edit.cshtml` script. It works because it waits for `DOMContentLoaded`. Pre-existing pattern.
- **O3:** the "Accessibility settings" deployment step card has `data-category=""` (no category), the same as "Debugging settings". Site Settings is `configuration`. Pre-existing pattern of `SiteSettingsPropertyDeploymentStep`.

## Cleanup
- Stopped the app I started (PID 82927, port 5123); the port is free afterwards.
- Other Cms.Web processes seen during the session were not started by this task and were left alone: the user's `dotnet run` (PID 84180/84260) and another job on port 5189.
- Closed the browser tab I opened.
- The scratch App_Data, the jars and the captured HTML remain in the scratchpad.
