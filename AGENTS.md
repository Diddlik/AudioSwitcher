# Shared Agent Instructions

This file is the canonical instruction source for Codex, Claude Code, and GitHub Copilot CLI.

## Maintenance

- Keep this file synchronized with the repository's verified behavior.
- Update this file in the same change when build commands, validation steps, architecture constraints, workflows, or conventions change.
- Remove obsolete instructions instead of appending corrections.
- Record only durable facts that are not obvious from the codebase.
- Tool-specific instruction files may contain only an import of this file and genuine tool-specific exceptions.

## First-use onboarding

If any `[TO FILL]` entry remains, complete this onboarding before implementing the user's first task:

1. Inspect the repository and determine every project fact that can be verified from existing files and commands.
2. Do not ask the user for information that can be discovered reliably from the repository.
3. Briefly present the discovered facts, then ask guided questions only for the remaining decisions or unknowns.
4. Ask one focused question or one closely related group of no more than three questions at a time. Explain why each answer matters and offer a recommended option when useful while allowing a free-form answer.
5. Cover the remaining topics in this order: purpose and scope, runtimes and platforms, build and validation commands, environment requirements, then architecture and generated-file constraints.
6. After the user answers, replace the applicable placeholders with concise verified facts, remove entries that do not apply, and record unresolved decisions explicitly as `OPEN` rather than inventing an answer.
7. Summarize what was written to this file, then continue with the user's original task.

## Implementation

- Implement only what the current requirement needs.
- Prefer editing existing code over adding files, layers, helpers, or abstractions.
- Use the standard library and existing dependencies before writing custom implementations.
- Do not add speculative extension points, configuration, parameters, or abstractions.
- Add a dependency only when it provides a concrete benefit and does not duplicate existing functionality.
- Preserve validation, security, accessibility, error handling, and data-integrity safeguards.

## Language and comments

- Use English for source code, identifiers, comments, tests, documentation, logs, and commit messages.
- Keep commit messages limited to change- and process-related content.
- Do not add AI attribution or co-author trailers such as `Co-Authored-By: Copilot`, Claude, Codex, or similar.
- Comments explain why a decision, constraint, workaround, or non-obvious trade-off exists.
- Do not comment what readable code already expresses.
- Prefer clear naming and small functions over explanatory comments.

## Working method

- Inspect the relevant implementation and existing conventions before editing.
- Search for an existing implementation before creating a new one.
- Make the smallest coherent change that fully satisfies the request.
- Preserve unrelated user changes.
- Do not perform unrelated refactoring during a focused change.
- Ask before destructive, irreversible, security-sensitive, or materially out-of-scope actions.

## Verification

- Run the narrowest relevant test, build, lint, format, or executable check after the last change.
- Add or update tests for non-trivial behavior changes and bug fixes.
- Do not claim success without current verification evidence.
- Report what was verified and what could not be verified.
- Distinguish product failures from environment, permission, network, and tooling failures.

## Security

- Never commit, print, store, or document secrets, tokens, credentials, or private keys.
- Validate data at user, file, environment, process, and network boundaries.
- Preserve authentication, authorization, escaping, permission checks, and safe defaults.
- Do not weaken security controls to make tests or local execution pass.

## Project facts

- Purpose: Windows desktop utility for defining audio profiles, assigning one default input and output device per profile, activating profiles through global shortcuts, and toggling between two profiles. Runs in the system tray and supports optional Windows startup.
- Primary languages and runtimes: C# on .NET 10 with Avalonia UI.
- Important entry points: `src/AudioSwitcher/Program.cs` starts the application; `src/AudioSwitcher/App.axaml` configures Avalonia; `src/AudioSwitcher/Views/MainWindow.axaml` is the main GUI.
- Build command: `dotnet build`
- Test command: `dotnet test`
- Lint and format command: `dotnet format --verify-no-changes`
- Local run command: `dotnet run --project src/AudioSwitcher`
- Required environment: Windows 10 or 11. The development environment has .NET SDK 10.0.401 installed.
- Architecture constraints: Use MVVM for GUI state, NAudio WASAPI for endpoint discovery, the Windows policy configuration COM interface for default-endpoint changes, and `RegisterHotKey` for global shortcuts. Velopack checks the public GitHub Releases feed automatically, downloads stable updates in the background, and installs a pending update when the app exits. The GUI defaults to English and supports English, Russian, Ukrainian, French, Italian, and Polish; source code, identifiers, comments, tests, documentation, and logs are English. Activating a profile sets its input and output endpoints as the defaults for the Windows Console, Multimedia, and Communications roles. Persist user configuration, including the selected language, in `%LocalAppData%\AudioSwitcher\config.json`. Closing the window hides it to the system tray; the tray menu provides explicit exit. Configure optional startup per user without administrator rights. Enforce a single running application instance.
- Generated files: `bin/`, `obj/`, `artifacts/`, and `Releases/`; do not commit or edit them manually.
- Files or directories not to edit manually: `bin/`, `obj/`, `artifacts/`, and `Releases/`.
- Platform-specific constraints: Windows-only. Global shortcuts use `RegisterHotKey`, tray integration uses Avalonia Win32, startup uses the current user's `Run` registry key, and default-endpoint switching uses the Windows `IPolicyConfig` COM interface.

Do not begin implementation while `[TO FILL]` entries remain. Follow the guided onboarding above instead.

## Definition of done

A change is complete when:

- The requested behavior is implemented.
- Relevant verification passes after the final edit.
- Appropriate error paths and edge cases are handled.
- Documentation and this file reflect changed behavior or workflows.
- No unrelated files, abstractions, or dependencies were introduced.
- Remaining limitations and unverified points are stated clearly.
