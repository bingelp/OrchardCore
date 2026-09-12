# Toast Accessibility — Where Should the Configuration Live?

Issue: #19780 (toast accessibility)
Grounding: [toast-catalog.md](toast-catalog.md), gap #3 (*"Auto-dismiss durations are hard-coded, have no global or user setting, and cannot be extended"*).
Scope: Where in the admin panel a site-level toast setting (auto-dismiss on/off and duration) should be edited, and how the value would reach the renderers. This document does not settle exactly which fields the setting has; see [Open questions](#open-questions).

## TL;DR

**Recommendation: Settings → General, on a new tab (working name "Messages") next to Site.** Store the value as a settings section (`site.Properties["ToastSettings"]`) and expose it to code through an options class, following the `PagerOptions` pattern from #19824.

- **Why General:** toasts are a core DisplayManagement primitive that render in **both** TheAdmin and TheTheme. General is the only site-wide settings page owned by the always-on `OrchardCore.Settings` module. The most recent comparable setting (page size selection, #19824) went there too.
- **Why a new tab instead of the Site tab:** the Site tab holds site identity and listing defaults and already has 7 fields. Toast settings are likely to grow (per-severity behavior, and possibly the Media and Data Localization toasts), and a dedicated tab gives them room. **The Site tab is a close second.** It is the right choice if the setting stays at one or two fields.
- **Rejected:** the Notifications module (a different feature), a theme settings page (none exists), and appsettings only (not reachable from the admin panel).
- **Deferred:** a per-user preference. It is the strongest SC 2.2.1 story, but it builds on top of a site default and does not replace one.

---

## 1. Constraints the location must satisfy

| # | Constraint | Source |
|---|---|---|
| C1 | Must cover **admin and front-end** toasts. `TheAdmin/Views/Message.cshtml` and `TheTheme/Views/Message.cshtml` are identical and both have the hard-coded `5000` (line 20). | catalog §1.1 |
| C2 | Must be available whenever toasts are. `INotifier` is registered by DisplayManagement (`OrchardCoreBuilderExtensions.cs:93`), so it is always on and does not depend on an optional feature. | agent research |
| C3 | Must be editable from the admin panel by someone with an existing, sensible permission | request |
| C4 | Should be exportable through deployment plans and settable from recipes, like other site settings | repo convention |
| C5 | Should be discoverable by an admin looking for "how long do success messages stay up" | request |

## 2. How settings pages work today (short version)

- Every page under **Configuration → Settings** is served by the same controller: `OrchardCore.Settings/Controllers/AdminController.cs` at `Admin/Settings/{groupId}`. Each page is a `groupId`, and the content comes from any `IDisplayDriver<ISite>` that contributes to that group.
- **Tabs are just placement strings:** `.Location("Content:1#TabName;position")`. Any driver, from any module, that emits a shape with the same group and tab name is merged into that tab.
- **General** (`groupId = "general"`) is built by `DefaultSiteSettingsDisplayDriver` (`OrchardCore.Settings/Drivers/DefaultSiteSettingsDisplayDriver.cs:51-57`), which defines three tabs:
  - **Site** (10): site name, page title format, base URL, default time zone, page size, page size selection, page size options
  - **Resources** (20): CDN, CDN base URL, resource debug mode, append version
  - **Cache** (30): cache mode
- **Storage options:**
  - **Typed `ISite` properties.** Used by General today and by #19824. These need hand edits in `SiteSettingsDeploymentSource.cs` and `Recipes/SettingsStep.cs`.
  - **A settings section POCO** stored in `site.Properties[typeof(T).Name]` through `SiteDisplayDriver<T>` (`OrchardCore.DisplayManagement/Entities/SiteDisplayDriver.cs`). Deployment support comes from `AddSiteSettingsPropertyDeploymentStep<T, …>()`, as `AdminSettings` does in `OrchardCore.Admin/Startup.cs:98`.
- **No module outside `OrchardCore.Settings` adds to the `general` group today.** Every other module creates its own settings page.

---

## 3. Options

### Option A: Settings → General → **Site** tab (the idea from the request)

Add the fields to the bottom of the Site tab, below the page size fields.

```
Settings › General
┌ Site ┬ Resources ┬ Cache ┐
│ Site name            [ My Site            ]
│ Page title format    [ ...                ]
│ Base url             [ ...                ]
│ Default time zone    [ UTC            ▾ ]
│ Page size            [ 10 ]
│ ☐ Allow page size selection
│ ─────────────────────────────────────────
│ ☑ Automatically dismiss success messages
│ Dismiss after (seconds) [ 5 ]
│   Hint: Info, warning and error messages always stay until closed.
```

| Pros | Cons |
|---|---|
| Meets C1–C3 with no new tab and no new permission (`ManageGeneralSettings`) | The Site tab is about site identity and defaults. "Toast timing" is not what admins expect to find next to "Base url", which hurts C5. |
| Direct precedent: #19824 added an admin-UI behavior setting (page size selection) to this tab | The tab is already the longest on the page and would grow further |
| Smallest UI change | Leaves no room if the setting grows (per-severity toggles, pause behavior, Media and Data Localization toasts) |
| | If stored as typed `ISite` properties like #19824, it widens a core abstraction (`OrchardCore.Infrastructure.Abstractions/Settings/ISite.cs`) for a UI concern |

### Option B: Settings → General → **new tab** ⭐ Recommended

Same page and permission as A, but on its own tab, placed right after Site (for example `Content:1#Messages;15`).

```
Settings › General
┌ Site ┬ Messages ┬ Resources ┬ Cache ┐
│ Status messages (toasts)
│ ☑ Automatically dismiss success messages
│ Dismiss after (seconds) [ 5 ]
│   Hint: Timers pause while the pointer or keyboard focus is on the message.
│   Screen reader users may need longer; consider 10+ seconds or turning
│   auto-dismiss off (WCAG 2.2 SC 2.2.1).
│ ☐ Automatically dismiss information messages
│ (Warning and error messages are never dismissed automatically.)
```

| Pros | Cons |
|---|---|
| Meets C1–C4. Same permission as the rest of General. | A new tab with only a few fields at first |
| Better for C5: the tab name says what it controls, and it can hold an accessibility hint without making the Site tab longer | The tab name needs care. **"Notifications" would clash** with the `OrchardCore.Notifications` bell-icon module. Candidates: "Messages", "Status messages", "Accessibility". |
| Room to grow (per-severity rules, and later the Media and Data Localization timings from catalog §4) without changing other tabs | First time a tab has been added to General since Site, Resources and Cache |
| Works well with section storage (`ToastSettings` in `site.Properties`): no change to `ISite`, and deployment and recipe support come from `AddSiteSettingsPropertyDeploymentStep` | |
| Placement strings mean the driver can live in `OrchardCore.Settings` now and move later without changing the page | |

### Option C: Settings → **Admin** (`AdminSettings`, `OrchardCore.Admin`)

Add fields to `AdminSettings` next to `DisplayThemeToggler`, `DisplayMenuFilter` and `DisplayTitlesInTopbar`.

| Pros | Cons |
|---|---|
| Low effort. `TheAdmin/Views/Layout.cshtml:10` already reads `Site.GetOrCreate<AdminSettings>()`. | **Fails C1.** The Admin page describes the admin shell. Front-end toasts in TheTheme would be controlled by a setting labelled "Admin", or not covered at all. |
| Deployment step already registered | Ties a core DisplayManagement behavior to the `OrchardCore.Admin` module (C2) |
| Close in spirit to the existing "admin UI toggles" | |

Worth choosing only if the scope is intentionally narrowed to admin toasts.

### Option D: A new dedicated settings page (Settings → Messages)

A new `groupId`, its own `AdminMenu` entry, and possibly its own permission. This is the idiomatic pattern for optional modules such as Https, ReCaptcha or Security headers.

| Pros | Cons |
|---|---|
| Clean separation; its own permission is possible | One more item in an already long Settings menu, for a small setting |
| Easy to feature-gate, if that were ever wanted | The owning module would need to be always on (C2), so it ends up in `OrchardCore.Settings` anyway. At that point it is Option B with extra navigation. |
| | A new permission means role migration and documentation work, with no clear benefit |

### Option E: Per-user preference (user profile)

A `SectionDisplayDriver<User, ToastPreferencesPart>` on the user edit screen (precedent: `UserNotificationPreferencesPart` in `OrchardCore.Notifications`). Another approach is a browser-local preference stored in `localStorage` and a cookie, like TheAdmin's `userPreferencesPersistor.ts` (compact sidebar).

| Pros | Cons |
|---|---|
| The strongest answer to SC 2.2.1: each user who needs more time can set it | Not an admin panel configuration point, so it misses C3 on its own |
| The localStorage variant needs no server changes | The server-side part approach needs a user lookup while rendering `Message.cshtml`. The localStorage approach doesn't sync across devices and can't be deployed. |
| | Either one still needs a site default to fall back to |

**Recommended as a later layer on top of B, not instead of it.**

### Option F: appsettings / `IOptions` only

`services.Configure<ToastOptions>(configuration.GetSection("OrchardCore_…"))`, like `AdminOptions` or `NotificationOptions`.

| Pros | Cons |
|---|---|
| Simplest, and good for hosting operators | **Fails C3.** Nothing appears in the admin panel. |
| Can be kept as the code-level default beneath B (see §4) | |

### Option G: `OrchardCore.Notifications` module — rejected

This is the persistent, database-backed bell-icon inbox. It is an optional feature with no settings page (it is configured through `OrchardCore_Notifications` in appsettings). Toast settings here would disappear when the feature is disabled (fails C2), and they would mix up two unrelated systems.

### Theme settings page — not an option

No theme in the repo (TheAdmin, TheTheme, TheAgency, TheBlog, TheComingSoon) has its own settings page. Creating the first one would be a larger design decision than this issue calls for.

---

## 4. Comparison

| | C1 admin + front end | C2 always on | C3 admin panel | C4 deploy/recipe | C5 discoverable | Effort |
|---|---|---|---|---|---|---|
| **A** General → Site tab | ✅ | ✅ | ✅ | ✅ | ⚠️ | Low |
| **B** General → new tab ⭐ | ✅ | ✅ | ✅ | ✅ | ✅ | Low–Med |
| **C** Settings → Admin | ❌ | ⚠️ | ✅ | ✅ | ⚠️ | Low |
| **D** New settings page | ✅ | ⚠️ | ✅ | ✅ | ✅ | Med |
| **E** Per-user preference | ✅ | ✅ | ❌ (alone) | ❌ | ⚠️ | Med–High |
| **F** appsettings only | ✅ | ✅ | ❌ | n/a | ❌ | Low |
| **G** Notifications module | ✅ | ❌ | ❌ | — | ❌ | — |

---

## 5. Recommended shape (Option B)

This section covers only enough of the implementation to show the location works end to end. Details belong in the solution spec.

1. **Options (code default):** add a `ToastOptions` class (or `NotifyOptions`) in `OrchardCore.DisplayManagement/Notify/`, holding today's behavior as its defaults: success auto-dismisses after 5000 ms, other types don't. Optionally bind a shell configuration section so operators can change the default (Option F as a base layer).
2. **Site setting:** add a `ToastSettings` POCO saved through a `SiteDisplayDriver<ToastSettings>` in `OrchardCore.Settings`:
   - `SettingsGroupId => "general"`
   - `.Location("Content:1#Messages;15")`
   - authorized with `SettingsPermissions.ManageGeneralSettings`
   - register the deployment step with `AddSiteSettingsPropertyDeploymentStep<ToastSettings, …>()`
   - no changes to `ISite`, `SiteSettingsDeploymentSource` or `SettingsStep`
3. **Bridge:** add an `IPostConfigureOptions<ToastOptions>` in `OrchardCore.Settings` that copies `ToastSettings` from `ISiteService.GetSiteSettings()`. This mirrors `OrchardCore.Settings/PagerOptionsConfiguration.cs` from #19824.
4. **Consumers:**
   - **Server:** `Notifier` is scoped and can inject `IOptions<ToastOptions>`. Change `SuccessAsync(message)` (`NotifierExtensions.cs:71`) to stop passing a literal `5000` and let `Notifier.AddAsync` apply the configured default when no explicit value is given. Explicit per-call values still win.
   - **Views:** remove the `5000` fallback from both `Message.cshtml` files.
   - **Later:** pass the value to the Media Vue toasts and the Data Localization toast, for example through a `data-` attribute on the layout. This closes the rest of catalog gap #3.
5. **Later (Option E):** a per-user override that can only make toasts *longer* or turn auto-dismiss off, and falls back to the site value.

Why section storage instead of copying #19824's typed `ISite` properties: #19824 added page size to `ISite` because paging is a core site concept used by many modules. Toast timing is a presentation detail. A section keeps `ISite` smaller, gets deployment and recipe support without editing the special-cased switch statements, and makes it easy to move the driver to another module later.

**If the team prefers Option A:** keep everything above and change only the placement string to `Content:1#Site;…`. The storage, options and consumer design are the same, so the tab can move between A and B at any time without data migration.

---

## Open questions

1. **Tab name:** "Messages", "Status messages" or "Accessibility"? Avoid "Notifications".
2. **Scope of the fields:** only the success duration and on/off switch, or per-severity rules (for example, allowing Information to auto-dismiss)? Should Warning and Error be *locked* to never auto-dismiss, in line with SC 2.2.1 and catalog §5.1?
3. **Unit and bounds:** seconds in the UI (easier for admins) stored as milliseconds? What minimum is allowed (for example, ≥ 5 s, or 0 to disable)?
4. **Should appsettings also set the default** (step 1), or is the site setting enough?
5. **Media and Data Localization toasts:** should the same setting drive them in this issue, or in a follow-up?
