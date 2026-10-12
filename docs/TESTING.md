# Test coverage and evidence

## Automated checks

`VerificationTests` creates hidden synthetic Win32 controls, not the actual vendor panel. Existing regression assertions cover disabled Playback controls (including an already-selected requested route), unchanged synthetic selection, direct endpoint matching, correctly mapped FxSound, prohibited redirects in separate-endpoint mode, stopped/misconfigured enhancers, unrelated outputs and ambiguous names.

`Test.ps1` compiles and runs these fixtures, then runs the built executable with `-List -NoUI` and an invalid argument using separate temporary configuration folders. It checks process exit codes, expected log messages, absence of saved device-pair configuration, and equality of all playback/capture default role IDs before and after. It does not issue switching commands or install the application. The release ZIP is checked for its exact three-file allowlist and matching EXE hash.

The role snapshots are observations: concurrent external audio changes can fail the check and are not silently ignored. A headless runner can have no default audio endpoint; only the specific Windows `E_NOTFOUND` result is recorded as an absent-endpoint marker and compared before/after. Other COM errors still fail the check. An unchanged absent endpoint is not evidence of hardware routing.

## Historical hardware evidence (2026-10-10 / 2026-10-11)

The originating ASUS TUF Gaming A17 FA707RM with onboard C-Media CM6549 was checked with forced speakers, forced earphones and repeated ordinary toggles while the analog earphones remained connected. Recorded checks verified Playback state, unchanged Recording selection and all microphone defaults. FxSound stayed running in the later checks. The user provided a listening confirmation in that prior session.

A disabled-control incident recovered after one physical jack reinsertion without a Windows reboot. This establishes an observed recovery, not a proven root cause. Historical evidence is not a new hardware test performed during publication. Raw machine logs and endpoint IDs are intentionally not published.

## Not verified

- A second-PC install and physical speaker/headset routing.
- Specific USB-C or Bluetooth headset models.
- Windows 10 physical operation.
- Other shared-jack chipsets, translated vendor panels or future driver versions.
- Acoustic correctness in CI, transactional failure rollback, code signing or enterprise deployment policies.

Passing build and fixture checks must not be described as universal hardware validation.
