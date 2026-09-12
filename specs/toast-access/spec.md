# Spec: Toast Accessibility — Configurable Success Dismissal Delay and Severity-Appropriate Announcements

Issue: #19780
Research (working tree): `specs/toast-access/toast-catalog.md` (current state, gaps 1–8), `specs/toast-access/toast-settings-location.md` (where the setting lives; Option B chosen).
Terms follow `CONTEXT.md` (Toast, Notification, Severity, Success toast, Auto-dismiss, Success dismissal delay).

## Problem

- **Success toasts disappear too quickly, and nobody can change that.** Success toasts auto-dismiss after a hard-coded 5000 ms (`NotifierExtensions.cs:71`, with a dead fallback in both `Message.cshtml` files). There is no site setting. Screen reader users, low-vision users and slower readers can miss the message. Once it is gone it can't be recovered, which is a WCAG 2.2 SC 2.2.1 (Timing Adjustable) concern.
- **Every toast is announced assertively.** All four severities render `role="alert"` / `aria-live="assertive"`. Success and Information messages interrupt assistive technology when they should be announced politely (ARIA22 vs ARIA19, and the MDN guidance to use assertive sparingly). This affects TheAdmin, TheTheme, and the CoreShapes fallback used by the Liquid themes.
- **The close button's accessible name is not localized.** It is hard-coded `aria-label="Close"`, so non-English admins hear English.
- **Who is affected:** every admin and front-end user who receives toasts, most of all assistive-technology users. Site admins are affected too, because they have no control over the timing.

## Goals

- Site admins can set the success dismissal delay, or turn success auto-dismiss off, from **Settings → General → Accessibility**.
- The setting UI makes it unmistakable that **0 means success toasts are never dismissed automatically**.
- Success and Information toasts are announced politely. Warning and Error toasts are announced assertively.
- The toast close button has a localized accessible name.
- Sites that never touch the setting see the same timing as today.

## Non-goals

- Media module Vue toasts (`NotificationToast.vue`, `UploadToast.vue`, `UploadList.vue`), including their missing live-region semantics and unnamed buttons (catalog gap 4).
- The Data Localization ad-hoc toast: its timing, role, and container creation (part of gaps 1–3).
- A live region that is always present before content arrives (gap 2).
- Toast history or recovering dismissed toasts (gap 6).
- A dismiss control for the CoreShapes fallback toasts (gap 7).
- Auto-dismiss settings for Information, Warning or Error toasts.
- A per-user preference (location doc Option E).
- An appsettings / shell-configuration layer for the default.
- Changing Bootstrap's pause-on-hover/focus behavior, or adding Escape-key dismissal.
- Changes to the `OrchardCore.Notifications` (bell-icon) feature.

## Requirements

### Functional

**Setting**

- **FR1:** The system shall store a site-wide **success dismissal delay** in whole seconds.
- **FR2:** When the setting has never been saved, the system shall use a success dismissal delay of **5** seconds.
- **FR3:** The system shall accept only **0** or a whole number from **5 to 60**. Any other value (negative, 1–4, over 60, non-integer, empty) shall be rejected with a validation error on the field, and nothing shall be saved.
- **FR4:** A saved value shall apply to toasts rendered on the next request after saving, with no application restart.
- **FR5:** Only users with `ManageGeneralSettings` can view or change the setting.
- **FR6:** The setting shall be exportable through a deployment plan and importable through a recipe.

**Notifier behavior**

- **FR7:** When `SuccessAsync(message)` is called without an explicit duration, the success toast shall auto-dismiss after the configured delay. When the configured delay is 0, it shall not auto-dismiss.
- **FR8:** When a caller passes an explicit duration (`SuccessAsync/InformationAsync/WarningAsync/ErrorAsync(message, dismissalMilliseconds)`), that value shall be used unchanged, whatever the site setting is. The setting is a default, not an override.
- **FR9:** Information, Warning and Error toasts raised without an explicit duration shall never auto-dismiss (unchanged).
- **FR10:** No toast view or notifier code path shall keep a hard-coded 5000 ms success delay or delay fallback.

**Announcement semantics**

- **FR11:** In TheAdmin and TheTheme `Message.cshtml`, Success and Information toasts shall render `role="status"`, `aria-live="polite"` and `aria-atomic="true"`. Warning and Error toasts shall render `role="alert"`, `aria-live="assertive"` and `aria-atomic="true"`.
- **FR12:** The CoreShapes fallback message shape (used by themes with no `Message` override, such as TheAgencyTheme and TheBlogTheme) shall use the same severity-to-role mapping as FR11. Its other markup and behavior stay unchanged.
- **FR13:** The toast close button's `aria-label` in TheAdmin and TheTheme `Message.cshtml` shall be localized (`T["Close"]`).

### UX / UI

- **UX1: Location.** A new tab labelled **Accessibility** on Settings → General, placed directly after Site. Tab order: Site · Accessibility · Resources · Cache.
- **UX2: Field.** A number input labelled *"Success message dismissal delay (seconds)"* (final wording may be refined in `/plan`, but it must name success messages and seconds). It has `min="0"`, `max="60"` and `step="1"` as a client-side convenience only. FR3 is enforced on the server.
- **UX3: Static hint** (always visible, `form-text`):
  > Seconds before success messages are dismissed automatically. Set to 0 so success messages are never dismissed automatically and stay until closed. Otherwise use 5–60. Information, warning and error messages are never dismissed automatically.
- **UX4: Live status line** (a second `form-text`, below the hint):
  - value is **0**: *"Success messages will not be dismissed automatically. They stay until the user closes them."*
  - value is **5–60**: *"Success messages will be dismissed automatically after {N} seconds (paused while hovered or focused)."*
  - The server renders the line for the saved (or posted-back) value, so it is correct without JavaScript. A small inline script updates it on `input`.
  - For any other value (out of range or empty) while typing, the line shows no auto-dismiss claim. It is hidden or neutral, and validation handles the error.
- **UX5: Status line accessibility.** Both the hint and the status line are linked to the input through `aria-describedby`. The status line is **not** a live region, so it has no `aria-live` or `role="status"` and is not announced on every keystroke.
- **UX6: Visual style.** Match the existing General settings tabs (`Settings_Edit__Site` conventions): `mb-3` wrapper, `form-label`, narrow `form-control`, `asp-validation-for`, `form-text` hints. Use no alert or colour styling for the 0 state, no new CSS and no new dependencies. Light/dark and RTL are inherited from TheAdmin.
- **UX7: Localization.** The field label, hint, both status-line variants (including the client-side strings) and the validation message are all localizable. The tab label is the plain placement name `Accessibility` and is not localized, consistent with the Site, Resources and Cache tabs (see plan decision 10).
- **UX8: Tenant reload warning.** The Accessibility tab shows the same tenant-reload warning behavior as the rest of General settings.

### Delivery Constraints

- **DC1:** Existing Orchard Core solution and the current .NET / Bootstrap 5.3 stack. No new packages.
- **DC2:** The setting is stored as a site settings section (`site.Properties[...]`) edited through a `SiteDisplayDriver<T>` in `OrchardCore.Settings`, on group `general`. **No changes** to `ISite`, `SiteSettingsDeploymentSource` or `Recipes/SettingsStep`. Deployment and recipe support come from `AddSiteSettingsPropertyDeploymentStep`.
- **DC3:** The notifier reads the effective delay through an options class with a code default of 5 seconds. The site value is applied by an `IPostConfigureOptions<>` in `OrchardCore.Settings`, following `PagerOptionsConfiguration`. Saving triggers `IShellReleaseManager.RequestRelease()`, as General settings already do.
- **DC4:** The public `INotifier` / `NotifierExtensions` signatures don't change, so existing module code keeps compiling.
- **DC5:** Tests extend `test/OrchardCore.Tests/DisplayManagement/Notify/NotifierTests.cs` (and add settings/options tests where needed).
- **DC6:** Release note in `src/docs/releases/4.0.0.md` covering the new setting and the role change for Success/Information toasts (a behavior change screen reader users will notice). Add a brief docs mention of the Accessibility tab.

## Acceptance Criteria

- [ ] **AC1:** On a site where the setting has never been saved, Settings → General shows an **Accessibility** tab between Site and Resources. The field shows **5**, and the status line reads "…dismissed automatically after 5 seconds…".
- [ ] **AC2:** On a site where the setting has never been saved, a success toast (e.g. after saving a content item) renders `data-bs-autohide="true"` and `data-bs-delay="5000"`.
- [ ] **AC3:** After saving **0**, the next success toast renders `data-bs-autohide="false"` and stays visible until its close button is clicked.
- [ ] **AC4:** After saving **30**, the next success toast renders `data-bs-autohide="true"` and `data-bs-delay="30000"`. No application restart is needed.
- [ ] **AC5:** Saving any of `-1`, `1`, `4`, `61`, `2.5` or an empty value shows a validation error on the field and leaves the previously saved value unchanged. Saving `0`, `5` and `60` succeeds.
- [ ] **AC6:** With 0 saved, the Accessibility tab's status line reads "Success messages will not be dismissed automatically. They stay until the user closes them." on page load with JavaScript disabled.
- [ ] **AC7:** With JavaScript enabled, changing the field from 10 to 0 switches the status line to the "will not be dismissed automatically" text without saving. Changing it to 15 switches it to "…after 15 seconds…".
- [ ] **AC8:** The number input's `aria-describedby` references both the static hint and the status line. Neither element has `aria-live` or a `status`/`alert` role.
- [ ] **AC9:** A user without `ManageGeneralSettings` cannot see the Accessibility tab or change the value (a POST is not applied).
- [ ] **AC10:** With the site setting at 0, `SuccessAsync(message, 3000)` produces a toast with `data-bs-autohide="true"` and `data-bs-delay="3000"` (explicit value wins).
- [ ] **AC11:** With any site setting value, Information, Warning and Error toasts raised without an explicit duration render `data-bs-autohide="false"`.
- [ ] **AC12:** In TheAdmin and TheTheme, Success and Information toasts render `role="status" aria-live="polite" aria-atomic="true"`. Warning and Error toasts render `role="alert" aria-live="assertive" aria-atomic="true"`.
- [ ] **AC13:** In TheBlogTheme (CoreShapes fallback), a Success message renders `role="status"` and an Error message renders `role="alert"`.
- [ ] **AC14:** With the admin culture set to a language that has a translation for "Close", the toast close button's `aria-label` is the translated string.
- [ ] **AC15:** A deployment plan with the site settings property step for this setting exports the saved value. Importing that recipe on another tenant sets the same value there.
- [ ] **AC16:** Searching `src/` for the literal success-delay `5000` in `NotifierExtensions.cs` and both `Message.cshtml` files returns no matches.
- [ ] **AC17:** Unit tests cover: the default delay (5 s), a configured delay, 0 → no auto-dismiss, explicit per-call precedence, non-success severities not auto-dismissing, and the validation bounds. All tests pass.
- [ ] **AC18:** `src/docs/releases/4.0.0.md` documents the new Accessibility setting and the Success/Information role change.

## Assumptions

- Scope is the core notifier (TheAdmin, TheTheme, CoreShapes fallback) only. Media and Data Localization toasts are follow-ups.
- Existing sites need no migration. An unsaved setting resolves to the 5-second code default.
- Bootstrap's built-in hover/focus pause (restarting from the full delay) is acceptable as-is. The status-line text "paused while hovered or focused" describes it.
- The inline status-line script is page-local to the settings editor. It needs no asset pipeline changes.
- Localization of new strings uses the standard `IStringLocalizer`/`IViewLocalizer` flow. Translations themselves are not part of this work.
- This spec lives in `.git/specs/toast-access/` for the pipeline. The user will commit the workflow-generated files deliberately for this exercise.

## Open Questions

- None blocking. `/plan` may refine the exact field label wording (UX2) and the name of the settings section/options class.
