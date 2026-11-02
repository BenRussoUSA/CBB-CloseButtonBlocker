CBB-CloseButtonBlocker 0.8.0alpha
Author: BenRussoUSA
License: GNU General Public License version 3 (GPL-3.0-only)

PRIVATE ALPHA FOR USER ACCEPTANCE TESTING

GitHub project and README:
https://github.com/BenRussoUSA/CBB-CloseButtonBlocker
Releases and update announcements:
https://github.com/BenRussoUSA/CBB-CloseButtonBlocker/releases

Private repository links require GitHub sign-in and repository access.

CBB runs in the Windows notification area. Click its icon to list open
windows. Right-click a title and choose Close Button OFF to disable the
standard Windows Close command. Close Button ON restores it. The active
option is bold and marked with a diamond.

Settings lets you create exact-title or regular-expression rules. Matching
windows are blocked on first detection, including when CBB starts. A manual
Close Button ON choice lasts for that window until it closes or CBB exits.
New matching windows are blocked again. The Help button explains regex.
Settings includes startup control; it defaults to off.

Install the MSI, then open CBB-CloseButtonBlocker from the Start menu.
To upgrade, download and run the newer MSI. Saved settings are retained.
The included .NET runtime means no separate .NET installation is needed.
Windows 11 x64 is the initial target.

Application files install together under:
%LOCALAPPDATA%\Programs\CBB-CloseButtonBlocker
Settings and backup are stored separately under:
%LOCALAPPDATA%\CBB-CloseButtonBlocker
Windows maintains its own installer registration and cache.

Use Exit CBB to restore buttons CBB disabled. Forced termination or crashes
can leave a button disabled until the affected application is restarted.
Custom title bars may ignore Windows' disabled Close command even when its
menu state changes. Elevated applications may reject changes. This is an
accidental-close aid, not a security boundary. Keyboard/system-menu Close
can also be affected. Task Manager can still terminate applications.

Update checks run at launch and every 24 hours while running. Public GitHub
updates use a clickable Windows notification. This private alpha does not
store GitHub credentials; check private releases in the browser manually.
No automatic downloads or installations occur.

Uninstall from Windows Installed apps or the CBB menu. Feedback is optional
and opens GitHub in the browser. Settings are retained for reinstalling.
Use the Installed apps entry if the tray application is not running.

Complete application source and installer/build definitions are in src\.
See src\docs\BUILD.md for building and src\docs\UAT.md for testing.
Required third-party notices are in licenses\.
This alpha is unsigned. Windows may display an unrecognized-publisher prompt.
