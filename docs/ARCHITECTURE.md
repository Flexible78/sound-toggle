# Architecture and design boundaries

## Components

`Audio` wraps Windows Core Audio COM endpoint enumeration and default-device selection. `IPolicyConfig` is an undocumented Windows interface: behavior may change between Windows versions. `CMediaRouting` wraps Win32 window/control messages for a narrowly identified vendor panel. `Program` handles configuration, device selection, routing, verification, logging, serialization through a mutex and per-user installation.

```text
User activation -> named mutex -> enumerate active playback endpoints
                               -> load local pair / hardware detection / setup
                               -> shared jack: verify vendor controls, set Playback
                               -> distinct endpoints: resolve connected target
                               -> update three Windows playback roles
                               -> verify consecutive role samples and vendor state
                               -> operation log / exit status
```

Shared-jack detection requires the specific USB hardware registry key and one C-Media endpoint. Playback labels must match exactly. Localization or a panel update can therefore make routing unsupported; the app refuses unexpected controls.

The Recording selection is checked before/after the vendor route operation. The implementation contains no intended recording-route writes or capture-default updates. This is not a guarantee against external applications changing audio state concurrently.

## Verification versus reality

Windows endpoint state and the vendor Playback dropdown are necessary software checks, not proof of the audible destination. The originating machine had a separate human listening confirmation. Automated CI cannot establish physical routing on arbitrary audio hardware.

FxSound is accepted only for the shared-jack path when its running process and XML output-device setting identify exactly one expected physical endpoint. XML external resolution is disabled. This is a configuration check, not independent measurement of the enhancer's audio stream.

## Known technical limitations

- The source is an intentionally retained single-file implementation, not a redesigned engine.
- COM interop and vendor UI automation are platform-specific and need additional compatibility testing.
- Role updates are sequential; there is no complete transactional rollback after partial failure.
- The vendor panel is discovered/launched through its installed package identity; it is not bundled.
- Success samples can still be invalidated later by another application.
- Settings are one device pair per user; no hotplug daemon or multi-profile switching is implemented.
- The current error path logs messages rather than structured diagnostic events; logs can contain local endpoint IDs.
- The executable is unsigned; no claim of enterprise deployment readiness is made.

Future work should prioritize real second-machine tests, rollback design, broader test isolation and signing before promising wider hardware support.
