using Microsoft.Extensions.Options;
using OrchardCore.DisplayManagement.Notify;

namespace OrchardCore.Settings;

public sealed class ToastOptionsConfiguration : IPostConfigureOptions<ToastOptions>
{
    private readonly ISiteService _siteService;

    public ToastOptionsConfiguration(ISiteService siteService)
    {
        _siteService = siteService;
    }

    public void PostConfigure(string name, ToastOptions options)
    {
        var settings = _siteService.GetSettings<ToastSettings>();

        options.SuccessDismissalDelaySeconds = settings.SuccessDismissalDelaySeconds;
    }
}
