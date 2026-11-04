# CBB-CloseButtonBlocker

## Prevent accidental window closing

CBB is a small Windows 11 tray utility that lets you turn off the Close button
on selected windows. You can also save window-title rules so matching windows
are automatically protected when they open.

Created by **BenRussoUSA** · **0.9.0beta** · Windows 11 x64 · GPLv3

### [Download Installer (.MSI)](https://github.com/BenRussoUSA/CBB-CloseButtonBlocker/releases/download/v0.9.0beta/CBB-CloseButtonBlocker-0.9.0beta-win-x64.msi)

About **38 MiB**. Includes the .NET runtime and complete CBB source. Copy it to a
USB drive to install offline; no additional dependency downloads are required.
The installed application files occupy about **118 MiB**, plus Windows' installer cache.

| Link | What you will find |
| --- | --- |
| [Download Installer](https://github.com/BenRussoUSA/CBB-CloseButtonBlocker/releases/download/v0.9.0beta/CBB-CloseButtonBlocker-0.9.0beta-win-x64.msi) | Recommended Windows x64 MSI |
| [README](#use) | Setup and usage instructions below |
| [LICENSE](LICENSE.txt) | GNU General Public License version 3 |
| [CONTRIBUTING](CONTRIBUTING.md) | How to suggest changes or submit patches |
| [Release notes and other downloads](https://github.com/BenRussoUSA/CBB-CloseButtonBlocker/releases/tag/v0.9.0beta) | ZIP distribution and checksums |
| [Report a problem](https://github.com/BenRussoUSA/CBB-CloseButtonBlocker/issues) | Bug reports and feedback |
| [Discuss the project](https://github.com/BenRussoUSA/CBB-CloseButtonBlocker/discussions) | General comments and questions |

**Public beta:** downloads are available without GitHub sign-in.
The installer is unsigned and may trigger a Windows publisher warning.
Test with the applications you use before relying on Close-button blocking.

### Get started

1. Download and run the MSI, then launch **CBB-CloseButtonBlocker** from Start.
2. Click the tray icon to open the window table.
3. Turn on **Hide Close Button** beside a window to block its Close command.
   Turn the toggle off to restore normal closing.

CBB works through the standard Windows Close command. Applications with custom
title bars may behave differently. It is not a security or parental-control tool.

## Use

Install the x64 MSI and launch **CBB-CloseButtonBlocker** from the Start menu.
Left-click the tray icon (possibly under hidden icons) to open a table with
**Hide Close Button** on the left and **Window Title** on the right.
Click a toggle or focus it and press Space. **On** disables the Windows Close
command; **Off** allows normal closing. The heading describes the protection
setting: CBB disables the Close command and does not physically hide the button.
**N/A** means no standard Close command was found; that toggle cannot be changed.
The author link at the bottom right opens the GitHub profile.

Right-click the tray icon and choose **Settings** to add exact titles or regex
rules. You can edit or remove rules.
**Help** includes examples, and the rule editor can test a sample title.
Exact titles are case-sensitive; regex supports `(?i)` for case-insensitive matching.

Rules apply when a window is first detected, including existing windows at
CBB startup. Saving rules checks open windows that have no manual override.
A manual selection remains in effect until that window closes or CBB exits,
even if the title changes. Newly opened matching windows are blocked again.
Removing a rule does not unblock an existing window; turn its toggle off.

Startup is opt-in and is controlled at the top of Settings. The tray icon's
right-click menu also provides **About CBB-CloseButtonBlocker** and
**Check for updates**. **Exit CBB** restores the Close states CBB changed.

## Installation and updates

Download the MSI from [Releases](https://github.com/BenRussoUSA/CBB-CloseButtonBlocker/releases).
Run the downloaded MSI to upgrade the existing installation, including earlier
private test versions. Newer installers preserve user settings
and reject downgrades. Program files include the .NET runtime and complete CBB
source, plus installer definitions and build instructions.

Program folder: `%LOCALAPPDATA%\Programs\CBB-CloseButtonBlocker`.
Settings: `%LOCALAPPDATA%\CBB-CloseButtonBlocker\settings.json`.
Each saved update retains the previous settings file as `settings.json.bak`.
Windows also stores installer registration and cached installation data.

Update checks contact GitHub at launch and every 24 hours while CBB runs.
A newer release can generate a clickable Windows notification that opens its
GitHub release page. CBB contains no GitHub token.
The check sends no window titles or rule contents. GitHub receives ordinary
request information, including the source IP address.

Uninstall through **Windows Settings → Apps → Installed apps**, select
**CBB-CloseButtonBlocker**, and choose **Uninstall**. CBB has no in-app uninstall
command or feedback dialog. Uninstalling retains settings for a future reinstall
and removes the startup registration.

## Beta limitations

- Custom title bars may ignore the system-menu state. The beta cannot guarantee
  blocking in every application; visual/click behavior needs UAT per application.
- Elevated or protected applications may reject changes. CBB does not elevate itself.
- The Windows Close command can also affect Alt+F4 or the system-menu Close entry.
- Forced termination or a crash can leave buttons disabled. Restart the affected
  application if necessary. CBB only restores buttons it changed during its session.
- Detection is polled approximately every 700 ms. A newly opened window may be
  closable briefly before the first scan.
- Unsigned beta; Windows may show an unrecognized-publisher warning.
- x64 Windows 11 only in this first release. ARM64 and x86 are not packaged.

## Development and contributions

See [build instructions](docs/BUILD.md), [UAT checklist](docs/UAT.md), and
[contribution guidelines](CONTRIBUTING.md). Use Issues for reports or patch ideas.
Use Discussions for general comments. Propose code through pull requests.
GitHub sign-in is required to post comments or submit contributions.

## License

Copyright (C) 2026 BenRussoUSA. CBB code is licensed under GPL version 3 only;
see [LICENSE.txt](LICENSE.txt). Third-party code retains its own license.
GPLv3 allows commercial redistribution; corresponding-source obligations apply.
