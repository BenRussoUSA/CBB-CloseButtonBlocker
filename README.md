# CBB-CloseButtonBlocker

Windows 11 tray utility by **BenRussoUSA**. Current version: **0.8.0alpha**.
Private alpha for user acceptance testing. Licensed under **GPL-3.0-only**.

CBB reduces accidental closing of selected windows by disabling their
standard Windows Close command. It is not a security or parental-control tool.

## Use

Install the x64 MSI and launch **CBB-CloseButtonBlocker** from the Start menu.
Click the tray icon (possibly under hidden icons), then right-click a window
title. **Close Button ON** means normal behavior; **Close Button OFF** blocks
the Windows Close command. The active option is bold with a diamond.

In **Settings**, add exact titles or regex rules. You can edit or remove rules.
**Help** includes examples, and the rule editor can test a sample title.
Exact titles are case-sensitive; regex supports `(?i)` for case-insensitive matching.

Rules apply when a window is first detected, including existing windows at
CBB startup. Saving rules checks open windows that have no manual override.
A manual selection remains in effect until that window closes or CBB exits,
even if the title changes. Newly opened matching windows are blocked again.
Removing a rule does not unblock an existing window; use Close Button ON.

Startup is opt-in. **Exit CBB** restores the Close states CBB changed.

## Installation and updates

Download the MSI from [Releases](https://github.com/BenRussoUSA/CBB-CloseButtonBlocker/releases).
Private alpha downloads require sign-in and repository access. Run the downloaded
MSI to upgrade the existing installation. Newer installers preserve user settings
and reject downgrades. Program files include the .NET runtime and complete CBB
source, plus installer definitions and build instructions.

Program folder: `%LOCALAPPDATA%\Programs\CBB-CloseButtonBlocker`.
Settings: `%LOCALAPPDATA%\CBB-CloseButtonBlocker\settings.json`.
Each saved update retains the previous settings file as `settings.json.bak`.
Windows also stores installer registration and cached installation data.

Update checks contact GitHub at launch and every 24 hours while CBB runs.
Public releases can generate a clickable Windows notification. Private releases
must be checked manually in the browser; CBB contains no GitHub token.
The check sends no window titles or rule contents. GitHub receives ordinary
request information, including the source IP address.

Uninstall through Windows Installed apps or the tray menu. The dialog offers
**Please leave feedback via GitHub** first. Feedback is optional. Uninstalling
retains settings for a future reinstall and removes the startup registration.

## Alpha limitations

- Custom title bars may ignore the system-menu state. The alpha cannot guarantee
  blocking in every application; visual/click behavior needs UAT per application.
- Elevated or protected applications may reject changes. CBB does not elevate itself.
- The Windows Close command can also affect Alt+F4 or the system-menu Close entry.
- Forced termination or a crash can leave buttons disabled. Restart the affected
  application if necessary. CBB only restores buttons it changed during its session.
- Detection is polled approximately every 700 ms. A newly opened window may be
  closable briefly before the first scan.
- Unsigned alpha; Windows may show an unrecognized-publisher warning.
- x64 Windows 11 only in this first release. ARM64 and x86 are not packaged.

## Development and contributions

See [build instructions](docs/BUILD.md), [UAT checklist](docs/UAT.md), and
[contribution guidelines](CONTRIBUTING.md). Use Issues for reports or patch ideas.
Use Discussions for general comments when enabled. Propose code through pull requests.
While private, these require an invitation to the repository.

## License

Copyright (C) 2026 BenRussoUSA. CBB code is licensed under GPL version 3 only;
see [LICENSE.txt](LICENSE.txt). Third-party code retains its own license.
GPLv3 allows commercial redistribution; corresponding-source obligations apply.
