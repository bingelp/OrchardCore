using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace OrchardCore.DisplayManagement.Notify;

public class Notifier : INotifier
{
    private readonly List<NotifyEntry> _entries = [];
    private readonly ILogger _logger;
    private readonly ToastOptions _toastOptions;

    public Notifier(ILogger<Notifier> logger, IOptions<ToastOptions> toastOptions)
    {
        _logger = logger;
        _toastOptions = toastOptions.Value;
    }

    public ValueTask AddAsync(NotifyType type, LocalizedHtmlString message)
        => AddAsync(type, message, context: null);

    public ValueTask AddAsync(NotifyType type, LocalizedHtmlString message, NotifyContext context)
    {
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Notification '{NotificationType}' with message '{NotificationMessage}'", type, message);
        }

        var dismissalMilliseconds = context?.DismissalMilliseconds;

        if (context is { UseDefaultSuccessDismissal: true })
        {
            dismissalMilliseconds = _toastOptions.SuccessDismissalDelaySeconds > 0
                ? _toastOptions.SuccessDismissalDelaySeconds * 1000
                : null;
        }

        _entries.Add(new NotifyEntry
        {
            Type = type,
            Message = message,
            DismissalMilliseconds = dismissalMilliseconds,
        });

        return ValueTask.CompletedTask;
    }

    public IList<NotifyEntry> List()
        => _entries;
}
