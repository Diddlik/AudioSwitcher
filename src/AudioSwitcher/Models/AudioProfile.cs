using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json.Serialization;

namespace AudioSwitcher.Models;

public sealed partial class AudioProfile : ObservableObject
{
    /// <summary><see cref="TriggerExitProfileId"/> value that restores the profile active before the trigger.</summary>
    public static readonly Guid PreviousProfileId = Guid.Empty;

    public Guid Id { get; set; } = Guid.NewGuid();

    [ObservableProperty]
    private string _name = "New profile";

    [ObservableProperty]
    private string _outputDeviceId = string.Empty;

    [ObservableProperty]
    private string _inputDeviceId = string.Empty;

    [ObservableProperty]
    private string _hotkey = string.Empty;

    [ObservableProperty]
    private string _triggerApps = string.Empty;

    /// <summary>Profile to activate after all trigger apps exit: null keeps the current one.</summary>
    [ObservableProperty]
    private Guid? _triggerExitProfileId;

    [ObservableProperty]
    [property: JsonIgnore]
    private bool _isHotkeyUnavailable;

    [ObservableProperty]
    [property: JsonIgnore]
    private string _secondaryText = string.Empty;

    [ObservableProperty]
    [property: JsonIgnore]
    private string _secondaryColor = "#5D5A53";

    [ObservableProperty]
    [property: JsonIgnore]
    private DateTime? _lastActivatedAt;

    [JsonIgnore]
    public IReadOnlyList<string> HotkeyParts => Hotkey.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

    partial void OnHotkeyChanged(string value) => OnPropertyChanged(nameof(HotkeyParts));

    public override string ToString() => Name;
}
