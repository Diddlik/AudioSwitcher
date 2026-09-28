using CommunityToolkit.Mvvm.ComponentModel;

namespace AudioSwitcher.Models;

public sealed partial class AudioProfile : ObservableObject
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [ObservableProperty]
    private string _name = "New profile";

    [ObservableProperty]
    private string _outputDeviceId = string.Empty;

    [ObservableProperty]
    private string _inputDeviceId = string.Empty;

    [ObservableProperty]
    private string _hotkey = string.Empty;

    public override string ToString() => Name;
}
