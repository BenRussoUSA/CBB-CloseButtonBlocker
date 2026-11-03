# 0.8.1alpha UAT checklist

Use a disposable document or test window first. Save work before testing closing.

1. Install the MSI as your normal user. Verify one CBB entry in Installed apps
   and a Start-menu shortcut. Launch it. No separate .NET installation is needed.
2. Find the black tray icon with a light-gray X and red prohibition sign.
   Left-click it and confirm the indented window list appears under
   "List of windows   (right click one to change close button state)".
   Green filled circles mean ON; CBB logos mean OFF. Unsupported windows
   use a gray outline circle. Check the author link at the bottom right.
3. Right-click a test window. Close Button ON should be bold with a diamond.
   Choose OFF; attempt the window's upper-right X. Record whether it is blocked.
   Choose ON and confirm normal behavior returns.
4. Test at least Notepad, File Explorer, and VS Code. Test other applications you
   actually use. Record custom-title-bar failures rather than assuming support.
5. Add an exact-title rule. Confirm a matching open window is blocked after saving.
   Edit it, then remove it. Removing a rule does not change an already blocked window.
6. Add regex `(?i).*notepad.*`. Use Help and the sample-title test. Try an invalid
   pattern `[` and confirm Save refuses it. Confirm settings survive closing CBB.
7. With a matching window, select ON manually. Wait several scans and change the
   title by editing a disposable document. It must remain ON. Close and reopen the
   application; the new matching window must be OFF.
8. Right-click the tray icon. Verify About, Check for updates, and Exit CBB appear
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
12. Open Uninstall. Feedback should be the first option. Cancel must leave CBB
    installed. Uninstall without feedback must work. Verify the startup entry and
    program files are removed; saved settings intentionally remain.

Report for each application: version, standard/custom title bar, elevated or not,
result of X click, result of Alt+F4, restoration on Exit CBB, and any error text.

Not yet a universal guarantee: native-menu state tests cannot prove that every
application's custom X button honors Windows' disabled Close state.
