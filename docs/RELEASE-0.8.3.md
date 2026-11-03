# CBB-CloseButtonBlocker 0.8.3

Private test release for Windows 11 x64.

[Download the 0.8.3 installer (.MSI)](https://github.com/BenRussoUSA/CBB-CloseButtonBlocker/releases/download/v0.8.3/CBB-CloseButtonBlocker-0.8.3-win-x64.msi)

The window list is now a table with a narrow **Hide Close Button** column and
a wide **Window Title** column. Turn a slider On to disable a window's Close
command, or Off to restore normal closing. Space also operates a focused toggle.
Unsupported windows show N/A. CBB disables the Close command; the button may
remain visible.

Settings is now in the tray icon's right-click menu. The window table retains
the author link. Uninstall through Windows Settings > Apps > Installed apps;
the in-app uninstall command and feedback dialog have been removed.

Run the MSI to upgrade. Saved rules and the startup preference are retained.
CBB stops during installation; launch it again from Start afterwards.
The installer includes the .NET runtime and source code for offline installation.

This repository remains private. Downloads require sign-in with repository
access. The installer is unsigned. Application compatibility still needs
[UAT](UAT.md), particularly for custom title bars.
