# Complete Before v1 Release

Motif v1 is now primarily an architecture-completion pass, not a proof-of-concept pass. Guitar Pro read/write fidelity works today. The remaining work is to finish the Core vs extension boundary, remove the last GP-shaped seams from the public API, and ship clean packages/docs.

## Current Verified State

- Guitar Pro read/write pipeline lives in `Motif.Extensions.GuitarPro`
- Typed extension contracts live in `Motif.Core`
- Core domain model is mutable and JSON-serializable
- Most Guitar Pro fidelity/cache state has moved behind GP extensions
- CLI works against `Motif.Core` + `Motif.Extensions.GuitarPro`
- Test split is in place and `dotnet test --no-restore` last passed with `133` succeeded / `0` failed

## v1 Goals

- Ship `Motif.Core` as a format-agnostic, mutation-friendly music domain model
- Ship `Motif.Extensions.GuitarPro` as the v1 format package with strong round-trip fidelity
- Keep Core expressive enough for future format packages without coupling it to GPIF/Guitar Pro
- Publish clean package metadata, docs, and API boundaries

## v1 Non-Goals

- Playback engine
- Notation engraving/rendering engine
- Editor UI framework
- DAW abstraction layer
- Universal normalization across every music format
- Shipping multiple fully-supported formats in v1

## Housekeeping

### Done

- Rename from `GPIO.NET` to `Motif*` across solution/projects/namespaces/docs in active code paths
- Split GP read/write code and GP tests out of Core
- Rename the root domain type from `GuitarProScore` to `Score`
- Reorganize `Motif.Core` so abstractions/extensions/serialization are not sitting in the project root

### Remaining

- Add real NuGet metadata/package IDs/readme/license/source link details to shipping projects
- Review README/examples/badges/package descriptions against the new architecture
- Decide whether legacy internal/public type names like `TrackModel` / `MeasureModel` / `BeatModel` are acceptable for v1 or should be renamed before freeze

# Step 0 - Core Architecture Baseline

These decisions are approved and should now be treated as constraints for implementation.

## Decisions

- Core is mutable by design and optimized for direct programmatic editing
- Core musical data is authoritative; extension data is supplemental fidelity state
- Import flow: deserialize source -> map Core -> attach extension fidelity
- Export flow: preserve usable extension data -> infer from Core -> infer from other extensions -> apply defaults -> emit diagnostics when fidelity degrades
- Extension payloads are preserved best-effort, but Motif does not guarantee validation/preservation of arbitrary user edits inside extension data
- Exact source-node identity is secondary to musical meaning, compatibility, and reasonable output size
- Traversal/cache/navigation state is derived state and must be recomputed after traversal-affecting edits
- Rhythm extensibility is folded into `Beat`
- Target Core hierarchy remains `Score -> Track[] -> Staff[] -> StaffMeasure[] -> Voice[] -> Beat[] -> Note[]`
- Timeline-global measure state belongs on `Score.MeasurePositions`, not on staff-local bars

## Remaining Work

- Define explicit extension lifecycle rules for import attachment, edit-time invalidation/preservation, and export-time regeneration/defaulting
- Decide whether Core needs stable node IDs beyond positional context plus extension-owned source IDs
- Implement derived navigation cache invalidation/recompute rules
- Replace the current `Track.Measures` + `Measure.AdditionalStaffBars` shape with `Track.Staves` + `Score.MeasurePositions`
- Normalize malformed imports into the aligned hierarchy with diagnostics where possible

# Step 1 - Extensibility Contracts in Core

## Status

Done.

## Landed

- `IModelExtension` and `IExtensibleModel` exist in `Motif.Core`
- Typed retrieval/set/remove helpers are in place
- Core references no GP-specific extension types
- Core JSON remains Core-only; GP extensions are intentionally not serialized in Core JSON
- GP package attaches `GpScoreExtension`, `GpTrackExtension`, `GpMeasureExtension`, `GpMeasureStaffExtension`, `GpVoiceExtension`, `GpBeatExtension`, and `GpNoteExtension`
- GP package exposes ergonomic accessors and score-to-score reattachment helpers for GP-aware write paths

## Remaining

- Formalize extension invalidation/regeneration policy as public/internal design guidance and tests
- Revisit extensible node list after the staff/measure hierarchy refactor lands

# Step 2 - Split Core Domain vs Guitar Pro Fidelity

## Status

Mostly done. The bulk extraction work is complete; the remaining work is semantic cleanup.

## Landed

GP-specific fidelity has been moved out of Core for:

- score/master-track/track metadata and cache models
- measure/staff/voice raw XML, source IDs, and property bags
- beat/note raw XML, source rhythm/note IDs, and note source pitch/string/fret context
- rhythm-shape preservation metadata
- beat-level GPIF shape flags and notation/reference payloads
- note raw slide flags
- note GP instrument-articulation IDs
- beat-level GP voice payload duplication

## Remaining Audit

Review surviving Core properties and either keep them as true music-domain semantics or move them behind GP extensions / remodel them more cleanly:

- `BeatModel.Wah`
- `BeatModel.Golpe`
- `BeatModel.VibratoWithTremBarStrength`
- `NoteArticulationModel.AntiAccentValue`
- any other surviving GP-oriented playback/notation values found during the hierarchy refactor

## Additional Cleanup

- Audit whether `TrackModel`, `MeasureModel`, `MeasureStaffModel`, `MeasureVoiceModel`, `BeatModel`, and `NoteModel` should keep legacy names for v1 or be renamed as part of the Core API cleanup
- Introduce `GpStaffExtension` when the new `Track -> Staff -> StaffMeasure` Core hierarchy exists

# Step 3 - Raw Cache Invariants

## Status

Structural extraction is mostly done. Policy work remains.

## Landed

- Raw GP/XML/source caches are no longer exposed from Core for score/track/measure/staff/voice/beat/note fidelity already migrated
- CLI `--from-json --source-gp` reattaches source GP extensions before no-op export so current JSON workflows can preserve fidelity where source context exists
- Writer already has some inference/default behavior when source fidelity is absent

## Remaining

- Document what each surviving GP cache is allowed to preserve and what it must never be treated as
- Define which edits invalidate which extension caches
- Define when export should reuse source payload, synthesize from Core, or fall back to defaults
- Emit diagnostics consistently when fidelity cannot be preserved exactly
- Add targeted tests around invalidation/regeneration behavior rather than only no-op round trips

# Step 4 - Guitar Pro Format I/O Ownership

## Status

Mostly done.

## Landed

- GPIF raw models, archive readers/writers, XML serializer/deserializer, mapper/unmapper, diagnostics helpers, `GuitarProReader`, `GuitarProWriter`, and `GpReadOptions` live in `Motif.Extensions.GuitarPro`
- `Motif.Core` no longer depends on GPIF/archive/XML implementation details
- Core JSON serialization lives in `Motif.Core.Serialization`

## Remaining

- Decide whether `IScoreMapper` / `IScoreUnmapper` should remain public in the GP package or be internalized/removed
- Decide whether `WriteResult`, `WriteDiagnostics`, and `WriteDiagnosticEntry` should stay GP-package types or be generalized later
- Keep cleaning up folder placement/internal visibility as APIs settle

# Step 5 - Format-Agnostic Reader/Writer Contracts

## Status

Not started.

## Remaining

- Add stream-based `IScoreReader` / `IScoreWriter` contracts to `Motif.Core`
- Implement them in `Motif.Extensions.GuitarPro`
- Keep GP-specific path conveniences and GP-specific options in the GP package
- Avoid inventing fake universal options too early

# Step 6 - Navigation in Core

## Status

Not started architecturally; current implementation still lives behind GP contracts.

## Current State

- `DefaultNavigationResolver` exists in `Motif.Extensions.GuitarPro`
- The algorithm is musically generic, but its contract boundary is still GP-specific

## Remaining

- Define format-agnostic navigation inputs in `Motif.Core`
- Refactor navigation resolution to operate on Core musical structures instead of GPIF master-bar types
- Define invalidation/recompute behavior for repeats, jumps, codas, alternate endings, segnos, DS/DC/Fine, and measure-position edits
- Expand Core tests around traversal once the contract moves

# Step 7 - CLI

## Status

Partially done.

## Landed

- CLI references the new package layout
- Motif naming is in place
- JSON no-op round-trip flow can reattach GP extensions from `--source-gp`

## Remaining

- Route formats by extension and/or explicit argument in a way that can scale past Guitar Pro
- Keep `.gp` workflows simple while avoiding a GP-only CLI architecture
- Review CLI docs/help text once reader/writer abstractions are introduced

# Step 8 - Tests

## Status

Boundary split is done; Core coverage still needs to grow after the model refactors.

## Landed

- `Tests/Motif.Core.UnitTests` contains Core-only extension/mutation/API/JSON coverage
- `Tests/Motif.Extensions.GuitarPro.UnitTests` contains GP read/write/fidelity/mapping/diagnostics coverage
- GP tests were updated to use extension reattachment for no-op fidelity assertions
- Public API surface tests exist for Core and GP packages

## Remaining

- Add Core tests for the future `Track -> Staff -> StaffMeasure` hierarchy
- Add Core navigation tests after navigation contracts move into Core
- Add tests for extension invalidation/defaulting/regeneration policy
- Revisit whether shared test infrastructure is justified once a second format exists

# Step 9 - Public API Review

## Status

Not done.

## Remaining

- Audit `Motif.Core` for a minimal format-agnostic public surface
- Audit `Motif.Extensions.GuitarPro` for accidental leakage of GPIF/raw implementation details
- Internalize migration scaffolding and incidental helpers where possible
- Decide whether legacy `*Model` type names stay for v1
- Re-run API surface tests after each public cleanup pass

# Step 10 - Packaging and Release Prep

## Status

Not done.

## Remaining

- Add package metadata to `Motif.Core`, `Motif.Extensions.GuitarPro`, and `Motif`
- Validate package responsibilities/descriptions/dependencies
- Add symbols, Source Link, license/readme metadata, and release-friendly packaging settings
- Ensure `Motif.Core` is independently consumable and `Motif.Extensions.GuitarPro` depends on Core only
- Run release builds and final docs verification

# Release Gate

v1 is ready when all of the following are true:

- Motif rename/packaging/docs work is complete
- Core vs Guitar Pro boundaries are intentionally finalized
- The remaining GP-shaped Core properties have been audited
- The staff/measure hierarchy refactor is complete
- Navigation contracts/logic are Core-owned rather than GPIF-owned
- Format-agnostic reader/writer contracts exist
- Public API review is complete
- Test coverage matches the finalized architecture and all tests pass
