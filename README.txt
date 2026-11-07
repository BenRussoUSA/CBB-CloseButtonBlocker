CBB-CloseButtonBlocker 0.9.4beta
Author: BenRussoUSA
License: GNU General Public License version 3 (GPL-3.0-only)

An accidental click on the X can close a window you wanted to keep open. CBB
helps prevent that for the windows you choose. To get started, open CBB
from Start, then left-click its tray icon to see the window table. Turn Block
Close Button On beside a window to disable its standard Windows Close command.
Turn it Off to restore normal closing. You can also select a toggle and press
Space. N/A means the window does not expose a standard Close command.

The Close button may remain visible. Some applications handle closing differently,
so please try CBB with an empty document first. An application's own Exit command
or Task Manager can still close it.

Right-click the tray icon for Settings or Check for updates. Settings lets you
choose Launch at startup and save automatic blocking rules. Rules can match
an exact window title or a regular expression. If regular expressions are new to
you, Help includes examples. The editor previews matching windows before saving.
You can also right-click a window in the table to create a rule based on its title.

Rules survive reboots and upgrades. A manual choice lasts until that window
closes or CBB exits. A newly opened matching window is blocked again. Removing
a rule does not restore an already blocked window; turn its toggle Off.

Version 0.9.4 focuses on resource efficiency. It uses .NET Framework 4.8.1 and
Windows Forms supplied with Windows 11, without bundling a separate runtime.
It listens for window events instead of scanning every 700 ms. See the complete
README and release notes for measured distribution size and resource usage.

Install the MSI on Windows 11 x64 version 22H2 or later with its included
Framework 4.8.1. The installer works offline, including from a USB drive, and
checks the Framework version before proceeding. The original Windows 11 release
included Framework 4.8 and needs an update first. No dependencies are downloaded
by this installer.

To upgrade, run the newer MSI and launch CBB from Start afterwards. Saved rules
and the startup preference are retained. Application files are installed in:
%LOCALAPPDATA%\Programs\CBB-CloseButtonBlocker
Settings are stored separately in:
%LOCALAPPDATA%\CBB-CloseButtonBlocker
Windows maintains its own installer registration and cache.

Update checks contact GitHub at launch and every 24 hours. A notification opens
the newer release's download page. CBB does not download or install updates
without user action. Window titles and rules are not sent to GitHub, and CBB
contains no GitHub credentials.

Exit CBB restores Close states changed during the current session. A crash or
forced termination can leave a button disabled; restart the affected application
if necessary. To uninstall CBB, use Windows Settings > Apps > Installed apps.
Uninstalling removes startup registration and retains rules for reinstalling.

GitHub project and full README:
https://github.com/BenRussoUSA/CBB-CloseButtonBlocker
Releases and downloads:
https://github.com/BenRussoUSA/CBB-CloseButtonBlocker/releases

Complete application source and build instructions are in src\.
See src\docs\BUILD.md for building and src\docs\UAT.md for testing.
See THIRD-PARTY-NOTICES.txt for build-tool notices.
This beta is unsigned. Windows may display an unknown-publisher warning.
