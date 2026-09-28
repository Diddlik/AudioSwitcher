using AudioSwitcher.Services;

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
}
