namespace OrchardCore.Settings;

public sealed class ToastSettings
{
    /// <summary>
    /// Gets or sets the number of seconds a success toast stays visible before it auto-dismisses.
    /// A value of 0 means success toasts never auto-dismiss.
    /// </summary>
    public int SuccessDismissalDelaySeconds { get; set; } = 5;
}
