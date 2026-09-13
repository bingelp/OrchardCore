namespace OrchardCore.Settings.ViewModels;

public class ToastSettingsViewModel
{
    /// <summary>
    /// Gets or sets the success dismissal delay, in seconds, as entered by the user.
    /// </summary>
    /// <remarks>
    /// Bound as a string so every invalid input (empty, non-integer, out of range) is reported with a
    /// single localized validation message by the driver instead of the framework's model-binding message.
    /// </remarks>
    public string SuccessDismissalDelaySeconds { get; set; }
}
