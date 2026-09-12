# Task 1: Notifier resolves the success default from `ToastOptions`

## What changed

- **`src/OrchardCore/OrchardCore.DisplayManagement/Notify/ToastOptions.cs`** (new): `ToastOptions` with
  `SuccessDismissalDelaySeconds` defaulting to `5`.
- **`.../Notify/NotifyContext.cs`**: added `internal bool UseDefaultSuccessDismissal { get; set; }`, documented
  as a marker that tells `Notifier` to resolve `DismissalMilliseconds` from `ToastOptions` instead of the
  value set on the context.
- **`.../Notify/NotifierExtensions.cs`**: `SuccessAsync(message)` (the no-duration overload) now passes
  `new NotifyContext { UseDefaultSuccessDismissal = true }` instead of `new NotifyContext { DismissalMilliseconds = 5000 }`.
  The `SuccessAsync(message, dismissalMilliseconds)` overload, and Information/Warning/Error, are unchanged.
- **`.../Notify/Notifier.cs`**: constructor replaced (not overloaded) to take `IOptions<ToastOptions>` in
  addition to `ILogger<Notifier>`, per the plan's explicit direction to avoid an ambiguous-DI overload. In
  `AddAsync`, when `context.UseDefaultSuccessDismissal` is `true`, `DismissalMilliseconds` is resolved as
  `SuccessDismissalDelaySeconds * 1000`, or `null` when `SuccessDismissalDelaySeconds <= 0`. All other paths
  (explicit ms, no context, Information/Warning/Error) are untouched.
- **`test/OrchardCore.Tests/DisplayManagement/Notify/NotifierTests.cs`**: updated to construct `Notifier` with
  a `CreateNotifier(successDismissalDelaySeconds = 5)` helper that builds `Options.Create(new ToastOptions { ... })`.
  Added/kept cases:
  - default (no explicit config, i.e. 5) → `SuccessAsync(message)` sets 5000
  - configured 30 → `SuccessAsync(message)` sets 30000
  - configured 0 → `SuccessAsync(message)` sets `null`
  - configured 0 but explicit `SuccessAsync(message, 3000)` → 3000 (explicit ms always wins)
  - `InformationAsync`/`WarningAsync`/`ErrorAsync` (no duration) → `null` (unaffected by the flag)
  - `AddAsync(NotifyType.Success, message)` with no `NotifyContext` at all → `null` (flag is per-context; no
    context means no flag, same as today)
  - kept the existing explicit-ms `SuccessAsync(message, 3000)` test and the `NotifyEntryConverter` round-trip
    test unchanged.

## Decisions / deviations from the plan

- No deviations. Followed the plan's directive to replace rather than overload the `Notifier` constructor.
  Confirmed via `rg -n "new Notifier\("` that only the test file constructs `Notifier` directly in the repo,
  so there was no compile-breaking fallout to report.
- `ToastOptions`/`IOptions<ToastOptions>` needed no explicit `services.AddOptions()`/`Configure<ToastOptions>()`
  registration for this task: `Notifier` is registered as `services.AddScoped<INotifier, Notifier>()` in
  `OrchardCoreBuilderExtensions.cs`, and the generic `IOptions<T>` infrastructure (already used elsewhere in
  this project, e.g. `ShapeRenderingOptions`) resolves an unconfigured `ToastOptions` to a plain
  `new ToastOptions()`, giving the coded default of 5. Configuration binding is a later task (task 4).

## Verification

```
$ dotnet build src/OrchardCore/OrchardCore.DisplayManagement/OrchardCore.DisplayManagement.csproj -v minimal
Build succeeded.
    0 Warning(s)
    0 Error(s)

$ dotnet test test/OrchardCore.Tests/OrchardCore.Tests.csproj --filter NotifierTests
Test run summary: Passed!
  total: 10
  failed: 0
  succeeded: 10
  skipped: 0
  duration: 1s 037ms

$ rg 5000 src/OrchardCore/OrchardCore.DisplayManagement/Notify
(no output — no matches)

$ rg -n "new Notifier\(" -t cs
test/OrchardCore.Tests/DisplayManagement/Notify/NotifierTests.cs   (only caller, already updated)
```
