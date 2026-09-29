using System.Runtime.InteropServices;
using System.Text.Json;
using AudioSwitcher.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AudioSwitcher.ViewModels;

public sealed record PackageCredit(string Name, string Version, string Authors, string License, string Url);

public partial class AboutWindowViewModel : ViewModelBase
{
    public LocalizationService Text { get; }
    public string Version => typeof(AboutWindowViewModel).Assembly.GetName().Version?.ToString(3) ?? "";
    public string Runtime => $"{RuntimeInformation.FrameworkDescription} · {RuntimeInformation.ProcessArchitecture} · {RuntimeInformation.OSDescription}";
    public IReadOnlyList<PackageCredit> Packages { get; }
    public IAsyncRelayCommand CheckForUpdatesCommand { get; }

    [ObservableProperty] private string _updateStatus;

    public AboutWindowViewModel(LocalizationService text, Func<Task> checkForUpdates)
    {
        Text = text;
        _updateStatus = text["UpdatesBody"];
        CheckForUpdatesCommand = new AsyncRelayCommand(checkForUpdates);
        using var credits = typeof(AboutWindowViewModel).Assembly.GetManifestResourceStream("AudioSwitcher.PackageCredits.json")
            ?? throw new InvalidOperationException("Package credits are missing.");
        Packages = JsonSerializer.Deserialize<PackageCredit[]>(credits)
            ?? throw new InvalidOperationException("Package credits are invalid.");
    }
}
