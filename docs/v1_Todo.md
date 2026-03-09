# Complete Before v1 Release

Motif has proven the hard part already: it can read and write `.gp` files without data fidelity loss or file size bloat.

That milestone validates the technical foundation. The remaining work before v1 is to align the project name, codebase, package structure, and public API with the actual product we are shipping:

> Motif is a format-agnostic .NET music domain model and conversion library, with format-specific extension packages for lossless or near-lossless round-tripping where possible.

---

## v1 Goals

- Ship a stable Core package for representing and modifying musical data programmatically
- Preserve full Guitar Pro read/write fidelity in the initial format extension package
- Establish a Core + format extension architecture that can support additional formats without breaking Core APIs
- Publish a professional NuGet package layout with clear package responsibilities

## v1 Non-Goals

The following are explicitly out of scope for v1:

- Playback engine
- Rendering / notation engraving engine
- End-user notation editor UI framework
- DAW abstraction layer
- Universal normalization of every concept across every music format
- Initial multi-format support beyond Guitar Pro

---

## Housekeeping

- [ ] Finalize repository/project/package naming migration away from `GPIO.NET` and over to `Motif`
- [ ] Rename solution, projects, namespaces, assembly names, package IDs, CLI binary, docs, badges, and references
- [ ] Update README to describe:
  - what Motif is
  - what problems it solves
  - current format support
  - package structure
  - basic read/modify/write examples
  - CLI capabilities and limitations
- [ ] Add/verify NuGet package metadata for all shipping packages

### Progress Update — 2026-03-09

- [x] Move the GP read/write pipeline out of `Motif.Core` and into `Motif.Extensions.GuitarPro`
- [x] Move the GP-focused test suite into `Motif.Extensions.GuitarPro.UnitTests`
- [x] Finish the namespace migration from `GPIO.NET` to `Motif*` across Core, Guitar Pro, and CLI code
- [ ] Remaining rename work: public type/API cleanup, package metadata/package IDs, and any last documentation/badge references

---

# Step 0 — Resolve Design Decisions That Shape the Core Model

These decisions define the target Core model shape. The design questions below are resolved enough to turn into implementation work, even if the codebase still reflects the pre-refactor model.

## 0.1 Mutation Model

### Decision

- [x] `Motif.Core` should expose a mutable domain model optimized for direct, logical programmatic edits.
- [x] Core musical data is authoritative. Extension data is supplemental fidelity/format state attached during import and consulted again during export.
- [x] Importers attach extension data after they map the core model, so a later export can preserve format fidelity where possible.
- [x] Exporters use existing extension data when it is present and still usable; otherwise they infer from the core model, infer from other available extensions, or fall back to sensible defaults so output remains valid.
- [x] Motif should preserve extension data best-effort, but it does not guarantee semantic validation or preservation of arbitrary user-authored edits inside extension payloads. The responsibility is to emit a structurally valid file, not to police every extension-specific field.
- [x] Exact source-node identity is secondary to preserving musical context, application compatibility, and reasonable output size. Structural edits may require reallocation of format-specific IDs on export.
- [x] Public traversal should stay loop-friendly. Prefer collections that support straightforward `for` loops, and add helper enumerators only where they simplify the API without fighting the underlying model.
- [x] Navigation state is derived state, not user-authored state. It includes cached playback-order sequences, resolved jump/coda targets, repeat-pass bookkeeping, and any cursor/snapshot objects derived from the current score structure.
- [x] Edits that affect traversal invalidate derived navigation state and require recomputation before playback/export features depend on it.

### Implementation Work

- [ ] Replace the remaining init-only/Core model patterns with mutation-friendly APIs that are consistent across score, track, staff, measure, beat, and note nodes
- [ ] Define the extension lifecycle explicitly: import attachment, edit-time preservation/invalidation rules, and export-time synthesis/defaulting
- [ ] Document the exporter fallback order: preserve extension data -> infer from core model -> infer from other extensions -> apply defaults -> emit diagnostics if fidelity degraded
- [ ] Decide whether v1 needs explicit stable Core node IDs, or whether positional context plus source-format IDs stored in extensions is sufficient
- [ ] Define and implement invalidation/recompute rules for derived navigation state after structural edits

### Acceptance Criteria

- [ ] The mutable editing model is documented
- [ ] Exporters can still produce valid files when extension data must be inferred or defaulted
- [ ] Mutation behavior does not leave extension state inconsistent
- [ ] Derived navigation state is invalidated and recomputed predictably after traversal-affecting edits
- [ ] The public API does not accidentally mix incompatible editing paradigms

## 0.2 Extension Granularity

The extension surface should stay as small as possible while still giving format packages a clean place to retain fidelity state.

### Planned Extension Points

- [x] `Score`
- [x] `MeasurePosition`
- [x] `Track`
- [x] `Staff`
- [x] `StaffMeasure`
- [x] `Voice`
- [x] `Beat`
- [x] `Note`
- [ ] `Rhythm` — fold any rhythm-specific fidelity into `Beat`

### Review Criteria

Each extensible node must:
- already be a meaningful domain object
- have a stable lifecycle/identity
- justify independent format-specific state

### Decision

- [x] `Rhythm` is not an independent extension point for v1 planning
- [x] GP-specific rhythm fidelity concerns belong on `Beat`
- [x] The public extensibility surface should not add nodes solely to carry leftover GP transport data

### Implementation Work

- [ ] Remove `Rhythm` as a first-class extensibility concept from the API sketches and downstream planning docs
- [ ] Fold planned `GpRhythmExtension` responsibilities into `GpBeatExtension`
- [ ] Verify that each remaining extension point has a concrete import/export or fidelity-preservation responsibility before the public API is frozen

### Acceptance Criteria

- [ ] Extension granularity is intentional
- [ ] The public API does not introduce extension points solely to house GP leftovers
- [ ] `Rhythm` does not require its own extension contract or retrieval API

## 0.3 Staff and Measure Hierarchy

The current model inlines primary staff data onto `MeasureModel` and stores additional staves in an `AdditionalStaffBars` collection. This creates an asymmetry that does not reflect the musical domain — staves within a track are peers, not a privileged primary plus bolt-on extras.

### Decision

- [x] Adopt `Score → Track[] → Staff[] → StaffMeasure[] → Voice[] → Beat[] → Note[]` so all staves are peer children of a track
- [x] Introduce score-level `MeasurePositions` aligned by index across the score to own timeline-global state (time signature, key signature, repeats, sections, jumps, targets, alternate endings, fermatas)
- [x] Define `StaffMeasure` as the staff-local container for clef, voices, beats, and other staff-scoped notation content at a given measure position
- [x] Remove the primary/additional-staff asymmetry from the Core model
- [x] Treat `MeasureStaffModel` as a migration artifact rather than a permanent Core type
- [x] Keep the public model loop-friendly through aligned collections and simple indexing rather than forcing consumers through complex traversal helpers for common operations
- [x] Imported malformed or incomplete data should be normalized into the aligned structure when possible, with diagnostics emitted for gaps or repairs
- [x] If imported data cannot be placed into deterministic musical order, strict import should fail and lenient import should surface diagnostics instead of inventing ambiguous structure

### Alignment Rules

- [x] Every `StaffMeasure.MeasureIndex` must correspond to a valid `Score.MeasurePositions[index]`
- [x] All staves in a track should align to the same `MeasurePositions` sequence unless the importer is explicitly modeling sparse/incomplete data
- [x] Missing staff-local content at a valid measure position should normalize to an empty `StaffMeasure` plus diagnostics rather than a broken graph

### Implementation Work

- [ ] Replace `Track.Measures` and `MeasureModel.AdditionalStaffBars` with `Track.Staves`
- [ ] Introduce `Score.MeasurePositions` and move shared timeline state there
- [ ] Define empty/placeholder `StaffMeasure` behavior for normalized malformed imports
- [ ] Migrate GP mapping/unmapping to the new hierarchy while preserving source-ID reuse and avoiding unnecessary archive/XML bloat
- [ ] Remove `MeasureStaffModel` from the public Core surface once mappers/writers no longer depend on it

### Acceptance Criteria

- [ ] No privileged "primary staff" representation exists in Core
- [ ] Multi-staff tracks are modeled as peer staves
- [ ] Timeline-global measure state has distinct ownership from staff-local content
- [ ] Single-staff instruments still work naturally (one-element `Staves` collection)
- [ ] Normalized imports preserve deterministic ordering, musical context, and acceptable re-export size characteristics
- [ ] The model aligns with MusicXML's part/staff structure without forcing GP-specific serialization shape onto Core

## 0.4 Navigation Abstraction

Playback/navigation semantics are format-agnostic, but the current implementation is GP-specific at the contract boundary.

### Current Problem

`DefaultNavigationResolver` implements generic musical traversal logic, but currently operates on `GpifMasterBar` input. The algorithm belongs in Core; the contract does not.

### Clarification

Navigation state means any derived traversal data that answers questions like:

- what is the playback order of measure positions?
- where does a jump, coda, segno, DS/DC, or alternate ending land?
- what repeat pass is currently active?
- what cursor/snapshot is needed to resume navigation after an edit or traversal step?

This state should be derived from Core musical structure, not stored as user-authored authoritative data.

### Required Work

- [ ] Define format-agnostic navigation input abstractions in `Motif.Core`
- [ ] Refactor navigation logic to operate on Core musical structures rather than GPIF types
- [ ] Define cache invalidation/recompute rules for derived navigation state after edits to repeats, jumps, targets, endings, or measure-position structure
- [ ] Preserve repeat/jump/coda/alternate-ending behavior during the refactor

### Acceptance Criteria

- [ ] Navigation concepts live in Core
- [ ] Navigation logic no longer depends on GPIF model types
- [ ] Derived navigation state is recomputed from Core structure rather than treated as authoritative mutable data
- [ ] Guitar Pro package can populate/navigation inputs without owning the traversal algorithm

---

# Step 1 — Define Extensibility Contracts in `Motif.Core`

Replace string-keyed public extension access with typed retrieval APIs.

## Requirements

- [x] Add a format-agnostic extension abstraction to `Motif.Core`
- [x] Support typed retrieval of extensions attached to domain nodes
- [x] Avoid exposing string-keyed lookup as the primary public API
- [x] Permit extension packages to attach package-specific fidelity/format state at runtime
- [x] Support absent extensions gracefully, since not every score will originate from the same source format

## Proposed Shape

```csharp
public interface IExtensibleModel
{
    bool TryGetExtension<TExtension>(out TExtension? extension)
        where TExtension : class, IModelExtension;

    IReadOnlyCollection<IModelExtension> GetExtensions();

    void SetExtension<TExtension>(TExtension extension)
        where TExtension : class, IModelExtension;

    bool RemoveExtension<TExtension>()
        where TExtension : class, IModelExtension;
}

public interface IModelExtension
{
}
```

## Notes

* `Motif.Extensions.GuitarPro` should expose ergonomic helpers such as `.GetGuitarPro()`
* Backing storage may still be dictionary-based internally, but that should not define the public API
* Writers/converters must be able to inspect available extensions dynamically at runtime
* Implemented in Core on 2026-03-09 via `IExtensibleModel`, `IModelExtension`, `ExtensibleModel`, and typed helper APIs such as `GetRequiredExtension<T>()`
* Current extensible Core nodes: `GuitarProScore`, `TrackModel`, `MeasureModel`, `MeasureStaffModel`, `MeasureVoiceModel`, `BeatModel`, and `NoteModel`
* Next step: start attaching concrete Guitar Pro extensions during import and consume them during the later Core/GP property split

## Acceptance Criteria

* [x] A consumer can query for a specific extension type without string keys
* [x] Missing extensions can be detected via `TryGetExtension`
* [x] Required extension access is explicit and predictable
* [x] `Motif.Core` references no format-specific extension types

---

# Step 2 — Split the Domain Model Between `Motif.Core` and `Motif.Extensions.GuitarPro`

Audit every public model and property to determine whether it represents:

1. a true music-domain concept, or
2. a Guitar Pro encoding / playback / round-trip fidelity concern

## Decision Rule

A property belongs in Core if it represents stable musical semantics, even if not every format supports it.

A property belongs in a format extension if it represents:

* source-format encoding details
* application-specific presentation/layout state
* application-specific playback engine state
* raw source caches needed only for lossless round-tripping

## Core Domain Model

`Motif.Core` should contain format-agnostic musical concepts such as:

| Model              | Core Responsibilities                                                                                       |
| ------------------ | ----------------------------------------------------------------------------------------------------------- |
| `Score`            | Metadata, tracks, measure positions (timeline-global state)                                                 |
| `MeasurePosition`  | Time signature, key signature, repeats, sections, alternate endings, jumps, targets, fermatas               |
| `Track`            | Identity, tuning, instrument info, capo, transpose, lyrics, staves                                         |
| `Staff`            | Peer staff within a track, owns its own measure sequence                                                    |
| `StaffMeasure`     | Staff-local content at a measure position: clef, voices, beats                                              |
| `Voice`            | Beat container and ordering                                                                                 |
| `Beat`             | Rhythm, notes, dynamics, grace notes, articulations, text/annotations, beat-level musical semantics         |
| `Note`             | Pitch, string/fret context, velocity, duration, articulation                                                |
| `NoteArticulation` | Ties, bends, vibrato, slides, harmonics, ornaments, muting, fingering, etc.                                 |
| Value Types        | Pitch, rhythm, tuplets, bend models, harmonic models, fermata metadata, tempo events, etc.                  |

## Guitar Pro Extension Model

`Motif.Extensions.GuitarPro` should contain GP-specific fidelity and encoding models such as:

| Extension Model             | Responsibilities                                                                                                           |
| --------------------------- | -------------------------------------------------------------------------------------------------------------------------- |
| `GpScoreExtension`          | GP metadata, score XML caches, layout/page setup, backing track state, explicit-empty flags, source archive fidelity state |
| `GpMeasurePositionExtension`| Master bar source XML, XProperties, attribute-presence flags, GP-only direction/layout state, the landing zone for GP master-bar fidelity state after the Core model separates shared measure-position state from staff-local bar state                               |
| `GpTrackExtension`          | GP-specific playback/RSE/audio state, source track XML fragments, notation patches, GP property bags, source IDs           |
| `GpStaffExtension`          | GP-specific staff metadata, staff-level source fragments and properties                                                    |
| `GpStaffMeasureExtension`   | Bar source XML, SourceBarId, bar XProperties/properties, GP-specific clef/simile fidelity state                            |
| `GpVoiceExtension`          | Voice source XML, source IDs, direction tags, GP-only properties                                                           |
| `GpBeatExtension`           | Beat source XML, raw GP flags, source rhythm IDs, rhythm-source fidelity (including augmentation-dot preservation), GP-only beat state such as golpe/fadding/rasgueado/slap/pop flags        |
| `GpNoteExtension`           | Source note XML, raw slide flags, source MIDI/transposed pitch/fret/string data, GP-specific articulation fidelity state   |

## Acceptance Criteria

* [ ] No Guitar Pro-specific property remains on Core types unless it is truly a music-domain concept
* [ ] Core remains expressive enough for future formats without becoming anemic
* [ ] Guitar Pro-specific fidelity state is preserved through extensions
* [ ] Public XML cache properties are removed from Core types

---

# Step 3 — Define Raw Source Cache Invariants

The raw XML/source-fragment preservation strategy is acceptable for v1, but it must be formalized.

## Requirements

* [ ] Raw XML/source fragments must live only in `Motif.Extensions.GuitarPro`
* [ ] Raw caches must exist only for fidelity preservation, not as a second authoritative object model
* [ ] Mutations that invalidate preserved fragments must follow a documented policy

## Required Design Decision

For edits that invalidate cached source fragments, define whether the system will:

* [x] invalidate the affected cache(s)
* [x] regenerate or synthesize the affected output from the Core model and any other usable extension data during export
* [x] apply sensible defaults when required extension state cannot be inferred, while recording diagnostics when fidelity is necessarily degraded

## Acceptance Criteria

* [ ] Raw source caches are not exposed from Core
* [ ] Internal docs clearly define the purpose and limits of cached source data
* [ ] The writer does not rely on stale caches after substantive model mutation
* [ ] Export remains valid even when source caches must be replaced by inferred or defaulted extension data

---

# Step 4 — Move Guitar Pro Format I/O into `Motif.Extensions.GuitarPro`

Move all `.gp`, GPIF, archive, mapping, and GP-specific diagnostics concerns out of Core.

## Move to `Motif.Extensions.GuitarPro`

* [x] `Models/Raw/` (all GPIF XML model types)
* [x] `Implementation/` GP-specific archive, serializer, deserializer, mapping, and unmapping logic
* [x] GP-specific abstractions
* [x] `GuitarProReader`
* [x] `GuitarProWriter`
* [x] `GpReadOptions`
* [x] `Resources/DefaultTemplate.gp`
* [x] `Utilities/ReferenceListParser`
* [x] `Utilities/ReferenceListFormatter`
* [x] `Utilities/ArticulationDecoders`
* [x] `GpifWriteFidelityDiagnostics`
* [x] `GpifXmlDifferenceComparer`

## Explicitly GP-Specific Interfaces/Types to Move or Remove

These names are generic today, but the contracts are not.

* [x] `IScoreMapper` — currently maps `GpifDocument -> GuitarProScore`; move to GuitarPro if it remains
* [x] `IScoreUnmapper` — currently maps `GuitarProScore -> GpifDocument`; move to GuitarPro if it remains
* [ ] Remove mapper/unmapper interfaces entirely if `IScoreReader` / `IScoreWriter` make them unnecessary public surface

## Keep in `Motif.Core` Only If Truly Format-Agnostic

* [ ] Navigation abstractions and traversal logic after refactoring away from GPIF inputs
* [ ] Format-agnostic diagnostics models such as `WriteDiagnostics` and `WriteDiagnosticEntry`
* [x] JSON serialization of the Core domain model

## Do Not Keep in Core

* [x] `WriteResult` in its current form, because it contains `GpifDocument` directly
* [x] Any GPIF/archive/XML implementation detail
* [x] Any utility that encodes GPIF-specific syntax or semantics

## JSON Serialization Clarification

Current JSON types serialize the domain model rather than GPIF transport types.

### Core Candidates

* [x] `MotifJsonContext.cs` (renamed from `GpioJsonContext.cs`)
* [x] `GuitarProScoreJson.cs` / `ToJson()` extension if it serializes the domain model rather than GP-only transport state

### Required Work

* [x] Rename JSON serialization components to match Motif naming
* [x] Ensure JSON serialization targets the Core model, not GPIF implementation types
* [x] Keep JSON APIs in Core only if they represent serialization of the domain model itself

## Acceptance Criteria

* [x] `Motif.Core` has no dependency on GPIF/archive/XML implementation details
* [x] `Motif.Extensions.GuitarPro` fully owns `.gp` read/write behavior
* [x] JSON serialization of the domain model remains available from Core
* [x] Removing the GuitarPro package does not break Core

---

# Step 5 — Generalize Reader/Writer Abstractions in `Motif.Core`

Define format-agnostic reader/writer contracts suitable for long-term multi-format support.

## Requirements

* [ ] Introduce stream-based format-agnostic interfaces in Core
* [ ] Keep file-path convenience APIs on concrete reader/writer implementations as needed
* [ ] Do not force fake universal options into Core

## Proposed Shape

```csharp
public interface IScoreReader
{
    ValueTask<Score> ReadAsync(Stream stream, CancellationToken cancellationToken = default);
}

public interface IScoreWriter
{
    ValueTask WriteAsync(Score score, Stream stream, CancellationToken cancellationToken = default);
}
```

## Notes

* `Motif.Extensions.GuitarPro` should implement these contracts
* File path overloads may exist on concrete GP reader/writer types
* `GpReadOptions` remains in the GuitarPro package because its current concerns are GPIF-specific
* If format-agnostic read/write options are needed later, that should be a new Core abstraction rather than a forced reuse of `GpReadOptions`

## Acceptance Criteria

* [ ] Consumers can read/write using streams without file system coupling
* [ ] Guitar Pro package implements the Core contracts
* [ ] Concrete GP APIs still provide convenient path-based overloads where appropriate

---

# Step 6 — Keep Musical Navigation Concepts in `Motif.Core`

Preserve format-agnostic traversal semantics in Core once the contract boundary has been refactored.

## Requirements

* [ ] Keep repeat/jump/coda/alternate-ending traversal concepts in Core
* [ ] Express these concepts in musical/domain terms rather than GP parser terms
* [ ] Ensure the algorithm operates on Core abstractions rather than GPIF raw types

## Candidate Concepts

* [ ] Playback / measure traversal sequence
* [ ] Repeat unrolling
* [ ] Jump target resolution
* [ ] Alternate ending resolution

## Acceptance Criteria

* [ ] Navigation logic is reusable beyond Guitar Pro
* [ ] Core contains the algorithm and abstractions
* [ ] The Guitar Pro extension package populates the needed Core state

---

# Step 7 — Update CLI Tool

Convert the CLI into a host for Core + extension packages rather than a GP-only companion.

## Requirements

* [x] Update project references to the new package layout
* [x] Rename namespaces and binary output to Motif naming
* [ ] Route format selection by file extension or explicit argument
* [ ] Keep current `.gp` workflows simple while allowing future formats

## Acceptance Criteria

* [x] CLI works with `Motif.Core` + `Motif.Extensions.GuitarPro`
* [x] CLI defaults remain simple for `.gp`
* [ ] CLI architecture can accept future formats without redesign

---

# Step 8 — Split and Harden the Test Suite

Align tests to match package boundaries and protect the new contracts.

## Target Test Layout

```text
Tests/
  Motif.Core.UnitTests/
  Motif.Extensions.GuitarPro.UnitTests/
  Motif.TestCommon/        (optional if shared infrastructure earns its keep)
```

## Core Tests

* [ ] Domain model construction and invariants
* [ ] Value object behavior
* [ ] Extension retrieval contracts
* [x] JSON serialization of the Core model
* [ ] Navigation logic after GPIF decoupling
* [ ] Mutation/fidelity policy tests that belong to Core behavior

## GuitarPro Tests

* [x] Read/write behavior
* [x] Round-trip fidelity
* [x] Source cache preservation behavior
* [x] Mapping/unmapping correctness
* [x] GP-specific diagnostics utilities
* [x] Mutation scenarios that preserve or intentionally invalidate fidelity state
* [x] Public API surface tests for the GuitarPro package

## Acceptance Criteria

* [x] Core tests do not depend on GP-specific implementation details
* [x] Guitar Pro tests validate the existing fidelity guarantees
* [x] Public API approval/surface tests exist for each public package

### Next Step After This Split

Core coverage is intentionally light until Steps 0-6 settle the shape of the format-agnostic model and reader/writer contracts. The next concrete work item is to design those Core abstractions, then grow Core-only tests around the finalized API rather than around Guitar Pro behavior.

---

# Step 9 — Review and Refine Public API Surface

Perform a deliberate API review before release.

## `Motif.Core` Should Expose

* [ ] Domain model
* [ ] Value types
* [ ] Semantic enums
* [ ] Extensibility contracts
* [ ] Format-agnostic reader/writer contracts
* [ ] Public helpers that are broadly useful and stable
* [ ] JSON serialization APIs for the Core domain model, if retained

## `Motif.Extensions.GuitarPro` Should Expose

* [ ] `GuitarProReader`
* [ ] `GuitarProWriter`
* [ ] GP-specific options
* [ ] GP extension types
* [ ] GP accessor extension methods
* [ ] GP-specific diagnostics/results only if intentionally consumer-facing

## Hide / Internalize

* [ ] GPIF raw implementation details unless intentionally supported
* [ ] archive plumbing
* [ ] serializer/deserializer internals
* [ ] mapper/unmapper internals if retained
* [ ] incidental helpers
* [ ] migration scaffolding
* [ ] any temporary compatibility shims

## Additional Cleanup

* [ ] Use `internal` aggressively where appropriate
* [ ] Consider `[EditorBrowsable(EditorBrowsableState.Never)]` for public infrastructure members that should not attract casual consumer use
* [ ] Run public API approval tests before release

## Acceptance Criteria

* [ ] Public API is minimal, intentional, and stable
* [ ] Internal implementation details are not leaking into consumer packages
* [ ] Package boundaries are obvious from the API surface alone

---

# Step 10 — Packaging and Release Preparation

Prepare Motif for a real v1 release.

## Requirements

* [ ] Complete NuGet package metadata for `Motif.Core` and `Motif.Extensions.GuitarPro`
* [ ] Verify package descriptions accurately reflect responsibilities
* [ ] Validate versioning strategy
* [ ] Configure symbols, Source Link, license/readme metadata, and docs where applicable
* [ ] Ensure `Motif.Core` is independently consumable
* [ ] Ensure `Motif.Extensions.GuitarPro` depends on `Motif.Core`, but not vice versa

## Acceptance Criteria

* [ ] Packages build cleanly in Release
* [ ] Package metadata is complete and professional
* [ ] `Motif.Core` can be consumed without any Guitar Pro dependencies
* [ ] Initial release artifacts are ready for publication

---

# Open Design Questions

* [ ] **Automation model**
  Decide which automation concepts belong in Core as true musical semantics and which remain GP-specific playback-engine state.

* [ ] **Mutation vs fidelity policy**
  Define exactly how source-fidelity extension state behaves after structural edits.

* [ ] **Extension granularity**
  Confirm the final set of extensible model nodes before freezing public APIs.

* [ ] **Navigation input model**
  Finalize the Core abstraction used to replace `GpifMasterBar` in navigation/traversal logic.

* [ ] **JSON API shape**
  Finalize whether JSON serialization remains extension-method based, service-based, source-generated-context based, or some combination thereof in Core.

---

# Release Gate

v1 is ready only when all of the following are true:

* [ ] The Motif rename is complete
* [ ] `Motif.Core` and `Motif.Extensions.GuitarPro` boundaries are implemented
* [ ] Typed extension retrieval is in place
* [ ] Core public API is stable and format-agnostic
* [ ] Navigation logic has been decoupled from GPIF input types
* [ ] JSON serialization of the Core domain model is correctly placed
* [ ] Guitar Pro read/write fidelity remains intact after the split
* [ ] README and package metadata accurately describe the product
* [ ] Tests validate the new architecture and existing fidelity guarantees
* [ ] The Core model no longer contains primary/additional staff asymmetry

---

# Post-v1 (v1.1)

* [ ] Add MusicXML support via `Motif.Extensions.MusicXml`
* [ ] Implement `IScoreReader` / `IScoreWriter` for MusicXML
* [ ] Introduce MusicXML-specific extension models only where fidelity requires them
* [ ] Validate that Core does not require breaking changes to support the second format
