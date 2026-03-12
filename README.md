# Motif

Motif is a .NET 10 music score library built around a mutable, format-agnostic `Score`
domain model. The current production extension is Guitar Pro support via
`Motif.Extensions.GuitarPro`, plus a companion CLI for inspection, conversion, and
round-trip diagnostics.

## Current Scope

- Read Guitar Pro `.gp` archives and extract `Content/score.gpif`
- Deserialize GPIF XML into a typed raw model
- Resolve GPIF references into a clean domain graph:
  `Score -> Tracks -> Staves -> StaffMeasures -> Voices -> Beats -> Notes`
- Preserve score-wide timeline and navigation state on `Score.TimelineBars`
- Rebuild derived playback order with `ScoreNavigation`
- Write edited scores back to `.gpif`, `.gp`, or native `.motif`
- Convert between `.gp`, `.gpif`, `.motif`, and mapped JSON with `motif-cli`

## Projects

| Project | Purpose |
| --- | --- |
| `Motif.Core` | Format-agnostic domain model, navigation helpers, and serialization helpers |
| `Motif.Extensions.GuitarPro` | Guitar Pro `.gp` / `.gpif` read-write support |
| `Motif` | Convenience package referencing Core and Guitar Pro support |
| `Motif.CLI` (`motif-cli`) | CLI for conversion, inspection, and batch diagnostics |

## Repository Layout

```text
Source/
  Motif/                             Convenience package
  Motif.Core/                        Core domain model and navigation helpers
  Motif.Extensions.GuitarPro/        Guitar Pro reader/writer, mapper, raw GPIF model
  Motif.CLI/                         CLI executable project
Tests/
  Motif.Core.UnitTests/              Core model, navigation, and serialization tests
  Motif.Extensions.GuitarPro.UnitTests/
                                     Guitar Pro mapping, writing, and round-trip tests
  Motif.IntegrationTests/            CLI integration and regression coverage
docs/
  CLI_WORKFLOW.md                    CLI usage and batch workflows
  LIBRARY_WORKFLOW.md                Recommended library edit/write workflow
```

## Library Quick Start

```csharp
using Motif;

var score = await MotifScore.OpenAsync("song.gp", cancellationToken: cancellationToken);

score.Title = "Edited Title";

// Rebuild derived playback state after navigation-affecting edits.
ScoreNavigation.RebuildPlaybackSequence(score);

await MotifScore.SaveAsync(score, "song-edited.gp", cancellationToken);
```

`MotifScore` handles mapped JSON and native `.motif` archives directly, and discovers
extension handlers such as Guitar Pro at runtime. `.motif` archives always contain
`manifest.json` and `score.json`, and now preserve namespaced `extensions/` and
`resources/` entries so format packages can round-trip supplementary data without the
core archive writer knowing format details. Guitar Pro now uses those locations to carry
raw GP metadata plus non-score archive files through `.gp -> .motif -> .gp` workflows.
Use `GuitarProWriter` directly, or resolve it through `MotifScore.CreateWriter("gp")`,
when you need Guitar Pro-specific write diagnostics or explicit source-archive control
such as the CLI `--source-gp` workflow.

## CLI Quick Start

Run during development:

```bash
dotnet run --project Source/Motif.CLI -- <args>
```

Common commands:

```bash
# Export mapped JSON (default output: song.mapped.json)
dotnet run --project Source/Motif.CLI -- song.gp

# Extract raw GPIF
dotnet run --project Source/Motif.CLI -- song.gp song.score.gpif

# Convert raw GPIF to mapped JSON
dotnet run --project Source/Motif.CLI -- song.gpif song.json

# Package a score as a native .motif archive
dotnet run --project Source/Motif.CLI -- song.gp song.motif

# Read a native .motif archive back to mapped JSON
dotnet run --project Source/Motif.CLI -- song.motif song.json

# Write a new .gp archive from mapped JSON
dotnet run --project Source/Motif.CLI -- song.json output.gp

# Preserve non-score archive entries from an existing source archive
dotnet run --project Source/Motif.CLI -- song.json output.gp --source-gp original.gp

# Batch export every .gp file under a directory to JSON
dotnet run --project Source/Motif.CLI -- \
  --batch-input-dir ./songs \
  --batch-output-dir ./json

# Batch round-trip diagnostics across a corpus
dotnet run --project Source/Motif.CLI -- \
  --batch-input-dir ./songs \
  --batch-output-dir ./analysis \
  --batch-roundtrip-diagnostics
```

Formats are inferred from file extensions when possible. Use `--input-format` and
`--output-format` when extensions are missing or ambiguous. Boolean flags follow the same
pattern everywhere: `--flag`, `--flag=true`, and `--flag=false`.

## Supported Formats

| Format | Read | Write | Notes |
| --- | --- | --- | --- |
| `gp` | Yes | Yes | Guitar Pro ZIP archive containing `Content/score.gpif` |
| `gpif` | Yes | Yes | Raw GPIF XML |
| `motif` | Yes | Yes | Native ZIP archive with `manifest.json`, `score.json`, and preserved namespaced extension/resource entries; GP-origin archives also preserve Guitar Pro metadata/resources |
| `json` | Yes | Yes | Mapped `Score` JSON, intended for editing and inspection |
| `musicxml` / `mxl` | No | No | Not part of the current CLI or library surface |
| `midi` | No | No | Not part of the current CLI or library surface |

The CLI intentionally rejects unsupported formats rather than silently routing them.

## Testing

Run the full test suite with:

```bash
dotnet test
```

Coverage includes real `.gp` fixtures, mapping fidelity, write diagnostics, public API
shape, and CLI regression tests.

## Documentation

- [docs/CLI_WORKFLOW.md](docs/CLI_WORKFLOW.md)
- [docs/LIBRARY_WORKFLOW.md](docs/LIBRARY_WORKFLOW.md)
- [AGENTS.md](AGENTS.md)

## License

[MIT](LICENSE.md)
