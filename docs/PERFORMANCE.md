# Resource measurements for 0.9.4beta

Measurements were taken on November 5, 2026 on one Windows 11 x64 desktop.
Memory and CPU results vary with application activity and Windows' memory
management. They are observations from this test, not minimum requirements.

## Distribution and installed files

| Measure | 0.9.0beta | 0.9.4beta |
| --- | ---: | ---: |
| MSI | 39,904,144 bytes (38.06 MiB) | About 152 KiB |
| ZIP | 49,999,725 bytes (47.68 MiB) | About 120 KiB |
| Application folder, including source | 123,372,937 bytes (117.66 MiB) | About 300 KiB |
| Bundled .NET runtime | Included | None |

Sizes count file lengths, not filesystem allocation units. The application-folder
measurement excludes user settings and Windows Installer's cache. It also does
not attribute part of Windows' existing shared Framework installation to CBB.
The executable loads Framework libraries from Windows; those libraries were not
removed from the host or moved into CBB's folder.

## Process memory and CPU

Both versions were freshly launched with the same saved settings. The tray popup
remained closed. After a 20-second warmup, the test sampled process counters once
per second for approximately 30 seconds. This was an otherwise mostly idle
working desktop, not an isolated laboratory workload. The script stopped each
version cleanly and verified that settings and startup registration were unchanged.

| Measure | 0.9.0beta | 0.9.4beta |
| --- | ---: | ---: |
| Actual sample interval | 30.420 seconds | 30.432 seconds |
| Mean working set | 66.73 MiB | 47.18 MiB |
| Working-set range | 66.05-67.86 MiB | 47.16-47.20 MiB |
| Mean private committed memory | 17.08 MiB | 26.92 MiB |
| Process CPU time accumulated | 171.875 ms | 0 ms measured |
| Average CPU, relative to one logical processor | 0.565% | 0% measured |

Working set measures pages resident in physical memory, including shared pages.
Private bytes measure committed private memory and are not the same as resident
RAM. The working set decreased by about 29%, while private committed memory
increased by about 58%. A broad claim that all memory usage decreased would be
incorrect. Microsoft defines these [process counters](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.process.workingset64)
and [private memory](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.process.privatememorysize64).

CPU percentages in this table are relative to one logical processor. Task
Manager's total-machine percentage uses a different denominator. Zero measured
CPU time is limited by the counter resolution and the short sampling interval.
Window creation or title changes still require processing, as do update checks.

An earlier development build subscribed to a broader Windows event range and
recorded higher CPU use. The tested build narrows subscriptions to the event
categories CBB needs. This report uses the subsequent sample; it does not claim
that event-driven designs are automatically more efficient for every workload.

## Reproducing the comparison

`tests/Measure-Resources.ps1` accepts the old and new executable paths. It
requires permission to stop and restart CBB, restores the previously installed
version afterwards, and writes JSON counters under `artifacts`. It does not
record window titles or rule contents. Keep the desktop workload comparable
between runs and repeat measurements if making performance guarantees.

The automated lifecycle tests separately verify that title changes and window
creation are handled without a full scan. They also verify that pumping the
message loop while idle does not trigger recurring full scans. Startup and
explicit refreshes still enumerate top-level windows. Rare event-queue overflow
also requests a recovery snapshot; it does not enable periodic polling.

A separate snapshot of the installed 0.9.4beta process showed 10.27 MiB private
working set, 49.80 MiB total working set, and 28.16 MiB private committed memory.
This was not part of the paired 30-second comparison above. It distinguishes
private resident RAM from the larger working set, which includes shareable pages.
