# Task 7 — Deployment and recipe support (FR6, AC15)

## What changed
- `src/OrchardCore.Modules/OrchardCore.Settings/Startup.cs` — in the existing `[RequireFeatures("OrchardCore.Deployment")] DeploymentStartup`, added after the `DebugSettings` step:
  `services.AddSiteSettingsPropertyDeploymentStep<ToastSettings, DeploymentStartup>(S => S["Accessibility settings"], S => S["Exports the accessibility settings."]);`
  (same multi-line shape as the DebugSettings registration beside it).
- New test `test/OrchardCore.Tests/Modules/OrchardCore.Settings/ToastSettingsDeploymentTests.cs` — `Export_ThenImport_RoundTripsSuccessDismissalDelaySeconds`.

## Decisions / findings
- Generic constraint is `where TModel : class, new()` (`src/OrchardCore/OrchardCore.Settings.Core/Deployment/ServiceCollectionExtensions.cs:14`). `ToastSettings` is a sealed class with an implicit public parameterless ctor, so it satisfies it. `sealed` is irrelevant.
- Export: `SiteSettingsPropertyDeploymentSource<TModel>` (`src/OrchardCore/OrchardCore.Settings.Core/Deployment/SiteSettingsPropertyDeploymentSource.cs:17-33`) writes `typeof(TModel).Name` (`"ToastSettings"`) into a `Settings` step.
- Import is generic: `src/OrchardCore.Modules/OrchardCore.Settings/Recipes/SettingsStep.cs:119-120` — the `default:` case of the switch does `site.Properties[property.Key] = property.Value.Clone();`, so any unknown key (including `ToastSettings`) lands in `site.Properties`. No changes to `ISite`, `SiteSettingsDeploymentSource` or `SettingsStep` (DC2 / ADR 0001 respected).
- No existing tests for `SiteSettingsPropertyDeploymentSource`. I modelled the new test on `test/OrchardCore.Tests/Modules/OrchardCore.OpenId/OpenIdServerDeploymentSourceTests.cs` (MemoryFileBuilder + DeploymentPlanResult + recipe step).
- The unit test covers the source and the recipe step with `ToastSettings`. It does not resolve the DI registration itself. The registration is covered by the build and the pattern it shares with ~20 other modules.

## Verification
- `dotnet build src/OrchardCore.Modules/OrchardCore.Settings/OrchardCore.Settings.csproj`: Build succeeded, 0 warnings, 0 errors.
- `dotnet build test/OrchardCore.Tests/OrchardCore.Tests.csproj`: Build succeeded.
- `OrchardCore.Tests -class OrchardCore.Tests.Modules.OrchardCore.Settings.ToastSettingsDeploymentTests`: Total 1, Failed 0. The test exports a site with `SuccessDismissalDelaySeconds = 12`, asserts `steps[0].name == "Settings"` and `steps[0].ToastSettings.SuccessDismissalDelaySeconds == 12` in Recipe.json, runs `SettingsStep` against a fresh `SiteSettings`, and asserts `UpdateSiteSettingsAsync` ran once and `As<ToastSettings>().SuccessDismissalDelaySeconds == 12`.
- `ToastSettingsTests` (task 4) still passes: Total 3, Failed 0.

## Deferred
- Live AC15 check (create a deployment plan in the admin UI, export, import on a second tenant, confirm the Accessibility tab value) was NOT done. Deferred to the task 9 end-to-end pass.
