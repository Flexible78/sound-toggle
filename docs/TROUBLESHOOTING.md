# Troubleshooting runbook

1. Confirm whether the headset is a shared analog jack or a separate Windows playback endpoint. Do not infer device type from connector shape or an internal USB bus.
2. For separate endpoints, connect the headset in Windows and run `-Setup`. Select two distinct physical outputs. Bluetooth pairing/connection is outside this app.
3. For supported shared-jack hardware, check the manufacturer's driver/panel and the enabled Playback control. Unexpected labels are deliberately refused.
4. If connected analog earphones produce the specific disabled-control error, remove the plug for five seconds and reinsert it fully once. Retry only after the control becomes enabled. This recovered one observed incident; it may not recover yours.
5. Inspect `%LOCALAPPDATA%\SoundToggle\last-run.log`. Device IDs are local identifiers; redact them before sharing. `-List -NoUI --config-dir=<temporary folder>` provides an isolated inventory log without switching.
6. If FxSound is active in shared-jack mode, check its configured physical output. Duplicate names or a different output are rejected. Separate-endpoint mode requires the requested endpoint itself.
7. Verify the audible destination manually at a safe volume. Software success alone is insufficient.

Do not automatically restart Windows, reset USB devices, stop audio applications or replace drivers as part of routine troubleshooting. Disruptive recovery needs a separately reviewed, hardware-specific plan.
