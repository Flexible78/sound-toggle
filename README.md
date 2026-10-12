# Sound Toggle

[![Windows build and regression tests](https://github.com/Flexible78/sound-toggle/actions/workflows/windows.yml/badge.svg)](https://github.com/Flexible78/sound-toggle/actions/workflows/windows.yml)

A small Windows endpoint-automation utility that turns an investigated audio-routing problem into a repeatable, one-click operation.

**By Alex Tsyrkin · Infrastructure / Systems Engineering**

## The operational problem

Changing the Windows default audio device does not always change where sound physically comes out. On the tested ASUS laptop, built-in speakers and the 3.5 mm jack share one C-Media playback endpoint. Switching endpoint labels alone cannot select the physical destination.

Sound Toggle supports two distinct paths:

- **Separate endpoints:** select one headset/speaker pair in a Windows Forms dialog and switch the default playback device for Console, Multimedia and Communications roles.
- **Supported ASUS C-Media shared jack:** use the installed manufacturer's Playback control to select external earphones or built-in speakers, including speakers while earphones remain plugged in.

This is a focused endpoint-operations project, **not** a universal audio-driver fix or a cloud infrastructure platform.

## Download and run

Get `SoundToggle-Portable.zip` from [Releases](https://github.com/Flexible78/sound-toggle/releases). Extract all files, then run `Install.cmd` for per-user installation. Alternatively, run `SoundToggle.exe` directly.

Installation creates Desktop and Start menu shortcuts and copies the executable to `%LOCALAPPDATA%\Programs\SoundToggle`. No administrator rights are required by this utility. No drivers or services are installed.

The executable needs the .NET Framework 4.x desktop runtime. Development and automated build checks target Windows; Windows 11 is the physically tested environment. Windows 10 compatibility has not been physically verified. The ASUS vendor driver/control panel is additionally required for shared-jack mode and is **not bundled**.

The executable is unsigned. Windows may display a security warning. Do not disable security protections; review the source or build it yourself if required by your environment.

## Usage

```powershell
.\SoundToggle.exe                         # Toggle the configured pair
.\SoundToggle.exe -Speakers               # Select configured speakers
.\SoundToggle.exe -Earphones              # Select configured earphones
.\SoundToggle.exe -Setup                  # Choose two separate outputs
.\SoundToggle.exe --install               # Install for the current user
.\SoundToggle.exe -List -NoUI             # Enumerate playback outputs into the log
.\SoundToggle.exe -List -NoUI --config-dir=C:\Temp\SoundToggle-Test
```

Because this is a GUI-subsystem executable, CLI output is not reliably visible in a terminal. Use `last-run.log` and wait for process completion when checking exit codes. Exit code `0` means the requested operation completed (or setup was cancelled); `1` means an error. `-NoUI` suppresses application dialogs.

Connect USB/USB-C or Bluetooth headsets in Windows before selecting them with `-Setup`. This app does not pair Bluetooth devices or establish wireless connections. Only one pair is configured at a time.

Settings and the latest operation log live in `%LOCALAPPDATA%\SoundToggle`; `--config-dir=<directory>` isolates both for tests. Do not copy device IDs from another PC.

## Safety and verification

- Refuses missing, disabled or unexpected vendor controls instead of cosmetically changing a dropdown and reporting success.
- Checks that Playback selection is retained and Recording selection has not changed during the vendor routing step.
- Verifies all three default playback roles using consecutive samples.
- Allows an FxSound default in shared-jack mode only when FxSound is running and its configured output uniquely matches the intended physical device. Separate-endpoint mode does not accept that redirect.
- Uses a named mutex to serialize switching. Does not stop audio applications, reboot Windows, reset devices or install drivers.
- Does not request microphone changes. Hardware regression coverage and prior manual checks are documented separately from general implementation claims.

**Limits:** role updates are sequential and are not transactional; a later failure does not guarantee rollback of earlier changes. Verification reads Windows/vendor state, not acoustic output. Applications with their own output selection may need separate configuration. See [architecture](docs/ARCHITECTURE.md) and [testing](docs/TESTING.md).

## Hardware compatibility

| Path | Evidence | Limits |
| --- | --- | --- |
| ASUS TUF Gaming A17 FA707RM, C-Media CM6549, onboard `VID_0B05&PID_6210&MI_00` | Prior local physical-routing tests and user listening confirmation | Requires compatible installed ASUS panel, exact Playback labels and enabled controls; not a general C-Media guarantee |
| Two distinct Windows playback endpoints | Implemented and regression-tested verification logic | Specific USB-C/Bluetooth headsets and a second PC have not been physically tested |
| Other shared 3.5 mm jacks | Unsupported by the specialized path | A generic endpoint switch cannot override hardware jack muting |

If the supported analog Playback control is disabled despite connected earphones, a one-time five-second unplug/reinsert recovered jack detection in the observed incident without a reboot. This is an incident-specific recovery, not an established universal root cause. Normal subsequent toggles do not require unplugging. See the [troubleshooting runbook](docs/TROUBLESHOOTING.md).

## Build and test

Windows PowerShell and the Windows .NET Framework C# compiler are sufficient; no NuGet packages are required.

```powershell
powershell -NoProfile -File .\scripts\Build.ps1
powershell -NoProfile -File .\scripts\Test.ps1
```

Outputs are written to `dist/`. Tests use synthetic native windows and isolated temporary configuration; they do not switch your real audio route or run the installer. GitHub Actions runs the same build/test steps on a Windows runner and retains the ZIP and checksums as artifacts.

## Repository layout

```text
src/SoundToggle.cs          Existing application implementation
tests/VerificationTests.cs Native-control and endpoint-verification regression tests
scripts/                   Build/package and non-switching smoke checks
packaging/                 Installer launcher and user instructions
docs/                     Architecture, evidence and operational runbook
```

Local device configuration, logs, diagnostics, vendor binaries, shortcuts and personal documents are deliberately excluded. The original working installation is maintained separately from this publication tree.

## Why this belongs in an infrastructure portfolio

The value is the engineering method: distinguish logical state from physical behavior, isolate a vendor-specific dependency, automate a recurring support task, reject ambiguous states, document recovery boundaries, and ship a tested per-user package. It complements larger AWS/CI/CD infrastructure projects rather than replacing them.

AI-assisted development was used with human review and hardware validation. This repository preserves the existing implementation rather than presenting it as an independently audited or enterprise-grade audio stack.

## Uninstall

Remove the installed SoundToggle folder and its Desktop/Start menu shortcuts. Optionally remove `%LOCALAPPDATA%\SoundToggle` to delete settings/logs. No service or driver cleanup is required.

## Author and license

Alex Tsyrkin — [GitHub](https://github.com/Flexible78) · [LinkedIn](https://www.linkedin.com/in/alexandertsyrkin/)

MIT license. Third-party Windows/vendor components are not included or covered by this license.
