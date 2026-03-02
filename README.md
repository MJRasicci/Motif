# GPIO.NET

**Guitar Pro I/O for .NET — precise parsing, deterministic mapping, and a clean domain model.**

GPIO.NET is a production-grade .NET library for reading and transforming Guitar Pro (`.gp`) files.  
It extracts and interprets GPIF (`score.gpif`) data and exposes a strongly-typed, traversal-friendly object model for working with musical structures.

A companion CLI tool is included for inspection, conversion, and round-trip workflows.

---

## Purpose

Guitar Pro files are ZIP archives containing a GPIF XML document with a heavily reference-based structure.  
While flexible, this format is difficult to consume and modify directly.

GPIO.NET provides:

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
  - Measures (Bars)
  - Voices
  - Beats
  - Notes
  - Rhythms, articulations, and properties

### 2. Clean Domain Model

GPIF uses cross-referenced IDs and indirect relationships.

GPIO.NET transforms this into a natural object graph:

```
Score → Tracks → Measures → Voices → Beats → Notes
```

- Eliminates manual reference resolution
- Enables intuitive iteration and traversal
- Retains links to original metadata where needed

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

## CLI Tool

A companion CLI is provided for file conversion and inspection.

**Project:**

```
Source/GPIO.NET.Tool
```

**Run:**

```
dotnet run --project Source/GPIO.NET.Tool -- <input> [output] [options]
```

---

## Supported Workflows

### Convert `.gp` → JSON

```
dotnet run --project Source/GPIO.NET.Tool -- input.gp score.json --format json
```

### Extract raw GPIF

```
dotnet run --project Source/GPIO.NET.Tool -- input.gp score.gpif --format gpif
```

### Edit JSON → Patch existing `.gp` (recommended)

```
dotnet run --project Source/GPIO.NET.Tool -- score.json output.gp 
--from-json 
--patch-from-json 
--source-gp input.gp 
--format json
```

---

## Output Formats

| Format | Description | Status |
|------|--------|--------|
| `json` | Mapped domain model | ✅ |
| `gpif` | Raw GPIF XML | ✅ |
| `midi` | MIDI export | 🚧 Planned |

---

## CLI Options

### General

- `--format json|gpif|midi`
- `--out <path>`

### JSON Options

- `--json-indent[=true|false]`
- `--json-ignore-null[=true|false]`
- `--json-ignore-default[=true|false]`

### Write Modes

- `--from-json` — treat input as mapped JSON
- `--patch-from-json` — patch an existing `.gp` file (preferred)
- `--source-gp <path>` — original file for patching

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

- `Source/GPIO.NET` — Core library
- `Source/GPIO.NET.Tool` — CLI tool

---

## Project Status

This repository represents the **canonical, production-ready implementation**.

Earlier experiments and migration notes are documented in `AGENTS.md`.

---

## Roadmap

- MIDI export support
- Improved write/round-trip fidelity
- Expanded test corpus
- Performance tuning for large scores

---

## License

MIT — see `LICENSE.md`.

```
