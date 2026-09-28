using System.Collections.ObjectModel;
using AudioSwitcher.Models;
using AudioSwitcher.Services;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NAudio.CoreAudioApi;

namespace AudioSwitcher.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, IDisposable
{
    private const string ToggleAction = "toggle";
    private readonly ConfigurationStore _configurationStore;
    private readonly AudioDeviceService _audioDeviceService;
    private readonly StartupService _startupService;
    private readonly GlobalHotkeyService _hotkeyService;

    [ObservableProperty] private AudioProfile? _selectedProfile;
    [ObservableProperty] private AudioDeviceInfo? _selectedOutputDevice;
    [ObservableProperty] private AudioDeviceInfo? _selectedInputDevice;
    [ObservableProperty] private AudioProfile? _toggleProfileA;
    [ObservableProperty] private AudioProfile? _toggleProfileB;
    [ObservableProperty] private string _toggleHotkey = string.Empty;
    [ObservableProperty] private bool _startWithWindows;
    [ObservableProperty] private LanguageOption? _selectedLanguage;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _isStatusError;

    public event Action<string>? ProfileActivated;

    public MainWindowViewModel(ConfigurationStore configurationStore, AudioDeviceService audioDeviceService,
        StartupService startupService, GlobalHotkeyService hotkeyService, LocalizationService localization)
    {
        _configurationStore = configurationStore;
        _audioDeviceService = audioDeviceService;
        _startupService = startupService;
        _hotkeyService = hotkeyService;
        Text = localization;
        _hotkeyService.Triggered += OnHotkeyTriggered;

        RefreshDevices();
        var configuration = _configurationStore.Load(out var warning);
        Text.SetLanguage(configuration.Language);
        SelectedLanguage = Text.SupportedLanguages.First(language => language.Code == Text.CurrentLanguage);
        StatusMessage = Text["Ready"];
        foreach (var profile in configuration.Profiles) Profiles.Add(profile);

        if (Profiles.Count == 0)
        {
            Profiles.Add(new AudioProfile
            {
                Name = Text.Format("DefaultProfile", 1),
                OutputDeviceId = _audioDeviceService.GetDefaultDeviceId(DataFlow.Render) ?? string.Empty,
                InputDeviceId = _audioDeviceService.GetDefaultDeviceId(DataFlow.Capture) ?? string.Empty,
            });
        }

        ToggleHotkey = configuration.ToggleHotkey;
        StartWithWindows = configuration.StartWithWindows;
        ToggleProfileA = Profiles.FirstOrDefault(profile => profile.Id == configuration.ToggleProfileAId);
        ToggleProfileB = Profiles.FirstOrDefault(profile => profile.Id == configuration.ToggleProfileBId);
        SelectedProfile = Profiles[0];

        if (!string.IsNullOrEmpty(warning)) ShowError(warning);
        else if (!TryRegisterHotkeys(out var error)) ShowError(error);
    }

    public ObservableCollection<AudioProfile> Profiles { get; } = [];
    public ObservableCollection<AudioDeviceInfo> OutputDevices { get; } = [];
    public ObservableCollection<AudioDeviceInfo> InputDevices { get; } = [];
    public LocalizationService Text { get; }
    public IReadOnlyList<LanguageOption> Languages => Text.SupportedLanguages;
    public string VersionText => Text.Format("Version",
        typeof(MainWindowViewModel).Assembly.GetName().Version?.ToString(3) ?? Text["Unknown"]);

    [RelayCommand]
    private void AddProfile()
    {
        var profile = new AudioProfile
        {
            Name = GetUniqueProfileName(),
            OutputDeviceId = _audioDeviceService.GetDefaultDeviceId(DataFlow.Render) ?? string.Empty,
            InputDeviceId = _audioDeviceService.GetDefaultDeviceId(DataFlow.Capture) ?? string.Empty,
        };
        Profiles.Add(profile);
        SelectedProfile = profile;
        ShowSuccess(Text["ProfileCreated"]);
    }

    [RelayCommand]
    private void DeleteProfile()
    {
        if (SelectedProfile is null) return;
        var index = Profiles.IndexOf(SelectedProfile);
        Profiles.Remove(SelectedProfile);
        SelectedProfile = Profiles.Count == 0 ? null : Profiles[Math.Min(index, Profiles.Count - 1)];
        if (ToggleProfileA is not null && !Profiles.Contains(ToggleProfileA)) ToggleProfileA = null;
        if (ToggleProfileB is not null && !Profiles.Contains(ToggleProfileB)) ToggleProfileB = null;
        ShowSuccess(Text["ProfileRemoved"]);
    }

    [RelayCommand]
    private void RefreshDevices()
    {
        var outputId = SelectedProfile?.OutputDeviceId;
        var inputId = SelectedProfile?.InputDeviceId;
        Replace(OutputDevices, _audioDeviceService.GetDevices(DataFlow.Render));
        Replace(InputDevices, _audioDeviceService.GetDevices(DataFlow.Capture));
        SelectedOutputDevice = OutputDevices.FirstOrDefault(device => device.Id == outputId);
        SelectedInputDevice = InputDevices.FirstOrDefault(device => device.Id == inputId);
        ShowSuccess(Text.Format("DeviceCounts", OutputDevices.Count, InputDevices.Count));
    }

    [RelayCommand]
    private void ActivateSelectedProfile()
    {
        if (SelectedProfile is not null) Activate(SelectedProfile);
    }

    [RelayCommand]
    private void Save()
    {
        if (!Validate(out var error) || !TryRegisterHotkeys(out error))
        {
            ShowError(error);
            return;
        }

        try
        {
            _startupService.SetEnabled(StartWithWindows);
            _configurationStore.Save(new AppConfiguration
            {
                Profiles = Profiles.ToList(),
                Language = Text.CurrentLanguage,
                ToggleHotkey = ToggleHotkey.Trim(),
                ToggleProfileAId = ToggleProfileA?.Id,
                ToggleProfileBId = ToggleProfileB?.Id,
                StartWithWindows = StartWithWindows,
            });
            ShowSuccess(Text["ConfigurationSaved"]);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            ShowError(Text.Format("SaveFailed", exception.Message));
        }
    }

    partial void OnSelectedLanguageChanged(LanguageOption? value)
    {
        if (value is null) return;
        Text.SetLanguage(value.Code);
        StatusMessage = Text["Ready"];
        OnPropertyChanged(nameof(VersionText));
    }

    partial void OnSelectedProfileChanged(AudioProfile? value)
    {
        SelectedOutputDevice = OutputDevices.FirstOrDefault(device => device.Id == value?.OutputDeviceId);
        SelectedInputDevice = InputDevices.FirstOrDefault(device => device.Id == value?.InputDeviceId);
    }

    partial void OnSelectedOutputDeviceChanged(AudioDeviceInfo? value)
    {
        if (SelectedProfile is not null && value is not null) SelectedProfile.OutputDeviceId = value.Id;
    }

    partial void OnSelectedInputDeviceChanged(AudioDeviceInfo? value)
    {
        if (SelectedProfile is not null && value is not null) SelectedProfile.InputDeviceId = value.Id;
    }

    public void Dispose() => _hotkeyService.Triggered -= OnHotkeyTriggered;

    public void ReportBackgroundStatus(string message) => ShowSuccess(message);

    public void ReportBackgroundError(string message) => ShowError(message);

    private void OnHotkeyTriggered(object? sender, string action)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (action == ToggleAction)
            {
                ToggleProfiles();
                return;
            }

            const string prefix = "profile:";
            if (action.StartsWith(prefix, StringComparison.Ordinal) &&
                Guid.TryParse(action.AsSpan(prefix.Length), out var profileId))
            {
                var profile = Profiles.FirstOrDefault(item => item.Id == profileId);
                if (profile is not null) Activate(profile);
            }
        });
    }

    private void ToggleProfiles()
    {
        if (ToggleProfileA is null || ToggleProfileB is null)
        {
            ShowError(Text["SelectTwoProfiles"]);
            return;
        }

        var currentOutput = _audioDeviceService.GetDefaultDeviceId(DataFlow.Render);
        Activate(currentOutput == ToggleProfileA.OutputDeviceId ? ToggleProfileB : ToggleProfileA);
    }

    private void Activate(AudioProfile profile)
    {
        if (!OutputDevices.Any(device => device.Id == profile.OutputDeviceId) ||
            !InputDevices.Any(device => device.Id == profile.InputDeviceId))
        {
            ShowError(Text.Format("UnavailableDevice", profile.Name));
            return;
        }

        try
        {
            _audioDeviceService.ActivateProfile(profile);
            SelectedProfile = profile;
            ShowSuccess(Text.Format("ProfileActive", profile.Name));
        }
        catch (Exception exception)
        {
            ShowError(Text.Format("ProfileActivationFailed", exception.Message));
            return;
        }

        ProfileActivated?.Invoke(profile.Name);
    }

    private bool Validate(out string error)
    {
        if (Profiles.Count == 0) return Fail(Text["ProfileRequired"], out error);
        if (Profiles.Any(profile => string.IsNullOrWhiteSpace(profile.Name))) return Fail(Text["ProfileNameRequired"], out error);
        if (Profiles.GroupBy(profile => profile.Name.Trim(), StringComparer.CurrentCultureIgnoreCase).Any(group => group.Count() > 1))
            return Fail(Text["UniqueProfileNames"], out error);

        foreach (var profile in Profiles)
        {
            if (string.IsNullOrEmpty(profile.OutputDeviceId) || string.IsNullOrEmpty(profile.InputDeviceId))
                return Fail(Text.Format("ProfileDevicesRequired", profile.Name), out error);
            if (!ValidateOptionalHotkey(profile.Hotkey, out error)) return false;
        }

        if (!ValidateOptionalHotkey(ToggleHotkey, out error)) return false;
        var shortcuts = Profiles.Select(profile => profile.Hotkey).Append(ToggleHotkey)
            .Where(shortcut => !string.IsNullOrWhiteSpace(shortcut)).Select(NormalizeHotkey).ToArray();
        if (shortcuts.Distinct(StringComparer.OrdinalIgnoreCase).Count() != shortcuts.Length)
            return Fail(Text["UniqueShortcuts"], out error);
        if (!string.IsNullOrWhiteSpace(ToggleHotkey) &&
            (ToggleProfileA is null || ToggleProfileB is null || ToggleProfileA == ToggleProfileB))
            return Fail(Text["ToggleProfilesRequired"], out error);

        error = string.Empty;
        return true;
    }

    private bool TryRegisterHotkeys(out string error)
    {
        var shortcuts = Profiles.Where(profile => !string.IsNullOrWhiteSpace(profile.Hotkey))
            .ToDictionary(profile => $"profile:{profile.Id}", profile => profile.Hotkey.Trim());
        if (!string.IsNullOrWhiteSpace(ToggleHotkey)) shortcuts[ToggleAction] = ToggleHotkey.Trim();
        return _hotkeyService.Register(shortcuts, out error);
    }

    private string GetUniqueProfileName()
    {
        for (var number = 1; ; number++)
        {
            var candidate = Text.Format("DefaultProfile", number);
            if (Profiles.All(profile => !string.Equals(profile.Name, candidate, StringComparison.CurrentCultureIgnoreCase))) return candidate;
        }
    }

    private bool ValidateOptionalHotkey(string hotkey, out string error)
    {
        if (!string.IsNullOrWhiteSpace(hotkey)) return HotkeyGesture.TryParse(hotkey, out _, out error, Text);
        error = string.Empty;
        return true;
    }

    private static string NormalizeHotkey(string hotkey) =>
        string.Join('+', hotkey.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)).ToUpperInvariant();

    private static bool Fail(string message, out string error) { error = message; return false; }

    private static void Replace<T>(ObservableCollection<T> collection, IEnumerable<T> values)
    {
        collection.Clear();
        foreach (var value in values) collection.Add(value);
    }

    private void ShowSuccess(string message) { IsStatusError = false; StatusMessage = message; }
    private void ShowError(string message) { IsStatusError = true; StatusMessage = message; }
}
