using OrchardCore.DisplayManagement.Notify;
using OrchardCore.Entities;
using OrchardCore.Settings;

namespace OrchardCore.Tests.Modules.OrchardCore.Settings;

public class ToastSettingsTests
{
    [Fact]
    public void PostConfigure_NoToastSettingsSection_UsesDefaultOfFiveSeconds()
    {
        var options = new ToastOptions
        {
            SuccessDismissalDelaySeconds = 42,
        };

        CreateConfiguration(new SiteSettings()).PostConfigure(string.Empty, options);

        Assert.Equal(5, options.SuccessDismissalDelaySeconds);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(30)]
    public void PostConfigure_SavedToastSettings_CopiesSuccessDismissalDelay(int seconds)
    {
        var site = new SiteSettings();
        site.Put(new ToastSettings
        {
            SuccessDismissalDelaySeconds = seconds,
        });

        var options = new ToastOptions();

        CreateConfiguration(site).PostConfigure(string.Empty, options);

        Assert.Equal(seconds, options.SuccessDismissalDelaySeconds);
    }

    private static ToastOptionsConfiguration CreateConfiguration(ISite site)
    {
        var siteService = new Mock<ISiteService>();
        siteService.Setup(x => x.GetSiteSettingsAsync())
            .ReturnsAsync(site);

        return new ToastOptionsConfiguration(siteService.Object);
    }
}
