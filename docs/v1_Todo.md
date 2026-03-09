# Complete Before v1 Release

GPIO.NET has proven the hard part already: it can read and write `.gp` files without data fidelity loss or file size bloat.

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

---

# Step 0 — Resolve Design Decisions That Shape the Core Model

These decisions must be resolved before the Core/extension split is finalized, because they directly determine the public model shape.

## 0.1 Mutation Model

- [ ] Define whether the Motif domain model is mutable, immutable, or mixed
- [ ] Define how extensions are attached, preserved, or invalidated during edits
- [ ] Define how node identity behaves across structural edits
- [ ] Define how navigation state behaves after edits

### Acceptance Criteria

- [ ] The editing model is documented
- [ ] Mutation behavior does not leave extension state inconsistent
- [ ] The public API does not accidentally mix incompatible editing paradigms

## 0.2 Extension Granularity

Validate whether every planned extension point is justified.

### Candidate Extension Points

- [ ] `Score`
- [ ] `MeasurePosition`
- [ ] `Track`
- [ ] `Staff`
- [ ] `StaffMeasure`
- [ ] `Voice`
- [ ] `Beat`
- [ ] `Note`
- [ ] `Rhythm`

### Review Criteria

Each extensible node must:
- already be a meaningful domain object
- have a stable lifecycle/identity
- justify independent format-specific state

### Specific Concern

- [ ] Decide whether `Rhythm` truly requires its own extension point or whether GP-specific fidelity concerns can be folded into `Beat`

### Acceptance Criteria

- [ ] Extension granularity is intentional
- [ ] The public API does not introduce extension points solely to house GP leftovers

## 0.3 Staff and Measure Hierarchy

The current model inlines primary staff data onto `MeasureModel` and stores additional staves in an `AdditionalStaffBars` collection. This creates an asymmetry that does not reflect the musical domain — staves within a track are peers, not a privileged primary plus bolt-on extras.

### Required Decisions

- [ ] Adopt `Track → Staff[] → StaffMeasure[] → Voice[] → Beat[] → Note[]` so all staves are peer children of a track
- [ ] Introduce a score-level `MeasurePosition` (or similar) state shared across tracks/staves at a given index to own shared timeline state (time signature, key signature, repeats, sections, jumps, targets, alternate endings, fermatas)
- [ ] Define `StaffMeasure` as the staff-local container for clef, voices, and beats
- [ ] Remove the primary/additional staff asymmetry from the Core model
- [ ] Retire `MeasureStaffModel` as a migration artifact rather than preserving it
- [ ] Define alignment invariants between Score.MeasurePositions and each staff’s StaffMeasure sequence, every StaffMeasure.MeasureIndex must correspond to a valid MeasurePosition and all staves in a track should align to the same measure-position structure unless intentionally modeling sparse/partial data
- [ ] Define what happens when imported data is malformed or incomplete

### Acceptance Criteria

- [ ] No privileged "primary staff" representation exists in Core
- [ ] Multi-staff tracks are modeled as peer staves
- [ ] Timeline-global measure state has distinct ownership from staff-local content
- [ ] Single-staff instruments still work naturally (one-element `Staves` collection)
- [ ] The model aligns with MusicXML's part/staff structure without forcing GP-specific serialization shape onto Core

## 0.4 Navigation Abstraction

Playback/navigation semantics are format-agnostic, but the current implementation is GP-specific at the contract boundary.

### Current Problem

`DefaultNavigationResolver` implements generic musical traversal logic, but currently operates on `GpifMasterBar` input. The algorithm belongs in Core; the contract does not.

### Required Work

- [ ] Define format-agnostic navigation input abstractions in `Motif.Core`
- [ ] Refactor navigation logic to operate on Core musical structures rather than GPIF types
- [ ] Preserve repeat/jump/coda/alternate-ending behavior during the refactor

### Acceptance Criteria

- [ ] Navigation concepts live in Core
- [ ] Navigation logic no longer depends on GPIF model types
- [ ] Guitar Pro package can populate/navigation inputs without owning the traversal algorithm

---

# Step 1 — Define Extensibility Contracts in `Motif.Core`

Replace string-keyed public extension access with typed retrieval APIs.

## Requirements

- [ ] Add a format-agnostic extension abstraction to `Motif.Core`
- [ ] Support typed retrieval of extensions attached to domain nodes
- [ ] Avoid exposing string-keyed lookup as the primary public API
- [ ] Permit extension packages to attach package-specific fidelity/format state at runtime
- [ ] Support absent extensions gracefully, since not every score will originate from the same source format

## Proposed Shape

```csharp
public interface IExtensibleModel
{
    bool TryGetExtension<TExtension>(out TExtension? extension)
        where TExtension : class, IModelExtension;

    IReadOnlyCollection<IModelExtension> GetExtensions();
}

public interface IModelExtension
{
}
```

## Notes

* `Motif.Extensions.GuitarPro` should expose ergonomic helpers such as `.GetGuitarPro()`
* Backing storage may still be dictionary-based internally, but that should not define the public API
* Writers/converters must be able to inspect available extensions dynamically at runtime

## Acceptance Criteria

* [ ] A consumer can query for a specific extension type without string keys
* [ ] Missing extensions can be detected via `TryGetExtension`
* [ ] Required extension access is explicit and predictable
* [ ] `Motif.Core` references no format-specific extension types

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
| `GpBeatExtension`           | Beat source XML, raw GP flags, source rhythm IDs, GP-only beat state such as golpe/fadding/rasgueado/slap/pop flags        |
| `GpNoteExtension`           | Source note XML, raw slide flags, source MIDI/transposed pitch/fret/string data, GP-specific articulation fidelity state   |
| `GpRhythmExtension`         | Rhythm source XML and GP-specific augmentation-dot fidelity state, only if Rhythm remains independently extensible         |

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

* [ ] invalidate the affected cache(s)
* [ ] regenerate the affected cache(s)
* [ ] mark fidelity preservation as degraded for the affected region

## Acceptance Criteria

* [ ] Raw source caches are not exposed from Core
* [ ] Internal docs clearly define the purpose and limits of cached source data
* [ ] The writer does not rely on stale caches after substantive model mutation

---

# Step 4 — Move Guitar Pro Format I/O into `Motif.Extensions.GuitarPro`

Move all `.gp`, GPIF, archive, mapping, and GP-specific diagnostics concerns out of Core.

## Move to `Motif.Extensions.GuitarPro`

* [ ] `Models/Raw/` (all GPIF XML model types)
* [ ] `Implementation/` GP-specific archive, serializer, deserializer, mapping, and unmapping logic
* [ ] GP-specific abstractions
* [ ] `GuitarProReader`
* [ ] `GuitarProWriter`
* [ ] `GpReadOptions`
* [ ] `Resources/DefaultTemplate.gp`
* [ ] `Utilities/ReferenceListParser`
* [ ] `Utilities/ReferenceListFormatter`
* [ ] `Utilities/ArticulationDecoders`
* [ ] `GpifWriteFidelityDiagnostics`
* [ ] `GpifXmlDifferenceComparer`

## Explicitly GP-Specific Interfaces/Types to Move or Remove

These names are generic today, but the contracts are not.

* [ ] `IScoreMapper` — currently maps `GpifDocument -> GuitarProScore`; move to GuitarPro if it remains
* [ ] `IScoreUnmapper` — currently maps `GuitarProScore -> GpifDocument`; move to GuitarPro if it remains
* [ ] Remove mapper/unmapper interfaces entirely if `IScoreReader` / `IScoreWriter` make them unnecessary public surface

## Keep in `Motif.Core` Only If Truly Format-Agnostic

* [ ] Navigation abstractions and traversal logic after refactoring away from GPIF inputs
* [ ] Format-agnostic diagnostics models such as `WriteDiagnostics` and `WriteDiagnosticEntry`
* [ ] JSON serialization of the Core domain model

## Do Not Keep in Core

* [ ] `WriteResult` in its current form, because it contains `GpifDocument` directly
* [ ] Any GPIF/archive/XML implementation detail
* [ ] Any utility that encodes GPIF-specific syntax or semantics

## JSON Serialization Clarification

Current JSON types serialize the domain model rather than GPIF transport types.

### Core Candidates

* [ ] `GpioJsonContext.cs` (rename appropriately for Motif)
* [ ] `GuitarProScoreJson.cs` / `ToJson()` extension if it serializes the domain model rather than GP-only transport state

### Required Work

* [ ] Rename JSON serialization components to match Motif naming
* [ ] Ensure JSON serialization targets the Core model, not GPIF implementation types
* [ ] Keep JSON APIs in Core only if they represent serialization of the domain model itself

## Acceptance Criteria

* [ ] `Motif.Core` has no dependency on GPIF/archive/XML implementation details
* [ ] `Motif.Extensions.GuitarPro` fully owns `.gp` read/write behavior
* [ ] JSON serialization of the domain model remains available from Core
* [ ] Removing the GuitarPro package does not break Core

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

* [ ] Update project references to the new package layout
* [ ] Rename namespaces and binary output to Motif naming
* [ ] Route format selection by file extension or explicit argument
* [ ] Keep current `.gp` workflows simple while allowing future formats

## Acceptance Criteria

* [ ] CLI works with `Motif.Core` + `Motif.Extensions.GuitarPro`
* [ ] CLI defaults remain simple for `.gp`
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
* [ ] JSON serialization of the Core model
* [ ] Navigation logic after GPIF decoupling
* [ ] Mutation/fidelity policy tests that belong to Core behavior

## GuitarPro Tests

* [ ] Read/write behavior
* [ ] Round-trip fidelity
* [ ] Source cache preservation behavior
* [ ] Mapping/unmapping correctness
* [ ] GP-specific diagnostics utilities
* [ ] Mutation scenarios that preserve or intentionally invalidate fidelity state
* [ ] Public API surface tests for the GuitarPro package

## Acceptance Criteria

* [ ] Core tests do not depend on GP-specific implementation details
* [ ] Guitar Pro tests validate the existing fidelity guarantees
* [ ] Public API approval/surface tests exist for each public package

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
