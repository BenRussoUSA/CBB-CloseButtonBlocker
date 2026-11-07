# Application compatibility

CBB changes the standard Windows system-menu Close command using SC_CLOSE. A
window can report that command as disabled while its custom-drawn Close button
still closes it. The toggle reflects the native command state, not a promise
that every closing path in an application has been intercepted.

## Verified in the 0.9.4 test harness

The separate-process Windows Forms fixture verifies automatic blocking and
restoration of the native Close command. Event notifications update its title
and track its lifetime, including hiding and reopening the window. Tests also
verify that mouse or keyboard operation of CBB's toggle changes that native
state. A fixture with no standard Close command is read-only in CBB.

These checks do not certify actual title-bar clicks in every application.

| Application type | 0.9.4 verification status |
| --- | --- |
| Standard Windows Forms fixture | Native menu state and CBB toggle input verified |
| Notepad | Actual title-bar click requires UAT |
| Microsoft Office | Actual title-bar click requires UAT |
| Chromium browsers or Electron apps | Custom title bars require UAT per application |
| Elevated or protected application | Changes may be rejected; CBB does not elevate |

For a compatibility report, use an empty disposable document. Record the
application version and Windows version, then enable Block Close Button and
try the actual title-bar X. Check that turning the toggle Off restores normal
closing. Also check behavior after reopening the application. Do not test with
unsaved work that matters.

Use [GitHub Issues](https://github.com/BenRussoUSA/CBB-CloseButtonBlocker/issues)
to report results. Remove private document names from screenshots before posting.
