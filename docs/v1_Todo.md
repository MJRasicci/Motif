# Motif v1 Todo

## Snapshot

- `Motif.Core` is now the format-agnostic, mutable score model.
- `Motif.Extensions.GuitarPro` owns the GPIF/raw/archive/XML read-write pipeline.
- Core exposes `IScoreReader` / `IScoreWriter`; the GP package implements them.
- Core model data is extensible, JSON-serializable, and now owns navigation via `ScoreNavigation`.
- The CLI works against the package split and routes by inferred/explicit formats instead of GP-only modes.
- Core and GP test suites are split and currently passing in targeted verification.

## v1 Goals

- Ship `Motif.Core` as the stable edit-first music domain model.
- Ship `Motif.Extensions.GuitarPro` as the v1 format package with strong round-trip fidelity.
- Finalize Core vs Guitar Pro boundaries, public API shape, packaging, and docs.

## Non-Goals

- Playback engine
- Notation engraving/rendering engine
- Editor UI framework
- DAW abstraction layer
- Universal normalization across every music format
- Shipping multiple fully-supported formats in v1

## Architecture Constraints

- Core is mutable and optimized for direct programmatic editing.
- Core musical data is authoritative; extension data is supplemental fidelity state.
- Core plus extensions is the highest-fidelity Motif representation; cross-format export may lose unsupported format-specific detail, but musical output must remain valid.
- Import flow: deserialize source -> map Core -> attach extension fidelity.
- Export flow: preserve usable extension data -> infer from Core/defaults -> emit diagnostics when fidelity degrades.
- Fidelity loss emits diagnostics; musically invalid output is a hard failure.
- Traversal/cache/navigation state is derived state and must be recomputed after traversal-affecting edits.
- Target hierarchy remains `Score -> Track[] -> Staff[] -> StaffMeasure[] -> Voice[] -> Beat[] -> Note[]`.
- Timeline-global measure state belongs on a score-owned timeline abstraction rather than staff-local bars.
- The library does not preserve a compatibility `Track.Measures` + `Measure.AdditionalStaffBars` shim; the staff-first hierarchy is authoritative.
- Core will not add new format-agnostic stable node IDs for v1; structural position plus extension/source IDs are sufficient.
- Raw format property bags / XProperty ids do not belong in the long-term Core surface; keep typed semantics in Core and move format fidelity behind extensions.
- Keep genuinely cross-format musical semantics in Core even when a source format models them poorly or app-specifically.

## Step Status

- `[x]` Step 1 - Extensibility contracts in Core
  Landed: `IModelExtension` / `IExtensibleModel`, typed helpers, GP extension attachments, Core-only JSON.

- `[~]` Step 2 - Split Core domain vs Guitar Pro fidelity
  Landed: score-owned timeline ownership via `Score.TimelineBars`; navigation and GP master-bar export now consume that score-level timeline instead of implicitly depending on the first populated track; GP master-bar fidelity metadata now attaches to `TimelineBarModel`; the compatibility `Track.Measures` + `Measure.AdditionalStaffBars` shape is gone; mapper, unmapper, GP fidelity diagnostics, reattachment, and hierarchy tests now operate on `Track.Staves` / `StaffMeasure` only.
  Remaining: move surviving Core property/XProperty bags behind GP extensions or normalize them as typed semantics, remove the `*Model` suffix from Core types, keep genuinely cross-format semantics such as golpe in Core.

- `[x]` Step 3 - Raw cache invariants
  Landed: explicit GP cache workflow via `InvalidateGuitarProExtensions`, reattachment result reporting from `ReattachGuitarProExtensionsFrom`, unmapper diagnostics for invalidated source fidelity, partial source reattachment, and mixed attached/missing GP fidelity within a source-derived score tree, plus specific regeneration warnings for track staves XML, note string/fret payloads, note pitch spellings, and source rhythm shapes, with workflow guidance in `docs/LIBRARY_WORKFLOW.md`.

- `[x]` Step 4 - Guitar Pro format I/O ownership
  Landed: GPIF/raw/archive/XML/mapper/unmapper live in the GP package; low-level GP seams are internal.

- `[x]` Step 5 - Format-agnostic reader/writer contracts
  Landed: `IScoreReader` / `IScoreWriter` live in Core and are implemented by `GuitarProReader` / `GuitarProWriter`.

- `[x]` Step 6 - Navigation in Core
  Landed: `ScoreNavigation`, `Score.Anacrusis`, explicit `HasCurrentPlaybackSequence` / `InvalidatePlaybackSequence` / `EnsurePlaybackSequence` workflow, `Score.TimelineBars` as the only canonical timeline source, and Core navigation tests for staff-first timeline playback and invalidation.

- `[~]` Step 7 - CLI
  Landed: package-split CLI, source-extension reattachment for no-op JSON writes, writer-diagnostic surfacing for partial reattachment, format-pair routing, legacy flag compatibility, GPIF batch export.
  Remaining: revisit routing once non-Guitar-Pro inputs/outputs exist.

- `[x]` Step 8 - Tests
  Landed: Core/GP test split, API surface tests, Core navigation coverage, explicit navigation invalidation tests, GP extension invalidation/reattachment tests, writer coverage for specific regeneration diagnostics plus score timeline master-bar export, and hierarchy coverage for staff-first mapping/unmap, staff-level fidelity reattachment, JSON round-trips, and empty-slot synthesis for sparse staff trees.

- `[ ]` Step 9 - Public API review
  Remaining: rename Core `*Model` types, trim remaining GP leakage from `Motif.Core`, remove Core property/XProperty bags that are only preserving GP fidelity, rerun API surface tests after each cleanup pass.

- `[ ]` Step 10 - Packaging and release prep
  Remaining: package metadata, Source Link/symbols/license/readme metadata, dependency validation, release builds, final docs verification, ship `Motif.Core` + `Motif.Extensions.GuitarPro` + `Motif` as the v1 package set.

## Next Up

1. Run the public API cleanup pass.
   - Rename Core `*Model` types
   - Move measure/bar/beat/note property/XProperty bags out of Core unless they become typed cross-format semantics
   - Audit remaining GP-shaped Core properties such as `BeatModel.Wah`, `BeatModel.VibratoWithTremBarStrength`, and `NoteArticulationModel.AntiAccentValue`
   - Keep cross-format semantics such as golpe in Core while remapping GP-specific representations behind the GP package

2. Finish packaging and release docs.
   - NuGet metadata, Source Link, symbols, readme/license metadata
   - Final package/dependency review for `Motif.Core`, `Motif.Extensions.GuitarPro`, and the `Motif` metapackage
   - Release builds and docs verification

## Release Gate

- Core hierarchy and derived-state policy are finalized.
- Remaining GP-shaped Core/API seams are intentionally resolved.
- Public API review, packaging, docs, and release builds are complete.
- Tests match the finalized architecture and pass.
