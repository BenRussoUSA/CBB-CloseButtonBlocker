# 0.8.3 UAT checklist

Use a disposable document or test window first. Save work before testing closing.

1. Install the MSI as your normal user. Verify one CBB entry in Installed apps
   and a Start-menu shortcut. Launch it. No separate .NET installation is needed.
2. Find the black tray icon with a light-gray X and red prohibition sign.
   Left-click it and confirm a grid appears with a narrow "Hide Close Button"
   column on the left and a wide "Window Title" column on the right.
   Check the author link at the bottom right.
   Settings and Uninstall must not appear in this window.
3. Click a test window's toggle to turn it On; attempt the window's upper-right X.
   Record whether it is blocked. Turn the toggle Off and confirm normal closing
   returns. Focus the toggle with the keyboard and press Space to change it.
   Unsupported windows should show N/A and reject toggling. Resize the table and
   check long titles through their tooltips. Verify readability at your display scale.
4. Test at least Notepad, File Explorer, and VS Code. Test other applications you
   actually use. Record custom-title-bar failures rather than assuming support.
5. Close the list and right-click the tray icon. Choose Settings and confirm it
   opens without the list window. Add an exact-title rule. Confirm a matching
   open window is blocked after saving.
   Edit it, then remove it. Removing a rule does not change an already blocked window.
6. Add regex `(?i).*notepad.*`. Use Help and the sample-title test. Try an invalid
   pattern `[` and confirm Save refuses it. Confirm settings survive closing CBB.
7. With a matching window, turn the toggle Off manually. Wait several scans and
   change the title by editing a disposable document. It must remain Off. Close
   and reopen the application; the new matching window's toggle must be On.
8. Right-click the tray icon. Verify Settings appears first, with About and Check
   for updates below it. Exit CBB must appear at the bottom; Uninstall must not appear.
   The right-click menu should appear
   without opening a new window list. Exit CBB. Its blocked windows must return to their original Close state. Restart
   CBB and confirm saved rules apply again to existing windows.
9. Enable Launch at startup at the top of Settings. Confirm it is no longer shown
   on the window-list popup.
   Sign out/in or reboot when convenient. CBB should run and apply saved rules.
   Disable startup and verify it stays disabled after another sign-in.
10. Check for updates. The private alpha should explain that browser sign-in is
    needed, without asking you to paste a token. Public notification delivery will
    need testing after a release is accessible through the public API.
11. Before a future upgrade, save several rules and enable startup. Run the newer
    MSI. Verify one Installed apps entry, retained rules, and startup preference.
    Try an older MSI and confirm the downgrade is refused.
12. In Windows Settings > Apps > Installed apps, find CBB-CloseButtonBlocker
    and select Uninstall. Verify the startup entry and program files are removed;
    saved settings intentionally remain. No CBB feedback dialog should appear.

Report for each application: version, standard/custom title bar, elevated or not,
result of X click, result of Alt+F4, restoration on Exit CBB, and any error text.

Not yet a universal guarantee: native-menu state tests cannot prove that every
application's custom X button honors Windows' disabled Close state.
