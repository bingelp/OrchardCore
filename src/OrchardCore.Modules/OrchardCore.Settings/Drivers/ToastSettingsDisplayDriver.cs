using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;
using OrchardCore.DisplayManagement.Entities;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.Environment.Shell;
using OrchardCore.Mvc.ModelBinding;
using OrchardCore.Settings.ViewModels;

namespace OrchardCore.Settings.Drivers;

public sealed class ToastSettingsDisplayDriver : SiteDisplayDriver<ToastSettings>
{
    public const string GroupId = DefaultSiteSettingsDisplayDriver.GroupId;

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAuthorizationService _authorizationService;
    private readonly IShellReleaseManager _shellReleaseManager;

    internal readonly IStringLocalizer S;

    protected override string SettingsGroupId
        => GroupId;

    public ToastSettingsDisplayDriver(
        IHttpContextAccessor httpContextAccessor,
        IAuthorizationService authorizationService,
        IShellReleaseManager shellReleaseManager,
        IStringLocalizer<ToastSettingsDisplayDriver> stringLocalizer)
    {
        _httpContextAccessor = httpContextAccessor;
        _authorizationService = authorizationService;
        _shellReleaseManager = shellReleaseManager;
        S = stringLocalizer;
    }

    public override async Task<IDisplayResult> EditAsync(ISite site, ToastSettings settings, BuildEditorContext context)
    {
        if (!await IsAuthorizedAsync())
        {
            return null;
        }

        return BuildEditor(settings.SuccessDismissalDelaySeconds.ToString(CultureInfo.InvariantCulture), context);
    }

    public override async Task<IDisplayResult> UpdateAsync(ISite site, ToastSettings settings, UpdateEditorContext context)
    {
        if (!await IsAuthorizedAsync())
        {
            return null;
        }

        var model = new ToastSettingsViewModel();

        await context.Updater.TryUpdateModelAsync(model, Prefix);

        if (TryParseSuccessDismissalDelay(model.SuccessDismissalDelaySeconds, out var seconds))
        {
            settings.SuccessDismissalDelaySeconds = seconds;

            _shellReleaseManager.RequestRelease();
        }
        else
        {
            context.Updater.ModelState.AddModelError(Prefix, nameof(model.SuccessDismissalDelaySeconds), S["Enter 0, or a whole number from 5 to 60."]);
        }

        // Render the posted-back value so an invalid entry stays visible next to its error.
        return BuildEditor(model.SuccessDismissalDelaySeconds, context);
    }

    internal static bool TryParseSuccessDismissalDelay(string value, out int seconds)
        => int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out seconds)
        && (seconds == 0 || seconds is >= 5 and <= 60);

    private ShapeResult BuildEditor(string successDismissalDelaySeconds, BuildEditorContext context)
    {
        context.AddTenantReloadWarningWrapper();

        // Position 15 places the tab between Site (10) and Resources (20) from DefaultSiteSettingsDisplayDriver.
        return Initialize<ToastSettingsViewModel>("ToastSettings_Edit", model =>
        {
            model.SuccessDismissalDelaySeconds = successDismissalDelaySeconds;
        }).Location("Content:1#Accessibility;15")
        .OnGroup(SettingsGroupId);
    }

    private Task<bool> IsAuthorizedAsync()
        => _authorizationService.AuthorizeAsync(
            _httpContextAccessor.HttpContext?.User,
            SettingsPermissions.ManageGeneralSettings);
}
