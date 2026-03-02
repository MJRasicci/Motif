# AGENTS.md — GPIO.NET Guidance

This is the canonical production repository for the GPIO.NET Guitar Pro I/O library and companion CLI.

---

## What This Repo Does

GPIO.NET reads and writes Guitar Pro (`.gp`) files — ZIP archives containing a GPIF XML document
with a heavily reference-based structure. The library:

1. Opens `.gp` archives and extracts `Content/score.gpif`
2. Deserializes GPIF XML into a typed raw model
3. Resolves all cross-references into a clean, navigable domain model
4. Writes/patches `.gp` archives from an edited domain model

---

## Repository Structure

```
Source/
  GPIO.NET/              Core library (no dependencies beyond .NET 10)
  GPIO.NET.Tool/         CLI tool (published as GPIO executable)

Tests/
  GPIO.NET.UnitTests/    Fixture-based unit + integration tests

docs/
  GPIF_COVERAGE_STATUS.md   What is typed / hybrid / missing in the GPIF mapping
  MAPPING_COVERAGE.md       Domain model coverage checklist
  CLI_WORKFLOW.md           Common CLI workflows
```

---

## Architecture: The Processing Pipeline

Every read flows through three explicit layers:

```
.gp file
  └─► IGpArchiveReader      → opens ZIP, exposes score.gpif stream
  └─► IGpifDeserializer     → deserializes XML into GpifDocument (raw model)
  └─► IScoreMapper          → resolves references → GuitarProScore (domain model)
```

Every write flows in reverse:

```
GuitarProScore (edited)
  └─► IScoreUnmapper        → produces GpifDocument from domain model
  └─► IGpifSerializer       → serializes to XML
  └─► IGpArchiveWriter      → writes ZIP archive
```

Patch mode is a shortcut that diffs an edited `GuitarProScore` against a source `.gp` and
produces a minimal `GpPatchDocument`, then applies it via `GuitarProPatcher`.

### Key entry points

| Class | Purpose |
|---|---|
| `GuitarProReader` | Top-level read API |
| `GuitarProWriter` | Top-level write API |
| `GuitarProPatcher` | Applies a `GpPatchDocument` to an existing `.gp` |
| `DefaultScoreMapper` | Raw GPIF → domain model |
| `DefaultScoreUnmapper` | Domain model → raw GPIF |
| `XmlGpifDeserializer` | XML → `GpifDocument` |
| `XmlGpifSerializer` | `GpifDocument` → XML |

---

## Models

### Raw model (`Models/Raw/`)
Faithful XML representation. Maps 1:1 to GPIF elements.
Hybrid fields use typed core + raw XML passthrough for elements not yet fully normalized.

### Domain model (`Models/`)
`GuitarProScore` — clean, navigable object graph:
```
Score → Tracks → Measures → Voices → Beats → Notes
```
Consumers should never need to deal with GPIF reference mechanics.

### Patch model (`Models/Patching/`)
`GpPatchDocument` — a list of typed patch operations.
`JsonPatchPlanResult` — output of the `JsonPatchPlanner` (planner result + unsupported changes).

---

## CLI Tool

The tool is published as a single-file self-contained executable named `GPIO`.

**Run during development:**
```
dotnet run --project Source/GPIO.NET.Tool -- <args>
```

**Publish (Windows x64):**
```powershell
dotnet publish Source/GPIO.NET.Tool -r win-x64 -c Release
```

Output: `artifacts/publish/GPIO.NET.Tool/release_win-x64/`

For full CLI usage, run `gpio --help` or see [docs/CLI_WORKFLOW.md](docs/CLI_WORKFLOW.md).

**Supported output formats:** `json`, `gpif`, `musicxml` (planned), `midi` (planned)

---

## Testing

Tests live in `Tests/GPIO.NET.UnitTests/` and cover:

- End-to-end reads against real `.gp` fixture files
- Articulation and rhythm mapping
- Navigation resolver (repeat/alternate ending sequences)
- Write path (round-trip fidelity, writer diagnostics, articulation parity)
- Patch path (note insertion, deletion, reorder, pitch/articulation updates)
- Public API surface shape

Run all tests:
```
dotnet test
```

Always add or update tests alongside behavior changes.

---

## Design Principles

- **Correctness over convenience** — musical semantics must be preserved exactly
- **Determinism** — identical input → identical output; no hidden state
- **Separation of concerns** — parsing, mapping, unmapping, and output are distinct pipeline stages
- **Ergonomics** — consumers operate on `GuitarProScore`, never on raw GPIF references
- **Library-first** — the core library has no host-specific dependencies (no web, EF, DI frameworks)

---

## Current Status and Gaps

See [docs/GPIF_COVERAGE_STATUS.md](docs/GPIF_COVERAGE_STATUS.md) for the detailed breakdown.

Highest-priority open areas:
1. DS/DC/Coda/Fine full notation-engine semantics in `DefaultNavigationResolver`
2. Deeper normalization of audio engine / MIDI connection / lyrics (currently passthrough)
3. Advanced patch planner: new tracks/measures, structural diffs beyond note/beat edits
4. Schema-driven coverage audit against the GPIF XSD

---

## Working in This Repo

- Read `docs/GPIF_COVERAGE_STATUS.md` before touching mapping or write-path code
- Keep the raw model and domain model in sync — changes to one typically require changes to both
- The `DefaultScoreMapper` and `DefaultScoreUnmapper` are the most complex files; read them fully before editing
- Patch operations are defined in `Models/Patching/GpPatchDocument.cs` — extend there first before touching the patcher
- Boolean CLI flags support `--flag`, `--flag=true`, `--flag=false` — keep new flags consistent with this pattern
