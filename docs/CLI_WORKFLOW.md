# Motif CLI Workflow

Use `motif-cli` to convert between Guitar Pro archives, raw GPIF XML, native `.motif`
archives, and mapped JSON.

Run during development:

```bash
dotnet run --project Source/Motif.CLI -- <args>
```

## Default Routing

If you omit the output path, the CLI picks a default output format and file name:

- `song.gp` -> `song.mapped.json`
- `song.gpif` -> `song.mapped.json`
- `song.motif` -> `song.mapped.json`
- `song.json` -> `song.gp`
- `song.gp --output-format gpif` -> `song.score.gpif`

Formats are inferred from extensions when possible. Use `--input-format` and
`--output-format` when the file names do not make the route obvious. Standard
score reads and non-templated GP/GPIF writes use the same `MotifScore` handler
registry as the library API.

## Single-File Workflows

Export a `.gp` archive to editable mapped JSON:

```bash
dotnet run --project Source/Motif.CLI -- input.gp score.json
```

Extract raw GPIF from a `.gp` archive:

```bash
dotnet run --project Source/Motif.CLI -- input.gp score.gpif
```

Route raw GPIF through the mapped domain model:

```bash
dotnet run --project Source/Motif.CLI -- input.gpif score.json
dotnet run --project Source/Motif.CLI -- score.json output.gpif
```

Package a score as a native `.motif` archive:

```bash
dotnet run --project Source/Motif.CLI -- input.gp score.motif
dotnet run --project Source/Motif.CLI -- input.gpif score.motif
dotnet run --project Source/Motif.CLI -- score.json score.motif
```

Read a native `.motif` archive back to mapped JSON:

```bash
dotnet run --project Source/Motif.CLI -- score.motif score.json
```

Write a `.gp` archive from mapped JSON using the built-in default archive template:

```bash
dotnet run --project Source/Motif.CLI -- score.json output.gp
```

Write a `.gp` archive while preserving non-score archive payload from another file:

```bash
dotnet run --project Source/Motif.CLI -- score.json output.gp \
  --source-gp input.gp
```

Use explicit format routing when extensions are missing or custom:

```bash
dotnet run --project Source/Motif.CLI -- score.data output.data \
  --input-format json \
  --output-format gp
```

## Writer Diagnostics

Writer warnings are always printed to stdout for `.gp` and `.gpif` writes. To persist
them:

```bash
dotnet run --project Source/Motif.CLI -- score.json output.gp \
  --diagnostics-out write-diagnostics.json \
  --diagnostics-json
```

- `--diagnostics-out <path>` writes diagnostics to a file
- `--diagnostics-json` switches that file from plain text to JSON

If a write only produces info-level diagnostics, such as XML-equivalent GPIF byte drift,
`--diagnostics-out` still writes those entries even when the warning count is zero.

`--source-gp` is only valid for `.gp` output.
Current `.motif` archives always contain `manifest.json` and `score.json`, and Motif now
preserves namespaced `extensions/` and `resources/` entries so format-specific archive
data can survive `.motif` read/write cycles even before a contributor package is loaded.
For Guitar Pro sources, those entries now include raw GP metadata plus non-score archive
files, so `motif-cli song.motif output.gp` can reconstruct the full `.gp` archive
without `--source-gp`. `--source-gp` remains the explicit template override for JSON-only
workflows and other cases where no preserved GP archive payload is attached.
`.motif` manifests also record the imported source format and file name in
`manifest.sources`, including extensionless routes that rely on `--input-format`.
That also means you can edit `score.json` inside an existing `.motif` archive and export
back to `.gp` while keeping the preserved Guitar Pro archive payload, as long as the
archive's `extensions/` and `resources/` entries remain intact.

## Batch Export

Export every `.gp` file under a directory tree to mapped JSON:

```bash
dotnet run --project Source/Motif.CLI -- \
  --batch-input-dir ./songs \
  --batch-output-dir ./json
```

Extract GPIF for every `.gp` file:

```bash
dotnet run --project Source/Motif.CLI -- \
  --batch-input-dir ./songs \
  --batch-output-dir ./gpif \
  --output-format gpif
```

Mirror `.gp` archives to another tree without remapping:

```bash
dotnet run --project Source/Motif.CLI -- \
  --batch-input-dir ./songs \
  --batch-output-dir ./gp-copy \
  --output-format gp
```

Export every `.gp` file under a directory tree to native `.motif` archives:

```bash
dotnet run --project Source/Motif.CLI -- \
  --batch-input-dir ./songs \
  --batch-output-dir ./motif \
  --output-format motif
```

Batch mode options:

- `--continue-on-error[=true|false]` controls whether failed files stop the batch
- `--failure-log <path>` overrides the JSONL failure log path
- The default failure log is `<batch-output-dir>/batch-failures.jsonl`

Batch mode currently accepts Guitar Pro input only.

## Batch Round-Trip Diagnostics

Run a no-edit corpus pass that reads each `.gp`, routes it through mapped JSON and the GP
writer, and records drift diagnostics:

```bash
dotnet run --project Source/Motif.CLI -- \
  --batch-input-dir ./songs \
  --batch-output-dir ./analysis \
  --batch-roundtrip-diagnostics
```

This workflow writes:

- `batch-roundtrip-summary.json`
- `batch-roundtrip-summary.txt`
- `batch-file-results.jsonl`
- `batch-diagnostics.jsonl`
- `batch-failures.jsonl` when failures occur

You can also copy the summary to a custom path:

```bash
dotnet run --project Source/Motif.CLI -- \
  --batch-input-dir ./songs \
  --batch-output-dir ./analysis \
  --batch-roundtrip-diagnostics \
  --diagnostics-out ./analysis/summary.json \
  --diagnostics-json
```

Constraints:

- Batch round-trip diagnostics currently support Guitar Pro input only
- Output format must remain `json` for this mode
- Exit code `10` means the batch completed with one or more failures

## Notes

- Supported formats are `json`, `gp`, `gpif`, and `motif`
- MusicXML, MXL, and MIDI are not supported by the current CLI
- Boolean flags consistently support `--flag`, `--flag=true`, and `--flag=false`
- `--format` remains an alias for `--output-format`
- `--from-json` remains a compatibility alias for `--input-format json`
