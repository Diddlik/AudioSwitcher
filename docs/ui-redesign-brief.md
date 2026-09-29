# AudioSwitcher UI redesign brief

## Purpose of this document

This brief gives a UI designer enough product context to redesign AudioSwitcher without changing its behavior. The intended reader is a designer or a design-focused coding agent who has not seen the application before.

After reading this brief, the designer should be able to create a complete desktop UI concept, including the main window, settings, interaction states, validation feedback, tray behavior, and profile activation feedback.

## Design assignment

Redesign the AudioSwitcher interface from the ground up while preserving the behavior in this brief. Improve hierarchy, clarity, speed, accessibility, and visual consistency. Do not add product features unless they are clearly marked as optional ideas outside the proposed core design.

Treat this document as the functional source of truth. The existing screenshots are reference material for the current information architecture, not a visual style that must be retained.

## Product summary

AudioSwitcher is a Windows desktop utility for people who regularly switch between audio setups. A setup might combine desktop speakers with a desk microphone, or a headset output with its built-in microphone.

The application stores each setup as a profile. A user can activate a profile from the main window, with a global shortcut, or by toggling between two selected profiles. Activating a profile changes both the default Windows output device and the default Windows input device.

The application runs in the system tray and can start with Windows. It is designed to stay available in the background instead of behaving like a document-based desktop application.

## Primary user goals

The redesign should make these tasks obvious and fast:

1. See the available audio profiles and identify the selected profile.
2. Create or edit a profile with one output device and one input device.
3. Activate a profile immediately.
4. Assign a global shortcut to a profile.
5. Configure a shortcut that switches between two profiles.
6. Change the interface language and Windows startup behavior.
7. Understand whether an action succeeded or why it failed.

## Core concepts

### Audio profile

Each profile contains:

- A unique user-defined name.
- One Windows audio output device.
- One Windows audio input device.
- An optional global shortcut.

Profiles are user data. Their names must never be translated when the interface language changes.

### Profile activation

Activating a profile sets its input and output devices as the Windows defaults for all three endpoint roles:

- Console
- Multimedia
- Communications

The operation applies to both playback and recording. A successful activation updates the selected profile, shows a short status message, plays a subtle Windows system sound, and displays a temporary confirmation overlay on every monitor.

AudioSwitcher does not continuously monitor which complete profile is active. The selected profile is an editing and navigation state, while the status message and overlay confirm the most recent successful activation. The redesign must not present selection as guaranteed proof that all current Windows defaults still match that profile.

The overlay must not steal focus or block clicks. It appears near the bottom center of each screen and closes automatically after about 1.6 seconds.

### Global shortcuts

The application supports one optional shortcut per profile and one optional shortcut for quick switching. Shortcuts work while the application is hidden in the system tray.

Shortcut fields are capture controls, not free-text fields. The user focuses a field and presses a combination. A valid combination requires at least one modifier from Ctrl, Alt, Shift, or Win, plus one supported key. Delete or Backspace clears the field.

The application rejects malformed shortcuts, duplicate assignments, and combinations that another Windows application has already registered.

### Quick switching

The user can select profile A and profile B and assign one global shortcut. When the shortcut is pressed, AudioSwitcher reads the current default output device and activates the other profile.

The two selected profiles must be different. The quick-switch shortcut is optional.

## Main user flows

### First launch

1. AudioSwitcher opens with one automatically created profile.
2. The profile uses the current default Windows output and input devices when available.
3. The user names the profile, checks both device selections, and optionally captures a shortcut.
4. The user selects Save to persist the configuration and register the shortcuts.

### Create a profile

1. The user selects the add action beside the profile list.
2. AudioSwitcher creates a profile with a unique generated name and the current default devices.
3. The new profile becomes selected.
4. The user edits its name, devices, and shortcut.
5. The user selects Save.

### Edit a profile

1. The user selects a profile from the list.
2. The detail area shows its current name, output device, input device, and shortcut.
3. Changes remain in the current session until the user selects Save.

### Delete a profile

1. The user selects a profile and chooses Remove profile.
2. The profile disappears from the list immediately.
3. Any quick-switch reference to the deleted profile is cleared.
4. A status message explains that Save is required to persist the change.

### Activate a profile

The user can activate the selected profile with the primary action in the detail area. The same activation behavior can also be triggered by a registered global shortcut.

### Configure quick switching

1. The user selects two different profiles.
2. The user focuses the quick-switch shortcut field and presses a combination.
3. The user selects Save to validate and register the shortcut.

### Change language

1. The user opens Settings.
2. The user selects a language.
3. All visible interface text changes immediately, including text behind the open settings panel.
4. Tray menu labels, status messages, update messages, and future activation overlays use the selected language.
5. The user selects Save to persist the choice.

## Current information architecture

The existing application uses one main window with a persistent header, profile navigation, an editable content area, and a status footer.

| Area | Required content and actions |
| --- | --- |
| Header | Product identity, refresh devices, settings, and Save |
| Profile navigation | Profile list, add profile, select profile, and remove profile |
| Profile editor | Profile name, output device, input device, profile shortcut, and Activate now |
| Quick switch | Profile A, profile B, and the quick-switch shortcut |
| Settings | Language, start with Windows, and an explanation of close-to-tray behavior |
| Footer | Current success or error message and application version |

The current window opens at approximately 1040 by 700 pixels and supports a minimum size of approximately 880 by 620 pixels. The profile editor can scroll when the available height is limited.

The redesign may reorganize these areas, but every action and state must remain easy to find. The profile list and selected profile editor should remain visually connected.

## System tray and window behavior

Closing the main window hides it instead of quitting the application. The tray icon keeps global shortcuts and automatic updates active.

The tray menu contains:

- Open, which restores and activates the main window.
- Exit, which closes the application completely.

Clicking the tray icon also restores the main window. If AudioSwitcher starts with the minimized argument, it opens directly in the tray.

Only one application instance can run at a time.

## Settings and persistence

The user configuration is stored locally for the current Windows user. It includes profiles, device identifiers, shortcuts, quick-switch selections, interface language, and the startup preference.

Save is an explicit commit action. It performs validation, registers global shortcuts, updates the per-user Windows startup setting, and writes the configuration. The redesign must make unsaved changes and the purpose of Save understandable.

The application does not require administrator rights.

## Language support

English is the default and fallback language. The application currently supports:

- English
- Russian
- Ukrainian
- French
- Italian
- Polish

Language names are shown in their native form. Layouts must accommodate longer translated labels without clipping or overlapping controls.

The screenshots in the repository show an older German interface. They are useful as a record of the current layout, but German is not currently one of the selectable interface languages.

## Feedback and states

The redesign must provide clear treatments for these states:

### Normal states

- Ready with a selected profile.
- No profile selected after the last profile is removed.
- Empty shortcut.
- Captured shortcut.
- Device list refreshed.
- Saved configuration.
- Recently activated profile confirmation.
- Update check, download progress, and pending installation.

### Validation and error states

- No profile exists when saving.
- A profile has no name.
- Two profiles use the same name.
- A profile has no input or output device.
- A saved device is no longer available.
- A shortcut is malformed.
- Two actions use the same shortcut.
- A shortcut is already registered by another application.
- Quick switching has fewer than two profiles or uses the same profile twice.
- Configuration loading or saving fails.
- Update checking or preparation fails.

Errors are currently shown in the persistent status area. A redesign may improve their presentation, but errors must remain visible long enough to understand and must not rely on color alone.

## Device behavior

The device selectors show the human-readable Windows device names. Internal device identifiers must not be exposed in the interface.

Refresh devices reloads the currently available Windows audio endpoints. The application then reports the number of output and input devices found.

If a profile references a device that is no longer available, activation must fail safely and identify the affected profile.

## Automatic updates

Installed builds check the public GitHub Releases feed for stable updates. An available update downloads in the background and is installed after the user exits the application.

Update activity appears in the same status system as other messages. Update controls do not need to dominate the main workflow.

## Technical constraints

The implementation is a Windows-only Avalonia desktop application built with C# and .NET 10. It uses MVVM for GUI state.

The redesign must remain practical to implement with standard Avalonia controls. It should not depend on browser-only layout, hover-only interaction, or platform behavior unavailable on Windows 10 and 11.

The final UI must preserve:

- Global shortcut capture and keyboard operation.
- System tray operation and close-to-tray behavior.
- Per-user startup without administrator rights.
- Single-instance behavior.
- Immediate language switching.
- Multi-monitor activation overlays.
- Explicit Save behavior.

## Accessibility requirements

The design must support keyboard navigation for every interactive control. Focus indicators must be clearly visible, and the current focus must never be communicated by color alone.

Controls need accessible names and help text. Shortcut capture fields need instructions that explain how to enter and clear a shortcut. Status and error messages need sufficient contrast and a presentation that screen-reader users can understand.

Hit targets should remain comfortable at common Windows display scaling levels. The layout must continue to work at the minimum window size and at increased text scaling.

## Design freedom

The redesign may replace the current colors, typography, spacing, cards, borders, icons, and visual hierarchy. It may also move Settings into a dedicated view instead of a flyout if the new navigation remains simple.

The designer may reconsider how profiles, profile editing, and quick switching share the available space. The design should remain a focused utility rather than growing into a general audio control center.

Do not add mixers, per-application volume controls, equalizers, device drivers, cloud accounts, telemetry, or profile synchronization. They are outside the product scope.

## Requested design output

Create a coherent desktop design system and provide:

1. A complete main-window design at the normal window size.
2. The selected, hover, focus, disabled, success, warning, and error states.
3. The add, edit, delete, activate, save, and refresh interactions.
4. The Settings experience with language and Windows startup controls.
5. The shortcut-capture interaction before, during, and after capture.
6. The quick-switch configuration experience.
7. The profile activation overlay.
8. Guidance for responsive behavior down to the minimum window size.
9. Design tokens for color, typography, spacing, corner radius, borders, and focus treatment.
10. Component specifications that can be implemented in Avalonia without changing product behavior.

## Acceptance checklist

A redesign is ready for implementation when:

- Every existing function in this brief has a visible and understandable place.
- The primary profile activation action is immediately recognizable.
- A user can distinguish profile selection, profile editing, and profile activation.
- Save and unsaved changes are understandable.
- Shortcut capture is discoverable without reading external documentation.
- Validation and unavailable-device errors have defined states.
- All six supported languages fit without broken layouts.
- Keyboard focus and screen-reader labels are covered.
- The design works at the normal and minimum window sizes.
- Tray behavior, automatic updates, and activation overlays are represented where relevant.
