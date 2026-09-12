# Task 3: CoreShapes fallback `Message` uses the same role mapping

## What changed
- `src/OrchardCore/OrchardCore.DisplayManagement/Shapes/CoreShapes.cs`, `Message` shape: `role` is now
  `status` for Success/Information and `alert` for Warning/Error (previously `alert` for everything).
  The mapping compares the lower-cased `Type` string, so it works whether `Type` is a `NotifyType` or a string.
  Any unrecognized type still falls back to `alert`, which was the old behavior.
- New `test/OrchardCore.Tests/DisplayManagement/CoreShapesMessageTests.cs`:
  - `MessageShouldRenderRoleBySeverity`: all four severities map to the expected role.
  - `MessageShouldKeepExistingMarkup` (Success, Error): still a `<div>` with the `message` and `message-{severity}` classes,
    inner content unchanged, and no `aria-live`.

## Decisions and deviations
- The fallback shape never had `aria-live` or `aria-atomic`. The task says "nothing else changes" and FR12 only requires
  the role mapping, so I did NOT add them. `role="status"` already implies polite and `role="alert"` implies assertive.
  The test asserts `aria-live` is still absent, so adding it later would be a deliberate choice.
- The test calls `CoreShapes.Message(shape)` directly on a `Shape`, the way `NotifyMessages` builds it via `Arguments.From(NotifyEntry)`,
  instead of spinning up the full display pipeline.

## Verification
- Red first: before the change, Success and Information failed (`<div class="message message-success" role="alert">`).
- Green after: `OrchardCore.Tests -class OrchardCore.Tests.DisplayManagement.CoreShapesMessageTests` gave
  Total: 6, Errors: 0, Failed: 0, Skipped: 0.
- Functional tests that use `.message-success` still match, because class names are unchanged.
- Live TheBlogTheme check (AC13) is deferred to task 9's end-to-end pass. The unit test covers the rendered markup.

## Notes for others
- In this repo (xunit v3 on Microsoft.Testing.Platform), `dotnet test --filter ...` runs zero tests. Use
  `dotnet test --project <csproj> ...` or run `bin/Debug/net10.0/OrchardCore.Tests -class <FQN>` directly.
- With concurrent builds, MSBuild can skip `CoreCompile` for a newly added file if another agent's build wrote a newer DLL.
  `touch` the file and rebuild.
