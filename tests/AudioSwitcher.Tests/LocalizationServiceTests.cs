using System.ComponentModel;
using AudioSwitcher.Services;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;

namespace AudioSwitcher.Tests;

public sealed class LocalizationServiceTests
{
    [Theory]
    [InlineData("en", "Settings")]
    [InlineData("ru", "Настройки")]
    [InlineData("uk", "Налаштування")]
    [InlineData("fr", "Paramètres")]
    [InlineData("it", "Impostazioni")]
    [InlineData("pl", "Ustawienia")]
    public void SetLanguage_UsesRequestedTranslation(string language, string expected)
    {
        var localization = new LocalizationService();

        localization.SetLanguage(language);

        Assert.Equal(expected, localization["Settings"]);
    }

    [Fact]
    public void SetLanguage_UnsupportedLanguage_FallsBackToEnglish()
    {
        var localization = new LocalizationService();

        localization.SetLanguage("de");

        Assert.Equal("en", localization.CurrentLanguage);
        Assert.Equal("Settings", localization["Settings"]);
    }

    [Fact]
    public void SetLanguage_RaisesIndexerPropertyChanged()
    {
        var localization = new LocalizationService();
        var changedProperties = new List<string?>();
        ((INotifyPropertyChanged)localization).PropertyChanged +=
            (_, eventArgs) => changedProperties.Add(eventArgs.PropertyName);

        localization.SetLanguage("fr");

        Assert.Contains("Item", changedProperties);
    }

    [Fact]
    public void SetLanguage_RefreshesAvaloniaIndexerBinding()
    {
        var localization = new LocalizationService();
        var target = new TextBlock { DataContext = localization };
        target.Bind(TextBlock.TextProperty, new Binding("[Settings]"));
        Assert.Equal("Settings", target.Text);

        localization.SetLanguage("fr");

        Assert.Equal("Paramètres", target.Text);
    }
}
