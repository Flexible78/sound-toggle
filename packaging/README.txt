SOUND TOGGLE - WINDOWS ENDPOINT AUTOMATION

Extract all three files. Run Install.cmd to install for the current user,
or run SoundToggle.exe directly. Requires the .NET Framework 4.x desktop runtime.
No drivers, services, personal device settings or vendor binaries are bundled.

Settings/logs: %LOCALAPPDATA%\SoundToggle
Install location: %LOCALAPPDATA%\Programs\SoundToggle
Use -Setup for two separate connected playback outputs.
Use -Speakers / -Earphones to select the configured target.
Use -List -NoUI to log active playback outputs without switching.

Specialized analog-jack routing was physically tested only on ASUS TUF Gaming
A17 FA707RM with C-Media CM6549 and the compatible installed vendor panel.
Other shared jacks are not supported by that path. Specific USB-C/Bluetooth
headsets, a second PC and Windows 10 physical operation remain unverified.

Disabled analog Playback controls: one five-second unplug/reinsert recovered
the observed jack-detection incident without reboot. This is not a universal
fix. Normal toggles do not require unplugging. Never disable security protections.

The EXE is unsigned. Audio role changes are sequential, not transactional.
Software state verification does not replace listening confirmation.
Microphone changes are not requested by the app.

Source, license, troubleshooting and evidence:
https://github.com/Flexible78/sound-toggle
