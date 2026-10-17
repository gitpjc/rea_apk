# Bounded PLX 100 Gain and Master — 1.13.0

## Scope at 17 October 2026 — current application 1.41.2

This is a retained early bounded experiment/procedure and its original results, not a current feature limit or instruction to rerun a device command. All 124 requested built-in models now have verified controls; all 80 destinations support general save/rename/Save As, with edited-copy power-cycle persistence and P5.8 workflow evidence. See Current-Status.md, Preset-Save-Rename.md and the category specifications. Earlier fixed 0D baselines, restricted intervals, test counts, next steps and unexecuted procedure checkboxes remain historical; always record a new baseline and preserve every original setting before authorized testing.

See [Current-Status.md](Current-Status.md) and [Documentation-Index.md](Documentation-Index.md) for current support and remaining gates.

## Retained specification and milestone evidence


14 October 2026, F15i V1.1.9 / 0D British Lead Solo. Phase 5 only; Phase 6 remains excluded.

Gain **99–100** and Master **74–75** are now editable alongside Bass **34–35**, Mid **42–43**, Treble **39–40** and Presence **44–45**. All six bounded intervals may coexist. The enabled third-position PLX 100 model and linked flag remain required. Full parameter ranges and other models are not established by these tests.

Authorized ADB operation and iAMP-only phone screenshots verified Gain 100→99→100 and Master 75→74→75 without saving. Captured complete-block opcode 25 commands identify Gain at word 2 and Master at word 7 (zero-based). After phone iAMP was closed, a fresh Windows connection matched the complete original state and detail payloads. The fixture records the captured frames and source hash without device identifiers.

Both desktop editors use the existing serialized fresh-snapshot check, explicit Apply and whole-payload readback verification. Sixty focused protocol cases pass, including exact phone frames, unsupported target/layout/flag rejection, regressions for the shared parameter guard and all 64 bounded six-control source combinations with every unrelated word preserved. Offline UI checks cover valid/invalid input, pending-effect protection, busy state, unsupported firmware and local imports. The full test suite was not rerun.

Actual Windows handlers changed/restored Gain and Master individually, verifying the complete baseline after each. They then changed all six Amp values together and restored them in reverse order. A separate fresh connection verified every original state/detail byte. Private report: analysis/private/amp-gm-windows-report.json (passed=true, baselineRestored=true, failure=null). No saves or preset recalls were performed.

USB stay-awake remains enabled across this ongoing testing/development session, including packaging. Its original value is retained privately for restoration when the entire session ends or the user asks. No computer screenshots or mobile stability checks were taken/requested.

Additional offline geometry checks confirm all six Amp inputs/actions remain within the horizontal viewport and scrollable content at 1920×1080, 1366×768, 911×512, 683×384 and 620×320 logical pixels. No screenshots or hardware commands were used for those checks; physical DPI/Narrator acceptance remains open.
