using System.Text.Json;
using OrchardCore.DisplayManagement.Notify;

namespace OrchardCore.Tests.DisplayManagement.Notify;

public class NotifierTests
{
    private static Notifier CreateNotifier(int successDismissalDelaySeconds = 5)
        => new(NullLogger<Notifier>.Instance, Options.Create(new ToastOptions
        {
            SuccessDismissalDelaySeconds = successDismissalDelaySeconds,
        }));

    [Fact]
    public async Task SuccessAsync_OutMilliseconds_SetsDefaultMilliseconds()
    {
        var notifier = CreateNotifier();

        await notifier.SuccessAsync(new LocalizedHtmlString("Saved", "Saved"));

        var entry = Assert.Single(notifier.List());
        Assert.Equal(NotifyType.Success, entry.Type);
        Assert.Equal(5000, entry.DismissalMilliseconds);
    }

    [Fact]
    public async Task SuccessAsync_OutMilliseconds_ConfiguredDelay_SetsConfiguredMilliseconds()
    {
        var notifier = CreateNotifier(successDismissalDelaySeconds: 30);

        await notifier.SuccessAsync(new LocalizedHtmlString("Saved", "Saved"));

        var entry = Assert.Single(notifier.List());
        Assert.Equal(NotifyType.Success, entry.Type);
        Assert.Equal(30000, entry.DismissalMilliseconds);
    }

    [Fact]
    public async Task SuccessAsync_OutMilliseconds_ConfiguredZero_SetsNullMilliseconds()
    {
        var notifier = CreateNotifier(successDismissalDelaySeconds: 0);

        await notifier.SuccessAsync(new LocalizedHtmlString("Saved", "Saved"));

        var entry = Assert.Single(notifier.List());
        Assert.Equal(NotifyType.Success, entry.Type);
        Assert.Null(entry.DismissalMilliseconds);
    }

    [Fact]
    public async Task SuccessAsync_ExplicitMilliseconds_ConfiguredZero_KeepsExplicitMilliseconds()
    {
        var notifier = CreateNotifier(successDismissalDelaySeconds: 0);

        await notifier.SuccessAsync(new LocalizedHtmlString("Saved", "Saved"), 3000);

        var entry = Assert.Single(notifier.List());
        Assert.Equal(NotifyType.Success, entry.Type);
        Assert.Equal(3000, entry.DismissalMilliseconds);
    }

    [Fact]
    public async Task ErrorAsync_OutMilliseconds_DoesNotSetMilliseconds()
    {
        var notifier = CreateNotifier();

        await notifier.ErrorAsync(new LocalizedHtmlString("Problem", "Problem"));

        var entry = Assert.Single(notifier.List());
        Assert.Equal(NotifyType.Error, entry.Type);
        Assert.Null(entry.DismissalMilliseconds);
    }

    [Fact]
    public async Task InformationAsync_OutMilliseconds_DoesNotSetMilliseconds()
    {
        var notifier = CreateNotifier();

        await notifier.InformationAsync(new LocalizedHtmlString("Info", "Info"));

        var entry = Assert.Single(notifier.List());
        Assert.Equal(NotifyType.Information, entry.Type);
        Assert.Null(entry.DismissalMilliseconds);
    }

    [Fact]
    public async Task WarningAsync_OutMilliseconds_DoesNotSetMilliseconds()
    {
        var notifier = CreateNotifier();

        await notifier.WarningAsync(new LocalizedHtmlString("Warning", "Warning"));

        var entry = Assert.Single(notifier.List());
        Assert.Equal(NotifyType.Warning, entry.Type);
        Assert.Null(entry.DismissalMilliseconds);
    }

    [Fact]
    public async Task AddAsync_SuccessWithNoContext_DoesNotSetMilliseconds()
    {
        var notifier = CreateNotifier();

        await notifier.AddAsync(NotifyType.Success, new LocalizedHtmlString("Saved", "Saved"));

        var entry = Assert.Single(notifier.List());
        Assert.Equal(NotifyType.Success, entry.Type);
        Assert.Null(entry.DismissalMilliseconds);
    }

    [Fact]
    public async Task SuccessAsync_Milliseconds_SetsMillisecondsOnNotifyEntry()
    {
        var notifier = CreateNotifier();

        await notifier.SuccessAsync(new LocalizedHtmlString("Saved", "Saved"), 3000);

        var entry = Assert.Single(notifier.List());
        Assert.Equal(NotifyType.Success, entry.Type);
        Assert.Equal(3000, entry.DismissalMilliseconds);
    }

    [Fact]
    public void NotifyEntryConverter_Default_RoundTripsMilliseconds()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new NotifyEntryConverter(HtmlEncoder.Default));

        var entry = new NotifyEntry
        {
            Type = NotifyType.Warning,
            Message = new HtmlString("<strong>Heads up</strong>"),
            DismissalMilliseconds = 3000,
        };

        var json = JsonSerializer.Serialize(entry, options);
        var result = JsonSerializer.Deserialize<NotifyEntry>(json, options);

        Assert.Contains(@"""DismissalMilliseconds"":3000", json);
        Assert.NotNull(result);
        Assert.Equal(NotifyType.Warning, result.Type);
        Assert.Equal(3000, result.DismissalMilliseconds);
        Assert.Equal("<strong>Heads up</strong>", result.ToHtmlString(HtmlEncoder.Default));
    }
}
