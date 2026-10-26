using AudioSwitcher.Models;
using AudioSwitcher.Services;
using AudioSwitcher.ViewModels;
using AudioSwitcher.Views;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace AudioSwitcher.Tests;

[CollectionDefinition("Desktop layout", DisableParallelization = true)]
public sealed class DesktopLayoutCollection;

[Collection("Desktop layout")]
public sealed class MainWindowLayoutTests
{
    [Fact]
    public void EditorPages_FitNormalAndMinimumWindowSizes()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                AppBuilder.Configure<Application>().UseWin32().UseSkia().SetupWithoutStarting();
                Application.Current!.Styles.Add(new FluentTheme());
                Application.Current.RequestedThemeVariant = ThemeVariant.Light;
                var text = new LocalizationService();
                using var hotkeys = new GlobalHotkeyService(text);
                using var model = new MainWindowViewModel(
                    new ConfigurationStore(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "config.json")),
                    new AudioDeviceService(), new StartupService(), hotkeys, text);
                model.Updates = new AboutWindowViewModel(text, () => Task.CompletedTask, () => { });
                AssertAutomaticDeviceRefresh(model);
                var window = new MainWindow
                {
                    DataContext = model,
                    ShowActivated = false,
                    ShowInTaskbar = false,
                    Position = new PixelPoint(-20000, -20000),
                };
                try
                {
                    window.Show();
                    var installButton = window.FindControl<Button>("InstallUpdateButton")!;
                    Assert.False(installButton.IsVisible);
                    model.Updates.IsUpdateReady = true;
                    Assert.True(installButton.IsVisible);
                    foreach (var size in new[] { new Size(1040, 700), new Size(880, 620) })
                        foreach (var language in model.Languages.Select(item => item.Code))
                            foreach (var page in Enum.GetValues<MainPage>())
                            {
                                model.SelectedLanguage = model.Languages.Single(item => item.Code == language);
                                model.SelectedProfile!.Hotkey = "Ctrl+Alt+1";
                                model.ToggleHotkey = "Ctrl+Alt+2";
                                model.CurrentPage = page;
                                window.Width = size.Width;
                                window.Height = size.Height;
                                window.Measure(size);
                                window.Arrange(new Rect(size));
                                var root = Assert.IsType<Border>(window.Content);
                                root.Measure(size);
                                root.Arrange(new Rect(size));
                                root.UpdateLayout();
                                foreach (var navigation in window.GetVisualDescendants().OfType<Button>()
                                             .Where(button => button.Classes.Contains("nav")))
                                    Assert.Equal(window.FindControl<ListBox>("ProfileList")!.Bounds.Width, navigation.Bounds.Width, 1);
                                var editor = window.FindControl<StackPanel>(page switch
                                {
                                    MainPage.Profile => "ProfileContent",
                                    MainPage.QuickSwitch => "QuickContent",
                                    _ => "SettingsContent",
                                })!;
                                Assert.InRange(editor.Bounds.Width, 500, 680);
                                foreach (var field in editor.GetVisualDescendants().OfType<Control>()
                                             .Where(control => control is TextBox or ComboBox))
                                {
                                    var position = field.TranslatePoint(default, editor)!.Value;
                                    Assert.True(position.X >= 0 && position.X + field.Bounds.Width <= editor.Bounds.Width + 1,
                                        $"{page}/{language}/{size}: {field.GetType().Name} exceeds editor width.");
                                }

                                // Optional local artifacts allow visual inspection without touching the user's configuration.
                                if (Environment.GetEnvironmentVariable("AUDIOSWITCHER_LAYOUT_OUTPUT") is { Length: > 0 } output)
                                {
                                    Directory.CreateDirectory(output);
                                    using var bitmap = new RenderTargetBitmap(new PixelSize((int)size.Width, (int)size.Height));
                                    bitmap.Render(root);
                                    bitmap.Save(Path.Combine(output, $"{page}-{language}-{size.Width}.png"));
                                }
                                var profileList = window.FindControl<ListBox>("ProfileList")!;
                                profileList.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Enter });
                                Assert.Equal(MainPage.Profile, model.CurrentPage);
                                var shortcut = window.FindControl<StackPanel>("ProfileContent")!
                                    .GetVisualDescendants().OfType<Button>().Single(field => field.Classes.Contains("hotkey"));
                                shortcut.RaiseEvent(new KeyEventArgs
                                {
                                    RoutedEvent = InputElement.KeyDownEvent,
                                    Key = Key.F9,
                                    KeyModifiers = KeyModifiers.Control | KeyModifiers.Alt,
                                });
                                Assert.Equal("Ctrl+Alt+F9", model.SelectedProfile!.Hotkey);
                                Assert.Equal(new[] { "Ctrl", "Alt", "F9" }, MainWindow.HotkeyPartsConverter.Convert(
                                    shortcut.Content, typeof(string[]), null, System.Globalization.CultureInfo.InvariantCulture));
                                shortcut.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Delete });
                                Assert.Empty(model.SelectedProfile.Hotkey);
                                var tabHandled = false;
                                EventHandler<KeyEventArgs> onKeyDown = (_, args) => tabHandled = args.Handled;
                                shortcut.AddHandler(InputElement.KeyDownEvent, onKeyDown,
                                    RoutingStrategies.Bubble, handledEventsToo: true);
                                shortcut.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Tab });
                                shortcut.RemoveHandler(InputElement.KeyDownEvent, onKeyDown);
                                Assert.False(tabHandled);
                            }
                    var aboutModel = new AboutWindowViewModel(text, () => Task.CompletedTask, () => { });
                    aboutModel.IsUpdateReady = true;
                    var about = new AboutWindow
                    {
                        DataContext = aboutModel,
                        ShowActivated = false,
                        ShowInTaskbar = false,
                        Position = new PixelPoint(-20000, -20000),
                    };
                    try
                    {
                        about.Show();
                        Assert.True(about.FindControl<Button>("InstallUpdateButton")!.IsVisible);
                        foreach (var language in text.SupportedLanguages)
                            foreach (var width in new[] { 660, 520 })
                            {
                                text.SetLanguage(language.Code);
                                aboutModel.UpdateStatus = text["StandaloneUpdates"];
                                about.Width = width;
                                var size = new Size(width, 660);
                                about.Height = size.Height;
                                about.Measure(size);
                                about.Arrange(new Rect(size));
                                var content = Assert.IsType<Grid>(about.Content);
                                content.Measure(size);
                                content.Arrange(new Rect(size));
                                content.UpdateLayout();
                                Assert.Equal(text["About"], about.Title);
                                Assert.Contains(about.GetVisualDescendants().OfType<TextBlock>(), block => block.Text == "Avalonia");
                                if (Environment.GetEnvironmentVariable("AUDIOSWITCHER_LAYOUT_OUTPUT") is { Length: > 0 } output)
                                {
                                    using var bitmap = new RenderTargetBitmap(new PixelSize(width, 660));
                                    bitmap.Render(content);
                                    bitmap.Save(Path.Combine(output, $"About-{language.Code}-{width}.png"));
                                }
                            }
                    }
                    finally
                    {
                        about.Close();
                    }
                }
                finally
                {
                    window.CloseForExit();
                }
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        })
        { IsBackground = true };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(45)), "Desktop layout check timed out.");
        Assert.Null(failure);
    }

    private static void AssertAutomaticDeviceRefresh(MainWindowViewModel model)
    {
        var outputs = model.OutputDevices.ToArray();
        var inputs = model.InputDevices.ToArray();
        var profile = model.SelectedProfile!;
        var outputId = profile.OutputDeviceId;
        var inputId = profile.InputDeviceId;
        model.OutputDevices.Clear();
        model.InputDevices.Clear();
        model.SelectedOutputDevice = null;
        model.SelectedInputDevice = null;
        profile.SecondaryText = model.Text["DeviceNotAvailable"];
        model.ReportBackgroundError("Keep the existing status");
        model.IsDirty = false;

        var callback = typeof(MainWindowViewModel).GetMethod("QueueDeviceRefresh",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        Task.Run(() =>
        {
            callback.Invoke(model, null);
            callback.Invoke(model, null);
        }).GetAwaiter().GetResult();
        Assert.Empty(model.OutputDevices);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(outputs, model.OutputDevices);
        Assert.Equal(inputs, model.InputDevices);
        Assert.Equal(outputId, profile.OutputDeviceId);
        Assert.Equal(inputId, profile.InputDeviceId);
        Assert.Equal(outputs.FirstOrDefault(device => device.Id == outputId), model.SelectedOutputDevice);
        Assert.Equal(inputs.FirstOrDefault(device => device.Id == inputId), model.SelectedInputDevice);
        if (model.SelectedOutputDevice is not null && model.SelectedInputDevice is not null)
            Assert.NotEqual(model.Text["DeviceNotAvailable"], profile.SecondaryText);
        Assert.False(model.IsDirty);
        Assert.Equal("Keep the existing status", model.StatusMessage);
        Assert.True(model.IsStatusError);

        var missing = new AudioProfile { OutputDeviceId = "missing-output", InputDeviceId = "missing-input" };
        model.Profiles.Add(missing);
        model.SelectedProfile = missing;
        callback.Invoke(model, null);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal("missing-output", missing.OutputDeviceId);
        Assert.Equal("missing-input", missing.InputDeviceId);
        Assert.Equal(model.Text["DeviceNotAvailable"], missing.SecondaryText);
        Assert.Null(model.SelectedOutputDevice);
        Assert.Null(model.SelectedInputDevice);
        model.SelectedProfile = profile;
        model.Profiles.Remove(missing);
    }
}
