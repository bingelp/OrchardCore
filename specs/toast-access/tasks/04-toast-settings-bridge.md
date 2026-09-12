# Task 4 — `ToastSettings` section and `ToastOptionsConfiguration` bridge

## What changed
- **New** `src/OrchardCore.Modules/OrchardCore.Settings/ToastSettings.cs` — `public sealed class ToastSettings { int SuccessDismissalDelaySeconds = 5 }`, stored as the `site.Properties["ToastSettings"]` section (ADR 0001). Sealed, matching `DebugSettings`.
- **New** `src/OrchardCore.Modules/OrchardCore.Settings/ToastOptionsConfiguration.cs` — `public sealed class ToastOptionsConfiguration : IPostConfigureOptions<ToastOptions>`; injects `ISiteService`, reads `_siteService.GetSettings<ToastSettings>()` and copies `SuccessDismissalDelaySeconds` into `ToastOptions`.
- `src/OrchardCore.Modules/OrchardCore.Settings/Startup.cs` — added `using OrchardCore.DisplayManagement.Notify;` and `services.AddTransient<IPostConfigureOptions<ToastOptions>, ToastOptionsConfiguration>();` on the line directly after the `PagerOptionsConfiguration` registration (same lifetime/shape).
- **New** `test/OrchardCore.Tests/Modules/OrchardCore.Settings/ToastSettingsTests.cs` — task 5 driver tests should be appended here. Uses a `Mock<ISiteService>` returning a `SiteSettings` (same pattern as `test/OrchardCore.Tests/Settings/ShapeRenderingOptionsConfigurationTests.cs`) and a private `CreateConfiguration(ISite)` helper.

No changes to `ISite`, `SiteSettingsDeploymentSource`, `Recipes/SettingsStep`. No driver, `RequestRelease`, or deployment step (tasks 5/7).

## Decisions / deviations
- **`GetSettings<ToastSettings>()` instead of `site.As<ToastSettings>()`.** `EntityExtensions.As<T>` is marked `[Obsolete("Use TryGet<T> or GetOrCreate<T> instead.")]`. `ISiteService.GetSettings<T>()` is `GetSiteSettings().GetOrCreate<T>()` — identical semantics (missing section → `new T()`, i.e. 5) and is what the sibling `ShapeRenderingOptionsConfiguration` uses. Timing exposure is identical to `PagerOptionsConfiguration` (both go through the synchronous `GetSiteSettings()` backed by `PreloadSiteSettingsTenantEventHandler`).
- The "no section" test pre-sets the options to 42 to prove the post-configure overwrites with the section default (5), rather than just leaving the code default in place.

## Verification
- `dotnet build src/OrchardCore.Modules/OrchardCore.Settings/OrchardCore.Settings.csproj` → Build succeeded, 0 warnings, 0 errors.
- `dotnet test test/OrchardCore.Tests/OrchardCore.Tests.csproj --filter-class "*ToastSettingsTests"` → total 3, succeeded 3, failed 0:
  - `PostConfigure_NoToastSettingsSection_UsesDefaultOfFiveSeconds`
  - `PostConfigure_SavedToastSettings_CopiesSuccessDismissalDelay(seconds: 0)`
  - `PostConfigure_SavedToastSettings_CopiesSuccessDismissalDelay(seconds: 30)`
- Confirmed by running the test executable directly: `test/OrchardCore.Tests/bin/Debug/net10.0/OrchardCore.Tests -class OrchardCore.Tests.Modules.OrchardCore.Settings.ToastSettingsTests` → `Total: 3, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0`.
- Note: this repo uses Microsoft.Testing.Platform (xunit v3). VSTest-style `--filter "FullyQualifiedName~..."` with `--no-build` discovered 0 tests; use `--filter-class` / `--filter-method`.
- Registration check: the new line mirrors `services.AddTransient<IPostConfigureOptions<PagerOptions>, PagerOptionsConfiguration>();` exactly (transient, post-configure, same `Startup`, no feature gate).
- **Deferred to task 9:** manual recipe/DB set of `ToastSettings.SuccessDismissalDelaySeconds` + restart + observe toast delay. Not done here (needs a running site; end-to-end pass is task 9's job). Wiring covered by the unit tests and the registration check above.
