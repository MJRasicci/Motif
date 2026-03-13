# AGENTS.md - Motif Guidance

This repository contains the Motif libraries and companion CLI.

Motif is a format-agnostic music domain model and conversion framework. The core design
goal is to preserve musical meaning first, and format fidelity through extensions where
possible.

## Design Philosophies

- Core models musical reality, not the internal structure of any notation application or
  file format.
- External formats must not dictate the Core model. Motif translates between formats and
  the domain model; it does not wrap file schemas.
- Format-specific metadata, playback-engine data, layout hints, and round-trip fidelity
  state belong in extensions, not Core.
- Core should remain stable, minimal, format-agnostic, and suitable for engraving,
  analysis, and playback use cases.
- Favor semantic models over storage models. Mirroring file schemas or legacy app designs
  is the wrong default.
- Core must never depend on format packages. Parsing, serialization, mapping, and fidelity
  concerns stay outside Core.

Changes that introduce format coupling into Core, blur Core/extension boundaries, or treat
Core as a file-format wrapper should be rejected.

## What This Repo Does

Motif exposes a format-agnostic `Score` domain model and a Guitar Pro extension capable of
reading and writing GP7+ Guitar Pro archives and raw GPIF XML.

Current end-to-end supported formats:

- `.gp` (GP7+ only)
- `.gpif`
- Motif model mapped JSON

Do not treat older pre-GP7 Guitar Pro formats such as `.gpx` as supported. While some of
those formats may be convertible or structurally similar, they are intentionally out of
scope for the current product direction.

Do not treat MusicXML, MXL, or MIDI as supported until real implementation and tests exist.

## Repository Structure

```text
Source/
  Motif/                                   Convenience package referencing Core + Guitar Pro
  Motif.Core/                              Domain model, navigation, JSON helpers
  Motif.Extensions.GuitarPro/              Guitar Pro reader/writer, mapper, raw GPIF model
  Motif.CLI/                               CLI executable (assembly name: motif-cli)

Tests/
  Motif.Core.UnitTests/                    Core model and navigation tests
  Motif.Extensions.GuitarPro.UnitTests/    Guitar Pro mapping and fidelity tests
  Motif.IntegrationTests/                  CLI integration and regression tests

docs/
  CLI_WORKFLOW.md                          CLI usage and batch workflows
  LIBRARY_WORKFLOW.md                      Recommended read/edit/write workflow

Temp/                                      Gitignored scratch area
```

Ignore `Temp/` unless explicitly asked to inspect it.

## Architecture

Motif conversions follow a deterministic multi-stage pipeline:

```text
.gp file
  -> IGpArchiveReader      opens ZIP and exposes Content/score.gpif
  -> IGpifDeserializer     deserializes XML into GpifDocument
  -> IScoreMapper          resolves references into Score

Score
  -> IScoreUnmapper        produces GpifDocument from domain state
  -> IGpifSerializer       serializes XML
  -> IGpArchiveWriter      writes the .gp archive
```

These layers exist to keep file-format concerns isolated from the domain model.

## Important Types and Files

| Type / File | Purpose |
| --- | --- |
| `GuitarProReader` | Top-level `.gp` read API |
| `GuitarProWriter` | Top-level `.gp` write API |
| `DefaultScoreMapper` | Raw GPIF -> `Score` mapper |
| `DefaultScoreUnmapper` | `Score` -> raw GPIF unmapper |
| `XmlGpifDeserializer` | GPIF XML reader |
| `XmlGpifSerializer` | GPIF XML writer |
| `ScoreNavigation` | Playback sequence rebuild and invalidation logic |
| `GuitarProModelExtensions` | GP fidelity attachment and reattachment helpers |
| `CliParser` / `Program.cs` | CLI routing and command handling |

## Models

- Raw model: `Source/Motif.Extensions.GuitarPro/Models/Raw/`
  Represents GPIF XML closely and keeps passthrough data where normalization is incomplete.
- Domain model: `Source/Motif.Core/Models/`
  Exposes `Score -> Tracks -> Staves -> StaffMeasures -> Voices -> Beats -> Notes`.
- Global timeline state lives on `Score.TimelineBars`.
- Derived playback order lives on `Score.PlaybackMasterBarSequence` and should be
  maintained through `ScoreNavigation`.

## CLI Notes

- Development entry point: `dotnet run --project Source/Motif.CLI -- <args>`
- Supported formats: `json`, `gp`, `gpif`
- Batch options: `--batch-input-dir`, `--batch-output-dir`,
  `--batch-roundtrip-diagnostics`
- Boolean flags support `--flag`, `--flag=true`, and `--flag=false`
- `--source-gp` is only valid when writing `.gp` files
- Detailed usage lives in `docs/CLI_WORKFLOW.md`

## Testing

Run the full suite with:

```bash
dotnet test
```

The test projects use `xunit.v3.mtp-v2` with `Microsoft.Testing.Platform`.

- Do not assume legacy `dotnet test --filter ...` examples will work.
- Targeted execution uses `--filter-query`, which has different syntax and should be
  verified before use.
- Running the full suite is the recommended default.

Test projects:

- `Motif.Core.UnitTests`
- `Motif.Extensions.GuitarPro.UnitTests`
- `Motif.IntegrationTests`

## Working In This Repo

- Before modifying code, read `DefaultScoreMapper` and `DefaultScoreUnmapper` completely
  and understand the read/write pipeline.
- Structural edits must go through `Track.Staves[staffIndex].Measures[measureIndex]`.
  There is no `Track.Measures` compatibility path.
- Navigation-affecting edits should update `Score.TimelineBars` and then call
  `ScoreNavigation.RebuildPlaybackSequence(score)`, or intentionally call
  `ScoreNavigation.InvalidatePlaybackSequence(score)`.
- JSON round-trips drop attached Guitar Pro extensions. When fidelity matters, use
  `ReattachGuitarProExtensionsFrom` or intentionally call
  `InvalidateGuitarProExtensions`.
- Keep `README.md`, `docs/CLI_WORKFLOW.md`, and `docs/LIBRARY_WORKFLOW.md` aligned with
  actual behavior when functionality changes.
