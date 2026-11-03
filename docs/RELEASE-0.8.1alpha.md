# CBB-CloseButtonBlocker 0.8.1alpha

Private Windows 11 x64 alpha by BenRussoUSA.

[Download Installer (.MSI)](https://github.com/BenRussoUSA/CBB-CloseButtonBlocker/releases/download/v0.8.1alpha/CBB-CloseButtonBlocker-0.8.1alpha-win-x64.msi)

Run the MSI to upgrade 0.8.0alpha. Saved rules and the startup preference are
retained. CBB stops during installation; reopen it from Start afterward.
The runtime and complete source are included for offline installation.

## Interface changes

- Left-click keeps opening the window list, now labeled
  "List of windows   (right click one to change close button state)".
- Titles are indented. A small green filled circle indicates Close Button ON;
  the CBB logo indicates OFF. A gray outline identifies an unsupported window.
- Launch at startup is available at the top of Settings, with no duplicate
  checkbox on the window-list popup.
- Right-clicking the tray icon opens About CBB-CloseButtonBlocker,
  Check for updates, and Exit CBB.
- A blue underlined "by BenRussoUSA @ GitHub" link appears at the bottom right.

Settings and Uninstall remain on the window-list popup. The blocking rules and
manual override behavior are unchanged. See [UAT](https://github.com/BenRussoUSA/CBB-CloseButtonBlocker/blob/v0.8.1alpha/docs/UAT.md).

The alpha remains unsigned. Custom title bars may ignore the standard Windows
Close command; an OFF indicator reflects the Windows menu state. Private
release checks require a signed-in browser with repository access.
