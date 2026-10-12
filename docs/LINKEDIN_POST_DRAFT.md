# LinkedIn post draft — not published

A small Windows audio problem reminded me of an important infrastructure lesson: a successful configuration change is not always a successful operational outcome.

On my laptop, the built-in speakers and 3.5 mm headset share one playback endpoint. Changing the Windows default-device label did not change the physical destination.

I turned the investigation into Sound Toggle, a small Windows utility with a hardware-specific routing path, guarded controls, playback-role verification, isolated regression tests and a portable per-user package.

The useful part was not the size of the application. It was the process: identify the real dependency, distinguish logical state from physical behavior, refuse ambiguous conditions, verify the outcome and document the recovery boundaries.

The shared-jack path was physically checked on my ASUS C-Media laptop. I am not claiming universal hardware support; USB-C/Bluetooth and second-PC testing still need real device validation.

Built with AI-assisted development, human technical review and hands-on testing.

Source and operational notes: https://github.com/Flexible78/sound-toggle

#InfrastructureEngineering #Windows #Automation #Troubleshooting #SystemsEngineering
