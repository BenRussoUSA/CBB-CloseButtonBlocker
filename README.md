# CBB-CloseButtonBlocker

## Prevent accidental window closing

CBB is a small Windows 11 tray utility that lets you turn off the Close button
on selected windows. You can also save window-title rules so matching windows
are automatically protected when they open.

Created by **BenRussoUSA** · **0.8.1alpha** · Windows 11 x64 · GPLv3

### [Download Installer (.MSI)](https://github.com/BenRussoUSA/CBB-CloseButtonBlocker/releases/download/v0.8.1alpha/CBB-CloseButtonBlocker-0.8.1alpha-win-x64.msi)

About **38 MiB**. Includes the .NET runtime and complete CBB source. Copy it to a
USB drive to install offline; no additional dependency downloads are required.
The installed application files occupy about **118 MiB**, plus Windows' installer cache.

| Link | What you will find |
| --- | --- |
| [Download Installer](https://github.com/BenRussoUSA/CBB-CloseButtonBlocker/releases/download/v0.8.1alpha/CBB-CloseButtonBlocker-0.8.1alpha-win-x64.msi) | Recommended Windows x64 MSI |
| [README](#use) | Setup and usage instructions below |
| [LICENSE](LICENSE.txt) | GNU General Public License version 3 |
| [CONTRIBUTING](CONTRIBUTING.md) | How to suggest changes or submit patches |
| [Release notes and other downloads](https://github.com/BenRussoUSA/CBB-CloseButtonBlocker/releases/tag/v0.8.1alpha) | ZIP distribution and checksums |
| [Report a problem](https://github.com/BenRussoUSA/CBB-CloseButtonBlocker/issues) | Bug reports and feedback |
| [Discuss the project](https://github.com/BenRussoUSA/CBB-CloseButtonBlocker/discussions) | General comments and questions |

**Private alpha:** currently available only to the owner and invited testers.
This repository has not been made public. Downloads require GitHub sign-in and
repository access. The alpha is unsigned and may trigger a Windows publisher warning.

### Get started

1. Download and run the MSI, then launch **CBB-CloseButtonBlocker** from Start.
2. Click the tray icon and right-click the window you want to protect.
3. Choose **Close Button OFF**. Choose **Close Button ON** to restore normal closing.

CBB works through the standard Windows Close command. Applications with custom
title bars may behave differently. It is not a security or parental-control tool.

## Use

Install the x64 MSI and launch **CBB-CloseButtonBlocker** from the Start menu.
Left-click the tray icon (possibly under hidden icons), then right-click a window
title. **Close Button ON** means normal behavior; **Close Button OFF** blocks
the Windows Close command. The active option is bold with a diamond.

The indented window list shows a green filled circle for Close Button ON and
the CBB logo for OFF. A gray outline circle means no standard Close command
was found. The author link at the bottom right opens the GitHub profile.

In **Settings**, add exact titles or regex rules. You can edit or remove rules.
**Help** includes examples, and the rule editor can test a sample title.
Exact titles are case-sensitive; regex supports `(?i)` for case-insensitive matching.

Rules apply when a window is first detected, including existing windows at
CBB startup. Saving rules checks open windows that have no manual override.
A manual selection remains in effect until that window closes or CBB exits,
even if the title changes. Newly opened matching windows are blocked again.
Removing a rule does not unblock an existing window; use Close Button ON.

Startup is opt-in and is controlled at the top of Settings. Right-click the
tray icon for **About CBB-CloseButtonBlocker**, **Check for updates**, or
**Exit CBB**. Exit restores the Close states CBB changed.

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
