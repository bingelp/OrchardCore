# Toast settings are stored as a site settings section, not typed `ISite` properties

The success dismissal delay is edited on Settings → General, next to settings that are typed `ISite` properties (`PageSize`, `AllowPageSizeSelection` from #19824). We deliberately store it as a section instead (`site.Properties["ToastSettings"]` through `SiteDisplayDriver<ToastSettings>`).

- **Toast timing is a presentation detail, not a core site concept.** Paging is used by many modules. Widening `ISite` for toast timing isn't justified.
- **A section needs no edits to the special-cased code.** Deployment and recipe support come from `AddSiteSettingsPropertyDeploymentStep`, with no changes to `SiteSettingsDeploymentSource` or `SettingsStep`.
- **The driver can move to another module later** without changing the page or migrating data.

## Consequences

- Recipes and deployment exports carry the value under `ToastSettings.SuccessDismissalDelaySeconds`, not as a top-level General setting.
- Moving it to `ISite` later would need a data migration and a recipe-format change.
