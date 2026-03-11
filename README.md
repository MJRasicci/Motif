# Motif

**A format-agnostic .NET music domain model and conversion library.**

Motif provides a clean, strongly-typed domain model for representing and modifying musical data programmatically. Format-specific extension packages handle lossless or near-lossless round-tripping for supported formats.

The initial release ships with Guitar Pro (`.gp`) support via `Motif.Extensions.GuitarPro`.

A companion CLI tool is included for inspection, conversion, and round-trip workflows.

---

## Purpose

Guitar Pro files are ZIP archives containing a GPIF XML document with a heavily reference-based structure.
While flexible, this format is difficult to consume and modify directly.

Motif provides:

- **Deterministic parsing** of `.gp` archives and GPIF data
- **Reference resolution** into a usable in-memory graph
- **A clean, hierarchical domain model** for musical traversal
- **Safe handling of malformed or partial data**
- **A CLI tool** for rapid inspection and conversion workflows

---

## Core Concepts

### 1. Accurate Parsing

- Safely opens `.gp` archives
- Extracts and deserializes `Content/score.gpif`
- Preserves full musical semantics:
  - Tracks
  - Staves
  - Staff measures (Bars)
  - Voices
  - Beats
  - Notes
  - Score timeline bars
  - Rhythms, articulations, and properties

### 2. Clean Domain Model

GPIF uses cross-referenced IDs and indirect relationships.

Motif transforms this into a natural object graph:

```
Score -> Tracks -> Staves -> StaffMeasures -> Voices -> Beats -> Notes
```

- Eliminates manual reference resolution
- Enables intuitive iteration and traversal
- Retains links to original metadata where needed
- Keeps timeline-global playback/navigation state on score-owned `TimelineBars`

### 3. Deterministic Mapping Layer

An intermediate mapping/index stage:

- Resolves all GPIF references
- Ensures consistent object identity
- Handles:
  - Missing references
  - Optional sections
  - Partially malformed files

This layer is the backbone of correctness.

### 4. Production-Ready Design

- Test-driven against real-world `.gp` files
- Stable, versioned API surface
- Clear separation of concerns:
  - **Core library uses no dependencies except .NET 10**
- Designed for embedding in:
  - Analysis tools
  - Converters
  - DAWs or notation systems

---

## Package Structure

| Package | Description |
|---|---|
| `Motif.Core` | Format-agnostic domain model and abstractions |
| `Motif.Extensions.GuitarPro` | Guitar Pro `.gp` read/write support |
| `Motif` | Convenience wrapper referencing Core + GuitarPro |

---

## CLI Tool

A companion CLI is provided for file conversion and inspection.

**Project:**

```
Source/Motif.CLI
```

**Run:**

```
dotnet run --project Source/Motif.CLI -- <input> [output] [options]
```

Formats are inferred from file extensions when possible. Use `--input-format` /
`--output-format` when extensions are missing or ambiguous.

---

## Supported Workflows

### Convert `.gp` -> JSON

```
dotnet run --project Source/Motif.CLI -- input.gp score.json
```

### Extract raw GPIF

```
dotnet run --project Source/Motif.CLI -- input.gp score.gpif
```

### Edit JSON -> Write back to `.gp`

```
dotnet run --project Source/Motif.CLI -- score.json output.gp \
  --source-gp input.gp
```

---

## Output Formats

| Format | Description | Status |
|------|--------|--------|
| `json` | Mapped domain model | Supported |
| `gp` | Guitar Pro archive read/write | Supported |
| `gpif` | Raw GPIF XML | Supported |
| `musicxml` / `mxl` | MusicXML import/export | Planned post-v1 |
| `midi` | MIDI export | Planned post-v1 |

---

## CLI Options

### General

- `--input-format json|gp|gpif`
- `--output-format json|gp|gpif`
- `--format json|gp|gpif` (alias for `--output-format`)
- `--out <path>`

### JSON Options

- `--json-indent[=true|false]`
- `--json-ignore-null[=true|false]`
- `--json-ignore-default[=true|false]`

### Write Modes

- `--from-json` — compatibility alias for `--input-format json`
- `--source-gp <path>` — original file for preserving archive payload

### Diagnostics

- `--diagnostics-out <path>`
- `--diagnostics-json`

---

## Design Principles

- **Correctness over convenience** — musical semantics must be preserved
- **Determinism** — identical input produces identical object graphs
- **Separation of concerns** — parsing, mapping, and output are distinct layers
- **Ergonomics** — consumers should never deal with GPIF reference mechanics

---

## Non-Goals

The core library intentionally does **not** include:

- UI or web application layers
- Database or persistence concerns
- Hardcoded environment-specific paths

---

## Repository Structure

- `Source/Motif.Core` — Core library
- `Source/Motif.Extensions.GuitarPro` — Guitar Pro format support
- `Source/Motif` — Convenience wrapper package
- `Source/Motif.CLI` — CLI tool

---

## Project Status

This repository represents the **canonical, production-ready implementation**.

See `docs/v1_Todo.md` for the v1 release plan.

---

## Roadmap

- MIDI export support
- MusicXML format support via `Motif.Extensions.MusicXml`
- Improved write/round-trip fidelity
- Expanded test corpus
- Performance tuning for large scores

---

## License

[MIT](LICENSE.md)
MIT — see `LICENSE.md`.
