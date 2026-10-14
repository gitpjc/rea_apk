# Mobile transfer-screen loop: recovered, cause unresolved

14 October 2026. After the approved Windows save test and publication, the user reported that iAMP repeatedly alternated between its home/connected-device screen and a PC/USB illustration. Restarting iAMP alone did not resolve it. No preset edits, saves or amplifier commands were sent during this diagnosis; Windows BLE connections were deliberately avoided after the recovery restart.

## Observed evidence

- Phone PID 8559 reported successful BLE connection, normal application mode / firmware V1.1.9 and complete preset loading. It then pushed/popped `/f15Transfer` about every five seconds, without a corresponding BLE disconnect loop.
- Read-only process inventory found no running iAMP Desktop, DeviceProbe or MOOER Studio process at that check.
- Passive `phone-screen-loop-capture.json` contains 124 incoming `9B 00` and 114 incoming `9B 01` frames. In inspected overlapping timestamps, `9B 00` closely precedes transfer-route opening and `9B 01` precedes closing. This is a bounded runtime association, not a recovered firmware/API specification.
- The first retained `9B` was at record 7185, phone HCI-clock label 12:26:49.231, approximately 46.536 seconds after connection event 6849. It predates PID 8559's restart log; the seven earlier inspected phone captures contain no application `9B` frames. These clocks are not independently calibrated UTC.
- The user unplugged the amp USB cable but reported continued switching; Windows USB metadata confirmed no attached target interface (`devices: []`). This did not establish the cable as the cause.
- The phone app was closed, the user normally switched the amp off/on with USB unplugged, and iAMP was reopened. The user reported a stable effects screen. PID 11720 logged `/f15Home` at 12:47:31.059, slot 4 / British Lead Solo, Threshold 25 / Level 50.
- Post-restart passive capture's final amplifier connection event 9649 contains 39 application frames and **zero `9B` frames**. Detail records 9928–9930 and the complete state match the original baseline byte for byte. Private `phone-loop-recovery-verification.json` records both comparisons as true; detail SHA-256 is `644dee6d3ad07f3e5c2f6ed7670e37220b28d71892b2605763c820b5e19c30ce`.

## Recovery and remaining investigation

Follow-up: [Read-Only-Handoff.md](Read-Only-Handoff.md) records three clean read-only Windows→phone comparisons and stable USB attachment. The original loop was not reproduced. Defensive patch 1.2.1 now refuses further commands in a Windows session after the observed transfer-state discriminator, tested offline with captured frames. The trigger and root-cause fix remain unresolved.

The observed recovery was: close phone iAMP, restart the amplifier with USB unplugged, then reopen/reconnect iAMP. The phone can remain USB-connected to the PC for diagnostics. No factory reset, firmware operation, preset edit or save was needed. Original settings survived this normal restart in this one incident; that does not establish a general power-cycle acceptance matrix.

The loop occurred after Windows testing, so a Windows-to-phone handoff interaction remains a candidate requiring investigation. Timing alone does not prove the Windows save request caused it. Amplifier transfer/connection state, mobile status handling and USB interactions remain possible factors. No application fix or root-cause resolution is claimed.

Before further persistent-write expansion, investigate read-only Windows connect/dispose → phone reconnect versus save/recall → phone reconnect, with explicit baselines, `9B` monitoring and the known recovery available. Validate mobile usability after session closure, rather than relying on matching preset bytes alone. Avoid intentionally reintroducing the recovered loop during routine use.

Private supporting files are the `phone-screen-loop-*`, `phone-loop-usb-unplugged-*`, `loop-usb-presence.json`, `phone-loop-recovered-*` and `phone-loop-recovery-verification.json` artifacts. Raw device identifiers and captures remain excluded from Git.
