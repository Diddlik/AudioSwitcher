<p align="center">
  <img src="src/AudioSwitcher/Assets/audioswitcher.png" width="112" alt="AudioSwitcher-Logo">
</p>

# AudioSwitcher

Schlankes Windows-Tool zum Umschalten von Standard-Audiogeräten über globale Shortcuts.

## Funktionen

- Profile mit je einem Ein- und Ausgabegerät
- Globaler Shortcut pro Profil
- Globaler Shortcut zum Wechsel zwischen zwei Profilen
- Setzt die Windows-Rollen Konsole, Multimedia und Kommunikation
- System-Tray; Schließen blendet das Fenster aus
- Optionaler Autostart für den aktuellen Windows-Benutzer
- Automatische Update-Prüfung über GitHub Releases; Downloads werden beim nächsten Beenden installiert
- Lokale Konfiguration unter `%LocalAppData%\AudioSwitcher\config.json`

## Installation

Lade `Diddlik.AudioSwitcher-Setup.exe` aus dem neuesten [GitHub Release](https://github.com/Diddlik/AudioSwitcher/releases/latest) herunter. Der Velopack-Installer installiert die Anwendung für Windows x64 und übernimmt spätere Updates automatisch.

## Entwicklung

Voraussetzungen: Windows 10/11 und .NET SDK 10.

```powershell
dotnet build
dotnet test
dotnet run --project src/AudioSwitcher
```

Einen lokalen, selbstständigen Velopack-Installer erzeugen:

```powershell
.\scripts\package.ps1 -Version 1.0.0
```

Die Ausgaben liegen anschließend unter `artifacts\releases`.

Shortcuts werden direkt erfasst: Shortcut-Feld anklicken und die gewünschte Tastenkombination drücken. `Entf` oder `Backspace` löscht die Belegung.

## Bedienung

1. Profil anlegen und benennen.
2. Ausgabe- und Eingabegerät wählen.
3. Optional das Shortcut-Feld anklicken und die gewünschte Kombination drücken.
4. Für den Schnellwechsel zwei Profile wählen und den Wechsel-Shortcut ebenfalls per Tastendruck erfassen.
5. Speichern. Bereits systemweit belegte Shortcuts werden abgelehnt.

`Jetzt aktivieren` schaltet das gewählte Profil sofort. Systemeinstellungen sind über das Menü `Einstellungen` erreichbar. Das Tray-Menü öffnet oder beendet die App.

## Veröffentlichung

Ein Tag im Format `vMAJOR.MINOR.PATCH`, beispielsweise `v1.0.0`, startet `.github/workflows/release.yml`. Der Workflow testet die Lösung, veröffentlicht eine selbstständige Windows-x64-App, erzeugt Velopack-Pakete und lädt Installer sowie Update-Feed in ein GitHub Release.

Der Installer ist derzeit nicht code-signiert. Windows kann deshalb beim ersten Start eine SmartScreen-Warnung anzeigen.
