namespace OrchardCore.DisplayManagement.Notify;

public sealed class ToastOptions
{
    /// <summary>
    /// The number of seconds a success toast stays visible before auto-dismissing. 0 means never.
    /// </summary>
    public int SuccessDismissalDelaySeconds { get; set; } = 5;
}
