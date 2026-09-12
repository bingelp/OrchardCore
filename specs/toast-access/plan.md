# Plan: Toast Accessibility (#19780)

Spec: `specs/toast-access/spec.md`. Terms follow `CONTEXT.md`. ADR: `docs/adr/0001-toast-settings-as-site-settings-section.md`.

## Approach

The work has three independent strands that meet in the toast views.

1. **A code default in DisplayManagement.** A new `ToastOptions` class (`OrchardCore.DisplayManagement/Notify/`) holds `SuccessDismissalDelaySeconds`, with a default of 5. `Notifier` injects `IOptions<ToastOptions>` and resolves the success default *when the toast is added*. The resolved milliseconds go into `NotifyEntry.DismissalMilliseconds` as they do today, so the `orch_notify` redirect cookie and the views need no new concept.

2. **How `SuccessAsync(message)` asks for the default.** `NotifierExtensions` is static and can't read options. `SuccessAsync(message)` now passes a `NotifyContext` with a new **internal** flag (`UseDefaultSuccessDismissal = true`) instead of the literal `5000`. `Notifier.AddAsync` sees the flag and swaps in `SuccessDismissalDelaySeconds * 1000`, or `null` when the value is 0. Every other path is unchanged:
   - an explicit `…Async(message, ms)` keeps `ms` (FR8)
   - `AddAsync(type, msg)` with no context still means "don't auto-dismiss". Workflows `NotifyTask` is the only such caller in the repo, and its toasts keep today's behavior.
   - Information, Warning and Error never get the flag (FR9)

   *(Decided with the user during `/plan`.)*

3. **The site setting in `OrchardCore.Settings`.** This is a `ToastSettings` section POCO stored in `site.Properties["ToastSettings"]` and edited by a `SiteDisplayDriver<ToastSettings>` on group `general`, placed at `Content:1#Accessibility;15`. A `ToastOptionsConfiguration : IPostConfigureOptions<ToastOptions>` copies the saved value into the options, mirroring `PagerOptionsConfiguration`. Saving calls `IShellReleaseManager.RequestRelease()`, so the next request rebuilds the shell's options singleton (FR4). Deployment and recipe support come from `AddSiteSettingsPropertyDeploymentStep<ToastSettings, DeploymentStartup>` (FR6). The reason for using section storage instead of typed `ISite` properties is in the ADR.

4. **Announcement semantics in the views.** TheAdmin and TheTheme `Message.cshtml` map Success/Information to `status`/`polite` and Warning/Error to `alert`/`assertive`, localize the close label, and drop the `5000` fallback. When auto-hide is off, `data-bs-delay` is not emitted. Razor does **not** drop `data-*` attributes whose value is null: it renders `data-bs-delay=""`, and Bootstrap 5.3 throws on that, so the view has to leave the attribute out explicitly (found in task 9, fixed in task 10). The CoreShapes `Message` shape gets the same role mapping.

5. **Editor UX.** A number input backed by a `string` view-model property. The server parses it strictly (see decisions below), so every invalid input produces one localized error. The hint and a server-rendered status line are both linked through `aria-describedby`. A page-local `<script at="Foot">` updates the status line on `input`, using localized templates passed in `data-` attributes, so no strings are built in JS.

## Files / modules touched

| File | Change | Why |
|---|---|---|
| `src/OrchardCore/OrchardCore.DisplayManagement/Notify/ToastOptions.cs` | **new** | Code default (DC3). It lives next to `Notifier` because DisplayManagement can't depend on Settings. |
| `…/Notify/NotifyContext.cs` | add `internal bool UseDefaultSuccessDismissal` | Marker from strand 2. Internal, so the public surface is unchanged (DC4). |
| `…/Notify/NotifierExtensions.cs` | `SuccessAsync(message)` sets the marker instead of `5000` | FR7, FR10 |
| `…/Notify/Notifier.cs` | ctor takes `IOptions<ToastOptions>`, and `AddAsync` resolves the marker | FR7–FR9 |
| `src/OrchardCore.Themes/TheAdmin/Views/Message.cshtml`, `TheTheme/Views/Message.cshtml` | role mapping, `T["Close"]`, no `5000` | FR10, FR11, FR13 |
| `src/OrchardCore/OrchardCore.DisplayManagement/Shapes/CoreShapes.cs` | `Message` role by severity | FR12 |
| `src/OrchardCore.Modules/OrchardCore.Settings/ToastSettings.cs` | **new** section POCO (`SuccessDismissalDelaySeconds = 5`) | FR1, FR2. Sits beside `DebugSettings.cs`. |
| `…/OrchardCore.Settings/ToastOptionsConfiguration.cs` | **new** `IPostConfigureOptions<ToastOptions>` | DC3. Beside `PagerOptionsConfiguration.cs`. |
| `…/OrchardCore.Settings/Drivers/ToastSettingsDisplayDriver.cs` | **new** `SiteDisplayDriver<ToastSettings>` | FR3, FR5, UX1, UX8 |
| `…/OrchardCore.Settings/ViewModels/ToastSettingsViewModel.cs` | **new** (`string SuccessDismissalDelaySeconds`) | Strict parsing, see decisions |
| `…/OrchardCore.Settings/Views/ToastSettings.Edit.cshtml` | **new** | UX2–UX7 |
| `…/OrchardCore.Settings/Startup.cs` | register driver and post-configure; deployment step in `DeploymentStartup` | FR6, DC2, DC3 |
| `test/OrchardCore.Tests/DisplayManagement/Notify/NotifierTests.cs` | update the ctor and the 5000 test, add cases | DC5, AC17 |
| `test/OrchardCore.Tests/Modules/OrchardCore.Settings/ToastSettingsTests.cs` | **new**: driver validation and auth, post-configure | AC5, AC9, AC17 |
| `test/OrchardCore.Tests/DisplayManagement/…` (CoreShapes test) | **new or extended** | AC13 |
| `src/docs/reference/modules/Settings/README.md` | Accessibility tab section | DC6 |
| `src/docs/releases/4.0.0.md` | new setting, role change, `Notifier` ctor change | DC6, AC18 |

No changes to `ISite`, `SiteSettingsDeploymentSource`, `Recipes/SettingsStep`, `INotifier`, the public `NotifierExtensions` signatures, the `NotifyMessages` views, or any asset pipeline.

## Key decisions & trade-offs

1. **Internal marker on `NotifyContext` instead of "null context = default".** *Chosen by the user.*
   - It keeps "same timing as today" true for `AddAsync(Success, msg)` callers, including Workflows `NotifyTask`.
   - Cost: a third-party `INotifier` implementation will no longer receive `5000` from `SuccessAsync(message)`. It sees a context with `DismissalMilliseconds == null`, and can't read the internal flag.
   - That case is rare (the repo has a single implementation) and goes in the release notes.
   - Rejected: a shared static sentinel `NotifyContext`. It's a mutable public type, so a caller could corrupt it.

2. **Resolve at add time, not render time.** The delay is baked into `NotifyEntry` when the toast is added.
   - A toast carried across a redirect keeps the value that was in effect when it was raised. It can go stale if the setting changes during that one redirect, which is harmless.
   - Views stay dumb, and the cookie format is unchanged.
   - Rejected: resolving in `Message.cshtml`. That would need options injected into two theme views and the CoreShapes path, and it would reintroduce a fallback in the views (FR10).

3. **Section storage (`ToastSettings`) instead of typed `ISite` properties.** This breaks from the #19824 precedent on the same page, so it's recorded as **ADR 0001**.

4. **Replace the `Notifier` constructor instead of adding an overload.**
   - With two public constructors, DI picks the longest satisfiable one, which is fine, but it's still ambiguous and a trap for tests.
   - 4.0.0 is a major release. Direct `new Notifier(logger)` outside tests is unlikely, and the change goes in the release notes.
   - If reviewers push back, keeping the old constructor (defaulting to `Options.Create(new ToastOptions())`) is a one-line change.

5. **Bind the field as `string` and parse it strictly on the server.** The rule is `int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out n) && (n == 0 || n is >= 5 and <= 60)`.
   - Binding to `int?` would make `2.5` or `abc` produce the framework's model-binding message. That message differs from ours, and it's keyed differently in `ModelState`.
   - A single localized message covers every FR3 failure: *"Enter 0, or a whole number from 5 to 60."*
   - `NumberStyles.None` also rejects `-1`, `+5`, whitespace and thousands separators.
   - Nothing is saved on error, because `AdminController` only persists when `ModelState.IsValid` (line 108). The driver still writes the parsed value only when it's valid.

6. **Use the settings page's real CSS conventions, not the classes named in UX6.** UX6 says "match `Settings_Edit__Site` conventions" but lists `mb-3`/`form-label`/`form-text`. The Site tab actually uses `ocat-limited-wrapper`, `ocat-label`, `ocat-limited` and `<span class="hint">`. The plan follows the real conventions, which is UX6's intent. The hint and status line are two `hint` spans, each with an `id` for `aria-describedby`.

7. **Field label wording (UX2 refinement):** *"Success message dismissal delay (seconds)"*, unchanged from the spec.

8. **Status line when the value is invalid (UX4):**
   - The server renders the posted-back value's line only when it parses. Otherwise the line is empty.
   - The script does the same: it sets empty text for an out-of-range or empty value and leaves the element in place, so `aria-describedby` always resolves.
   - Templates come from `data-text-never` and `data-text-after` (with `{0}`), both HTML-attribute-encoded by Razor.

9. **No shell-configuration layer (spec non-goal).** `ToastOptions` is not bound to `IShellConfiguration`, so there is nothing to document for appsettings.

10. **The tab label is the plain placement name `Accessibility`, not localized.** *Decided by the user during `/build`, after task 5.*
   - General settings tab names are the placement `#key`, which `GroupShapes` renders as-is. Site, Resources and Cache aren't localized either.
   - Putting `S["Accessibility"]` into the location string would break tab placement if a translation contains a placement delimiter (`: # @ % | ;`). It would also make the tab's HTML id change with the culture. Nothing else in the repo does this.
   - So the driver uses `Content:1#Accessibility;15`, and UX7 no longer requires a localized tab label. Localizing tab names across the framework can be a separate follow-up.

## Risks / things to verify during build

- `@T` must be available in both themes' `Message.cshtml` through `_ViewImports`. Confirm this before relying on `T["Close"]`.
- Confirm that `RequestRelease()` from a *second* driver on the same POST is harmless. `DefaultSiteSettingsDisplayDriver` already calls it on every General save, so the new driver's call is redundant but explicit.
- The post-configure must not run before the site document exists (setup). `PagerOptionsConfiguration` has the same exposure, so follow it exactly.
