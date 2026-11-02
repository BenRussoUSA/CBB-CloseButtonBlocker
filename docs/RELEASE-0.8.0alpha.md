# CBB-CloseButtonBlocker 0.8.0alpha

Private Windows 11 x64 alpha by BenRussoUSA, ready for user acceptance testing.

[Download the offline MSI](https://github.com/BenRussoUSA/CBB-CloseButtonBlocker/releases/download/v0.8.0alpha/CBB-CloseButtonBlocker-0.8.0alpha-win-x64.msi)

Copy the MSI to a USB drive and run it on the target PC. Installation and normal
blocking work without internet; the .NET runtime is included. GitHub links and
update checks require internet. Private downloads require repository access.

After installation, open **CBB-CloseButtonBlocker** from Start. Click its tray
icon, then right-click a window title. Choose **Close Button OFF** to block the
Windows Close command; **Close Button ON** restores it.

Included: persistent exact-title/regex rules, rule editing/removal and Help,
opt-in startup, optional GitHub feedback, and the complete source in `src`.
License: GPL-3.0-only. Copyright (C) 2026 BenRussoUSA.

## Validation

- Release build completed without compiler warnings or errors.
- 30 automated checks passed, including native Windows fixture lifecycle tests.
- MSI installation and upgrade from a disposable 0.7.99 fixture succeeded.
- The running app stopped during upgrade; rule data and startup registration survived.
- Downgrade rejection and uninstall were verified. Uninstall removed startup
  registration and retained saved rules. A clean final installation succeeded.

The upgrade fixture uses the alpha application with an older MSI version; this
tests installer behavior, not migration from an actual historical CBB release.

## UAT focus and limitations

Follow [the UAT checklist](https://github.com/BenRussoUSA/CBB-CloseButtonBlocker/blob/v0.8.0alpha/docs/UAT.md).
Test actual X-button clicks in the applications you use. Custom title bars may
ignore the native menu state; elevated windows may reject changes. Keyboard Close
commands can also be affected. This is not a security boundary.

Exit CBB restores the Close states it changed. Forced termination may leave a
button disabled until its application restarts. Detection runs about every 700 ms.

The alpha is unsigned. Private release updates are checked manually in your
signed-in browser; the application does not store GitHub credentials. Public
update-notification delivery and startup after a real reboot remain UAT items.

The ZIP is an alternative extracted distribution. The MSI is recommended for
installation and registered uninstall. SHA256SUMS.txt identifies the release files.
