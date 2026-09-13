# Tasks: Toast Accessibility (#19780)

Plan: `specs/toast-access/plan.md`. Each task leaves the solution building and tests green.

- [x] 1. **Notifier resolves the success default from `ToastOptions`.** `ToastOptions` added; `SuccessAsync(message)` now flags `UseDefaultSuccessDismissal` and `Notifier` (ctor now takes `IOptions<ToastOptions>`) resolves it to seconds*1000 or null — see tasks/01-notifier-default.md
  - Add `ToastOptions` (`SuccessDismissalDelaySeconds = 5`) and `internal bool UseDefaultSuccessDismissal` on `NotifyContext`.
  - `SuccessAsync(message)` sets the flag instead of passing `5000`.
  - `Notifier` takes `IOptions<ToastOptions>` and maps the flag to `seconds * 1000`, or `null` when the value is 0.
  - Update `NotifierTests` for the new ctor, and add tests for:
    - default → 5000
    - configured 30 → 30000
    - configured 0 → null
    - explicit `SuccessAsync(msg, 3000)` with configured 0 → 3000
    - Information/Warning/Error without a duration → null
    - `AddAsync(Success, msg)` with no context → null
  - Verify: `dotnet test --filter NotifierTests` passes. `rg 5000 src/OrchardCore/OrchardCore.DisplayManagement/Notify` finds nothing.

- [x] 2. **Severity-appropriate roles, localized close label and no delay fallback in TheAdmin/TheTheme `Message.cshtml`.** Done: role/aria-live mapped by severity, `T["Close"]` label, `data-bs-delay` omitted when not auto-hiding; both themes build, files identical, no `5000` — see tasks/02-message-view-roles.md
  - Success/Information render `role="status" aria-live="polite"`. Warning/Error render `role="alert" aria-live="assertive"`. All render `aria-atomic="true"`.
  - Close button uses `aria-label="@T["Close"]"`.
  - `data-bs-delay` is emitted only when auto-hide is on.
  - Keep the two files identical.
  - Verify: the solution builds. Run the app and raise one toast of each severity. Inspect the markup (AC2, AC11, AC12). `rg 5000` on both files finds nothing (AC16).

- [x] 3. **CoreShapes fallback `Message` uses the same role mapping.** Done: `role` is status for Success/Information and alert for Warning/Error; 6 unit tests pass — see tasks/03-coreshapes-message-roles.md
  - Set `role` by severity: status for success/information, alert for warning/error. Nothing else changes.
  - Add a unit test rendering the shape for Success and Error.
  - Verify: the test passes. In TheBlogTheme a success message renders `role="status"` (AC13).

- [x] 4. **`ToastSettings` section and `ToastOptionsConfiguration` bridge.** Added `ToastSettings` section POCO, `ToastOptionsConfiguration` post-configure registered next to `PagerOptionsConfiguration`, and 3 unit tests (all pass); manual recipe/restart check deferred to task 9 — see tasks/04-toast-settings-bridge.md
  - Add `ToastSettings` (`SuccessDismissalDelaySeconds = 5`) and an `IPostConfigureOptions<ToastOptions>` that reads `site.As<ToastSettings>()`.
  - Register it in `OrchardCore.Settings/Startup.cs` next to `PagerOptionsConfiguration`.
  - Add unit tests: no section → 5, saved 0 → 0, saved 30 → 30.
  - Verify: tests pass. Manually set the property through a recipe or DB, and after a restart the success toast delay matches it.

- [x] 5. **Accessibility tab: driver, view model and editor, with server validation.** Done: `ToastSettingsDisplayDriver` (general, `#Accessibility;15`, strict parse, single localized error, `RequestRelease` on valid save), view model, `ToastSettings.Edit.cshtml`, registration; 17 driver tests pass; in-app checks deferred to task 9 — see tasks/05-accessibility-tab-editor.md
  - Add `ToastSettingsDisplayDriver : SiteDisplayDriver<ToastSettings>`:
    - group `general`, location `Content:1#Accessibility;15`
    - `ManageGeneralSettings` checked in both edit and update
    - `context.AddTenantReloadWarningWrapper()`
    - strict string parsing with a single localized error
    - `RequestRelease()` on a valid save
  - Add `ToastSettingsViewModel` and `ToastSettings.Edit.cshtml`: label, `type="number" min="0" max="60" step="1"`, validation span, and the static hint (UX3) using `ocat-*`/`hint` conventions.
  - Register the driver.
  - Add tests: `0`/`5`/`60` accepted; `-1`/`1`/`4`/`61`/`2.5`/empty rejected with a field error and the section unchanged; unauthorized user gets a `null` edit result and no update.
  - Verify: tests pass. In the app, the tab order is Site · Accessibility · Resources · Cache and the field shows 5 (AC1, part). Saving 30 makes the next success toast show `data-bs-delay="30000"` with no restart (AC4). Saving 0 gives `autohide="false"` (AC3). Invalid values show an error (AC5). A user without the permission can't see or post (AC9).

- [x] 6. **Live status line and `aria-describedby`.** Done: server-rendered status line (driver's strict parse helper), hint/status ids linked via `aria-describedby` with no live region, page-local script using `data-text-never`/`data-text-after`; tab label now plain `#Accessibility` (decision 10); 17 driver tests pass; in-app AC1/AC6/AC7/AC8 deferred to task 9 — see tasks/06-live-status-line.md
  - Server-render the status line for the saved or posted-back value (empty when it doesn't parse). Give the hint and the status line ids, and set `aria-describedby="{hintId} {statusId}"` on the input. Neither element gets `aria-live` or a role.
  - Add a page-local `<script at="Foot">` that updates the line on `input`, using `data-text-never` / `data-text-after` localized templates.
  - Verify in the app:
    - AC1 status text reads "…after 5 seconds…"
    - AC6 with JS disabled and 0 saved, the "not dismissed automatically" text shows
    - AC7 changing 10→0→15 updates the text live
    - AC8 attributes are correct in the DOM

- [x] 7. **Deployment and recipe support.** Registered `AddSiteSettingsPropertyDeploymentStep<ToastSettings, DeploymentStartup>` in Settings `DeploymentStartup`; export/import round-trip unit test passes; live two-tenant check deferred to task 9 — see tasks/07-deployment-recipe.md
  - Register `AddSiteSettingsPropertyDeploymentStep<ToastSettings, DeploymentStartup>` with localized "Accessibility settings" / "Exports the accessibility settings." strings.
  - Verify: create a deployment plan with the step, export, and see `ToastSettings.SuccessDismissalDelaySeconds` in the JSON. Import it on a second tenant and the Accessibility tab shows the same value (AC15).

- [x] 8. **Docs and release notes.** Added Accessibility subsection to Settings README and Toasts breaking-change + Toast Accessibility feature entries to 4.0.0.md; markdownlint adds no new findings vs baseline — see tasks/08-docs-release-notes.md
  - Add an "Accessibility" subsection to `src/docs/reference/modules/Settings/README.md`: the setting, the 0/5–60 rule, and success only.
  - Add a `src/docs/releases/4.0.0.md` entry covering:
    - the new setting
    - the Success/Information `role="status"`/polite change, which screen reader users will notice
    - the localized close label
    - the `Notifier` constructor now taking `IOptions<ToastOptions>`
    - custom `INotifier` implementations no longer receiving `5000` from `SuccessAsync(message)`
  - Verify: the docs build or lint passes if it's run locally (AC18).

- [x] 9. **End-to-end acceptance pass.** FAILURES: AC3 (D1: `data-bs-delay=""` makes Bootstrap throw, so non-auto-hiding toasts never show); 16/18 ACs pass live, AC10 via unit test, full suite 2897 total / 0 failed — see tasks/09-e2e-acceptance.md
  - Walk AC1–AC18 against a running site: TheAdmin, TheTheme, TheBlogTheme, and a non-English admin culture with a "Close" translation for AC14.
  - Run the full `OrchardCore.Tests` suite.
  - Record any AC that couldn't be verified and why.

- [ ] 10. **Fix: toasts that don't auto-dismiss never show (task 9 defect D1).** IN PROGRESS, stopped (out of time): the fix is applied in both views but NOT verified. There's no rendering test, no live check and no suite run — see tasks/10-fix-empty-bs-delay.md
  - In TheAdmin and TheTheme `Message.cshtml`, leave out `data-bs-delay` entirely when auto-hide is off, instead of rendering `data-bs-delay=""`. Keep the two files identical.
  - Add a rendering test that fails on `data-bs-delay=""`, if the view can be rendered in a test. Otherwise record why.
  - Verify: in a browser, a page with Error, Warning, Information and Success toasts, and with the success delay at 0, shows all of them, with no Bootstrap console error. AC3 and AC11 pass live.


## Verification

Run by `/test` on 2026-09-12 while `/build` task 9 was still open (the user asked to start early). The run used a snapshot of the working tree at that time, in a detached worktree with its own build output, and a separate app on port 5189 with SQLite. The SaaS recipe was used for the Default tenant (TheAdmin/TheTheme), plus a Blog-recipe tenant `blog` (TheBlogTheme). HTTP flows were driven with a scripted client. Chrome was used only on a static copy of the rendered settings page, served locally, with TheAdmin's CSS/JS loaded from the running app.

**Result: 16 pass, 1 fail (AC3), 1 pass with a caveat (AC12). The run found one blocking regression, described below.**

### Blocking finding: non-auto-hiding toasts are never shown in TheAdmin/TheTheme
- `Message.cshtml` renders `data-bs-delay="@(autoHide ? milliseconds : null)"`. Razor emits this as `data-bs-delay=""`; it does not drop the attribute. Bootstrap 5.3 reads that as `null`, and `bootstrap.Toast.getOrCreateInstance` throws `TypeError: TOAST: Option "delay" provided type "null" but expected type "number".`. The init loop in `NotifyMessages.cshtml` stops, so the toast (and any toast after it) is never shown.
- Affected: Success with the setting at 0, and **every Information, Warning and Error toast** raised without an explicit duration. That covers all toast severities except auto-hiding Success.
- Evidence: the real rendered page (setting 0, success toast `data-bs-autohide="false" data-bs-delay=""`) gave the console exception above, and after 10 s the toast had no Bootstrap instance and was not visible. The same page with only ` data-bs-delay=""` removed had no exception, the toast was still shown after 13 s, and clicking close removed it.
- At HEAD the attribute always had a number (`autoHide ? milliseconds : 5000`), so this regression comes from the FR10 change. Suggested fix (for `/build`): omit the attribute entirely when auto-hide is off, for example by writing it conditionally rather than as a null-valued attribute. Then add a check that the rendered markup has no `data-bs-delay=""`.
- Unit tests and the full suite don't catch this, because nothing runs Bootstrap against the rendered markup.

### Acceptance criteria

| ID | Result | Evidence |
|----|--------|----------|
| AC1 | PASS | Never-saved Default tenant, GET `/Admin/Settings/general`: tabs render in the order Site, Accessibility, Resources, Cache. The input has `value="5"`. The status line reads "Success messages will be dismissed automatically after 5 seconds (paused while hovered or focused)." The tenant reload warning is present. |
| AC2 | PASS | Never-saved `blog` tenant: publishing a cloned Article gave a success toast `role="status" … data-bs-autohide="true" data-bs-delay="5000"` in TheAdmin. |
| AC3 | **FAIL** | Markup is correct: after saving 0, the next request's success toast has `data-bs-autohide="false"`. Behavior is not: the toast never appears, because of the blocking finding above, so it can't "stay visible until closed". The intended behavior (still shown after 13 s, removed on close click) was only confirmed on a copy with the empty attribute removed. |
| AC4 | PASS | Saved 30 (302). The success toast raised by the next request (a later General settings save) had `data-bs-autohide="true" data-bs-delay="30000"`, with no restart. Note: the toast raised by the save request itself still uses the previous value (saving 30 showed 5000), because the delay is resolved when the toast is raised, before the shell release. This matches FR4's "next request" wording. |
| AC5 | PASS | Server POSTs (bypassing the browser's min/max/step checks) of `-1`, `1`, `4`, `61`, `2.5` and `""` each returned 200 with the field error "Enter 0, or a whole number from 5 to 60.", and the reloaded value stayed `30`. `0`, `5` and `60` each returned 302 and persisted. Also covered by `ToastSettingsDisplayDriverTests.Update_InvalidValue_*` / `Update_ValidValue_*`. |
| AC6 | PASS | With 0 saved, the server-rendered HTML (fetched without executing JS) has the status line "Success messages will not be dismissed automatically. They stay until the user closes them." |
| AC7 | PASS | In Chrome, typing into the field gave: 10 → "…after 10 seconds…", then 0 → "Success messages will not be dismissed automatically. They stay until the user closes them.", then 15 → "…after 15 seconds (paused while hovered or focused)." No save happened. |
| AC8 | PASS | Input `aria-describedby="ISite_ToastSettings_SuccessDismissalDelaySeconds_Hint ISite_ToastSettings_SuccessDismissalDelaySeconds_Status"`. Checked in the DOM: neither the hint nor the status element has `aria-live` or `role`. Minor observation (not an AC): both are `span.hint` elements, so the status text flows into the same paragraph as the hint rather than sitting on its own line (UX4 says "below the hint"). |
| AC9 | PASS | User `plainuser` (no roles): GET `/Admin/Settings/general` redirected to `/Error/403`, and a POST of `0` was not applied (admin still saw 30). Caveat: this user is blocked at the admin-panel gate. The driver's own permission check is covered by the unit tests `Edit_WithoutGeneralPermission_ReturnsNull` and `Update_WithoutGeneralPermission_DoesNotUpdate` (pass). |
| AC10 | PASS (unit test only) | `NotifierTests.SuccessAsync_ExplicitMilliseconds_ConfiguredZero_KeepsExplicitMilliseconds` passes. No running-site code path calls `SuccessAsync(msg, 3000)`, so this wasn't exercised live. |
| AC11 | PASS (markup) | Live runs with the setting at 30: Error toast (disabling the Default tenant) and Information toast (cloning content on `blog`) both rendered `data-bs-autohide="false"`. Unit tests `Information/Warning/ErrorAsync_OutMilliseconds_DoesNotSetMilliseconds` pass. **But** these toasts hit the blocking finding and are not shown. No live Warning trigger was found; Warning is covered by the unit test and the shared view code. |
| AC12 | PASS (TheTheme by file identity) | TheAdmin live: Success/Information rendered `role="status" aria-live="polite" aria-atomic="true"` and Error rendered `role="alert" aria-live="assertive" aria-atomic="true"`. TheTheme: no front-end action that raises a toast could be found in the time available, so it wasn't rendered live. Its `Message.cshtml` is byte-identical to TheAdmin's (`diff` clean). |
| AC13 | PASS | `blog` tenant front end (TheBlogTheme assets): Success gave `<div class="message message-success" role="status">The Article has been unpublished.</div>`, and Error gave `<div class="message message-error" role="alert">Local login is disabled.</div>`. `CoreShapesMessageTests` pass for all 4 severities. |
| AC14 | PASS | Test-only `Localization/fr.po` (`Close` → `Fermer`) added in the snapshot. With site culture `fr` (`<html lang="fr">`), the Error toast close button rendered `aria-label="Fermer"`; with `en` it rendered `aria-label="Close"`. |
| AC15 | PASS | Default tenant (value 30): deployment plan `ToastPlan` with the "Accessibility settings" step (`ToastSettings_SiteSettingsPropertyDeploymentStep`) exported `{"name":"Settings","ToastSettings":{"SuccessDismissalDelaySeconds":30}}`. Importing that JSON on the `blog` tenant via `/Admin/DeploymentPlan/Import/Json` changed its value from 5 to 30. The unit test `ToastSettingsDeploymentTests.Export_ThenImport_RoundTripsSuccessDismissalDelaySeconds` passes. |
| AC16 | PASS | `rg -n 5000` on `NotifierExtensions.cs` and both `Message.cshtml` files found no matches (exit 1). `rg 5000 src/OrchardCore/OrchardCore.DisplayManagement/Notify` also found none. |
| AC17 | PASS | Toast-related classes (`NotifierTests`, `CoreShapesMessageTests`, `ToastSettings*`): 37 total, 0 failed. They cover the default of 5 s, a configured 30, 0 → null, explicit precedence, non-success severities and the validation bounds. Full `OrchardCore.Tests` suite: **2897 total, 0 failed, 0 errors, 1 skipped**. |
| AC18 | PASS | `src/docs/releases/4.0.0.md` has "Toasts" under Breaking Changes (role/polite change, localized close label, `Notifier` constructor, custom `INotifier`) and "Toast Accessibility" under New features. The Settings README has an Accessibility subsection. |

### Not verified / limits
- Task 9's end-to-end pass is still open in `/build`. This Verification section overlaps it.
- TheTheme toasts were not rendered live (see AC12). No live Warning toast was checked (see AC11).
- Browser checks ran on a static copy of the server-rendered page, not an authenticated browser session.

**Next step:** go back to `/build` to fix the empty `data-bs-delay` attribute, then re-run `/test` for AC3 and AC11 (plus a live check that Information/Warning/Error toasts appear).
