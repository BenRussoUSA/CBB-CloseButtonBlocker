# CBB-CloseButtonBlocker 0.9.4beta

This release focuses on making CBB smaller and reducing the work it does in the
background. The installer no longer carries a separate .NET runtime, and window
detection now uses notifications from Windows instead of a scan every 700 ms.
You can keep using the tray icon and your saved rules as before.

[Download the Windows installer (.MSI)](https://github.com/BenRussoUSA/CBB-CloseButtonBlocker/releases/download/v0.9.4beta/CBB-CloseButtonBlocker-0.9.4beta-win-x64.msi)

## A much smaller download

The MSI is about 152 KiB, down from 38.06 MiB in 0.9.0beta. That is a reduction
of more than 99%, with the complete CBB source still included. Installed files
occupy about 300 KiB instead of 117.66 MiB. Windows keeps its own installer cache,
and your settings remain in their separate folder.

CBB now uses .NET Framework 4.8.1 and Windows Forms supplied with Windows 11
version 22H2 and later. On those systems, you can copy the MSI to a USB drive
and install offline without downloading another runtime. The installer checks
that the required Framework is present before continuing.

If you are still using the original Windows 11 release, update Windows first.
That release included Framework 4.8. Microsoft's
[version table](https://learn.microsoft.com/en-us/dotnet/framework/get-started/system-requirements)
explains which Framework is included with each Windows release. Windows updates
also provide Framework servicing.

## Less background work

Rather than checking the whole desktop every 700 ms, CBB responds when Windows
reports a relevant change. Bursts of notifications are combined before affected
windows are updated. CBB still takes a snapshot at startup, and refreshes when
you open the window table or save rules. The daily GitHub update check runs
separately from window detection.

In a 30-second desktop sample, 0.9.4 recorded no measurable process CPU time,
compared with about 172 ms for 0.9.0beta. This is a short observation from one
computer, not a promise of zero CPU use. Opening windows or checking for updates
still requires processing.

Memory needs a little more explanation. In that comparison, the average working
set fell from 66.7 MiB to 47.2 MiB. This includes shared or shareable Windows and
Framework pages. Private committed memory increased from 17.1 MiB to 26.9 MiB,
so not every memory counter went down. A separate snapshot of the installed
build showed about 10.3 MiB of private resident RAM. These counters measure
different things; the
[measurement notes](https://github.com/BenRussoUSA/CBB-CloseButtonBlocker/blob/v0.9.4beta/docs/PERFORMANCE.md)
explain the comparison and its limits.

## Easier ways to find and protect a window

The toggle column now says Block Close Button, which better describes what CBB
does. The X may remain visible even though its standard Windows Close command
is disabled.

You can search by part of a window title, or choose Show blocked windows only.
To create a rule for a window already in the list, right-click its title. The
editor starts with that title and shows matching open windows before you save.
The README and rule help also have fuller explanations to make getting started
easier.

A few safeguards help keep settings responsive. You can save up to 100 rules,
with up to 2,048 characters per pattern. If a regular expression exceeds its
50 ms matching limit, CBB pauses it and displays a notification. Editing the
rule or restarting CBB lets it run again. Settings files and update responses
also have size limits.

## Updating your copy

Download and run the MSI, then open CBB from Start after installation. Your saved
rules and startup preference are retained. The upgrade also removes the old
bundled runtime files. If you decide to uninstall, use Windows Settings under
Apps and Installed apps; your rules are kept for a future reinstall.

The local build passed 57 automated checks. The upgrade from 0.9.0beta preserved
settings and startup registration, and the older installer correctly refused a
downgrade. The repository also includes an automated build and security workflow.

This is still an unsigned beta, so Windows may display an unknown-publisher
warning. Optional Microsoft signing support is prepared but has not been
activated. The
[signing notes](https://github.com/BenRussoUSA/CBB-CloseButtonBlocker/blob/v0.9.4beta/docs/SIGNING.md)
explain what that involves.

## Trying it with your applications

Please start with an empty document or another window you can safely close.
Some applications use custom title bars that do not honor the standard Windows
Close command. Newly opened windows also take a brief moment to be detected.
The
[compatibility notes](https://github.com/BenRussoUSA/CBB-CloseButtonBlocker/blob/v0.9.4beta/docs/COMPATIBILITY.md)
and [testing checklist](https://github.com/BenRussoUSA/CBB-CloseButtonBlocker/blob/v0.9.4beta/docs/UAT.md)
explain what to check.

If something does not work as expected, an
[issue report](https://github.com/BenRussoUSA/CBB-CloseButtonBlocker/issues)
with the application name and version helps make the next release more useful.
General questions and suggestions can go in
[Discussions](https://github.com/BenRussoUSA/CBB-CloseButtonBlocker/discussions).

[README](https://github.com/BenRussoUSA/CBB-CloseButtonBlocker#readme) |
[License](https://github.com/BenRussoUSA/CBB-CloseButtonBlocker/blob/v0.9.4beta/LICENSE.txt) |
[Contributing](https://github.com/BenRussoUSA/CBB-CloseButtonBlocker/blob/v0.9.4beta/CONTRIBUTING.md)
