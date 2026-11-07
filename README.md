# CBB-CloseButtonBlocker

An accidental click on the X can close a window you wanted to keep open. CBB
helps prevent that by letting you block the Close button on the windows you
choose. It runs in the Windows notification area, with a small tray icon that
opens your window list whenever you need it.

You can start with a single window. If you find yourself protecting the same
applications regularly, save a rule and CBB will apply it when matching windows
first appear.

CBB is a free, open-source project by [BenRussoUSA](https://github.com/BenRussoUSA).
Version 0.9.4beta focuses on a smaller installation and less background work.

## Download and install

### [Download the Windows installer (.MSI)](https://github.com/BenRussoUSA/CBB-CloseButtonBlocker/releases/download/v0.9.4beta/CBB-CloseButtonBlocker-0.9.4beta-win-x64.msi)

The installer is about 148 KiB, including the complete CBB source. Download the
MSI and run it to install CBB or upgrade an existing copy. Your saved rules and
startup preference are kept during an upgrade.

You can also copy the MSI to a USB drive and install without internet access.
On Windows 11 x64 version 22H2 or later, CBB uses the .NET Framework 4.8.1
libraries already included with Windows. There is no separate runtime to
download. The installer checks this for you before continuing.

If you are using the original Windows 11 release, update Windows first: that
version included Framework 4.8. See [Microsoft's Windows and Framework version
table](https://learn.microsoft.com/en-us/dotnet/framework/get-started/system-requirements)
for the details.

| Link | What it contains |
| --- | --- |
| [Download Installer](https://github.com/BenRussoUSA/CBB-CloseButtonBlocker/releases/download/v0.9.4beta/CBB-CloseButtonBlocker-0.9.4beta-win-x64.msi) | The Windows x64 MSI |
| [README](#using-cbb) | Instructions for using CBB |
| [Release notes](docs/RELEASE-0.9.4beta.md) | Changes and resource measurements for 0.9.4 |
| [Other downloads](https://github.com/BenRussoUSA/CBB-CloseButtonBlocker/releases) | Release announcements, ZIP packages and SHA-256 checksums |
| [LICENSE](LICENSE.txt) | The GNU General Public License, version 3 |
| [CONTRIBUTING](CONTRIBUTING.md) | How to report a problem or propose a change |

You do not need a GitHub account to download CBB. This beta is unsigned, so
Windows may display an unknown-publisher warning. After installation, open
CBB-CloseButtonBlocker from the Start menu. If you do not see its tray icon,
check the notification area's hidden-icons menu.

## Using CBB

Left-click the tray icon to see your open windows. In the Block Close Button
column, turn a window's toggle On to disable its standard Windows Close command.
Turn it Off to restore normal closing. You can click the toggle or select it
with the keyboard and press Space. An N/A toggle means that the window does not
provide a standard Close command that CBB can control.

The Close button may remain visible while blocked. It is best to try CBB with
an empty document first, because some applications use their own title bars or
handle closing differently. The [compatibility notes](docs/COMPATIBILITY.md)
explain what has been checked and where your testing can help.

Use Search to find a window by part of its title. Select Show blocked windows
only to reduce the list to windows whose standard Close command is disabled.
Right-click a window to create an automatic blocking rule based on its title.
The rule editor shows matching open windows before you save.

Right-click the tray icon to open Settings or check for updates. The same menu
contains About CBB-CloseButtonBlocker and Exit CBB. Exiting restores the Close
states that CBB changed during that session.

## Automatic blocking and startup

If you want CBB to be available whenever you sign into Windows, enable Launch
at startup at the top of Settings. It is off until you choose to enable it.

Below that option, you can add exact window titles or regular expressions.
You can edit or remove a rule at any time. If regular expressions are new to
you, the Help button includes examples, and you can test a sample title in the
editor before saving anything.

An exact title must match the whole window title, including capitalization.
For example, `Report - Notepad` matches that title only. A regular expression
such as `.* - Notepad$` matches titles ending in ` - Notepad`. Add `(?i)` at the
start of a regular expression to ignore capitalization.

Rules apply when CBB first detects a window, including windows already open at
startup. Saving rules also checks existing windows that have no manual override.
If you later turn a window's toggle Off, that choice lasts until the window
closes or CBB exits, even if its title changes. A newly opened matching window
is protected again. Removing a rule does not unblock a window already protected;
turn that window's toggle Off when you want to restore its Close command.

CBB keeps your rules across reboots and upgrades. There is room for up to 100
rules, with up to 2,048 characters per pattern. If a regular expression takes
longer than its 50 ms matching limit, CBB pauses that rule and displays a
notification. You can edit the pattern to simplify it, or restart CBB to try
it again. This prevents a slow pattern from repeatedly consuming CPU.

## Smaller installation and less background work

Version 0.9.4 uses the .NET Framework and Windows Forms libraries supplied with
Windows 11. Earlier packages carried their own modern .NET runtime; this one
contains the application and its complete source, without that runtime copy.
The [resource measurements](docs/PERFORMANCE.md) compare the distribution size
and installed files with 0.9.0beta. They also explain the memory and CPU samples.

CBB now listens for Windows events instead of scanning every 700 ms. It combines
bursts of notifications before updating affected windows, and it updates the
visible table when information changes. It still takes an initial snapshot at
startup and refreshes when you open the table or save rules. Event delivery is
asynchronous, so blocking a newly opened window is not instantaneous.

## Updates and uninstalling

CBB checks GitHub at launch and every 24 hours while running. If a newer release
is available, a Windows notification opens that release's download page. You
choose when to download and run the new MSI. The installer replaces the previous
version while preserving saved rules and the startup preference. Launch CBB from
Start after the upgrade. Older installers are prevented from replacing a newer
installed version.

Application files are kept in
`%LOCALAPPDATA%\Programs\CBB-CloseButtonBlocker`. Settings are stored separately
in `%LOCALAPPDATA%\CBB-CloseButtonBlocker\settings.json`, so upgrades do not replace
them. Saving settings keeps the previous file as `settings.json.bak`. Windows
also maintains installer registration and a cached copy of installation data.

To uninstall, open Windows Settings, then Apps, then Installed apps. Find
CBB-CloseButtonBlocker and choose Uninstall. This removes startup registration
but retains your rules for a future reinstall.

## Privacy and limitations

Window titles and rules stay on your computer. Update checks send no window
titles or rule contents, and CBB stores no GitHub credentials. GitHub receives
normal connection information, including your IP address, when an update check
runs or you open a GitHub link. CBB remains usable without internet access.

CBB is intended to help with accidental closing. A program's own Exit command
or Task Manager can still close it. Blocking the standard Close command can also
affect Alt+F4 and the system-menu Close entry. Applications running as
administrator may reject changes because CBB runs with ordinary user permissions.

A crash or forced termination can leave a Close button disabled. If necessary,
restart the affected application. This beta packages x64 only; it does not
include an ARM64 or x86 installer.

## Contributing and licensing

Found a problem or have an idea? You can open an
[issue](https://github.com/BenRussoUSA/CBB-CloseButtonBlocker/issues), or join
[Discussions](https://github.com/BenRussoUSA/CBB-CloseButtonBlocker/discussions)
for general questions and feedback. You do not need to write code to contribute;
reports about how CBB works with your applications are useful too.

If you would like to suggest a code change, submit a pull request. The
[build instructions](docs/BUILD.md) and [UAT checklist](docs/UAT.md) explain how
to build and test CBB. GitHub requires an account to post or contribute.

Copyright (C) 2026 BenRussoUSA. CBB is licensed under GPL-3.0-only. The license
allows commercial redistribution and requires corresponding source under its
terms. See [LICENSE.txt](LICENSE.txt) for the full license.
