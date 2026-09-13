namespace OrchardCore.DisplayManagement.Notify;

public sealed class NotifyContext
{
    /// <summary>
    /// Total milliseconds to auto dismiss the alert. Keep null to not auto dismiss.
    /// </summary>
    public int? DismissalMilliseconds { get; set; }

    /// <summary>
    /// When <see langword="true"/>, <see cref="Notifier"/> resolves <see cref="DismissalMilliseconds"/>
    /// from the configured <see cref="ToastOptions.SuccessDismissalDelaySeconds"/> instead of using the value set here.
    /// </summary>
    internal bool UseDefaultSuccessDismissal { get; set; }
}
