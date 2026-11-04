# CBB-CloseButtonBlocker 0.9.0beta

First public beta for Windows 11 x64, by BenRussoUSA.

[Download Installer (.MSI)](https://github.com/BenRussoUSA/CBB-CloseButtonBlocker/releases/download/v0.9.0beta/CBB-CloseButtonBlocker-0.9.0beta-win-x64.msi)

The installer includes the .NET runtime and complete CBB source. It can be copied
to a USB drive and installed offline without additional dependency downloads.
Downloads do not require GitHub sign-in.

Click the tray icon to open the window table. Turn on **Hide Close Button** next
to a window to disable its Windows Close command. Turn it off to restore normal
closing. The button may remain visible. Unsupported windows show N/A.

Right-click the tray icon for Settings, About, Check for updates or Exit CBB.
Settings stores exact-title and regex rules across reboots. A manual override
lasts until that window closes or CBB exits. Newly opened matching windows are
protected again. Launch at startup is optional.

Run this MSI to upgrade an earlier version. Saved rules and the startup
preference are retained. CBB stops during installation; launch it from Start
afterwards. Uninstall through Windows Settings > Apps > Installed apps.

Update checks contact GitHub at launch and every 24 hours while running.
A newer version can produce a clickable Windows notification to its release page.
Updates are downloaded and installed by the user.

This beta is unsigned. Custom title bars may ignore the standard Close command;
test the applications you use. Elevated applications may reject changes.
CBB is an accidental-close aid, not a security boundary.

The project is licensed under GPL-3.0-only. Comments and patch suggestions are
welcome through [Issues](https://github.com/BenRussoUSA/CBB-CloseButtonBlocker/issues)
or [Discussions](https://github.com/BenRussoUSA/CBB-CloseButtonBlocker/discussions).
Code changes can be submitted through pull requests.

[README](https://github.com/BenRussoUSA/CBB-CloseButtonBlocker#readme) ·
[License](https://github.com/BenRussoUSA/CBB-CloseButtonBlocker/blob/v0.9.0beta/LICENSE.txt) ·
[Contributing](https://github.com/BenRussoUSA/CBB-CloseButtonBlocker/blob/v0.9.0beta/CONTRIBUTING.md) ·
[UAT checklist](https://github.com/BenRussoUSA/CBB-CloseButtonBlocker/blob/v0.9.0beta/docs/UAT.md)
