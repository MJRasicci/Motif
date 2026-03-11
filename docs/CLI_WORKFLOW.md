# Motif CLI Workflow

## 1) Export a GP file to editable mapped JSON

```bash
dotnet run --project Source/Motif.CLI -- input.gp score.json
```

## 2) Edit `score.json`

Edit the mapped score JSON and then write it back out through the full roundtrip writer.

## 3) Full rewrite while preserving archive payload

```bash
dotnet run --project Source/Motif.CLI -- score.json output.gp \
  --source-gp input.gp \
  --diagnostics-out write-diagnostics.json --diagnostics-json
```

Use this when you want a full unmap/serialize write, but keep non-`score.gpif` zip entries
from an existing `.gp` archive:

## 4) Full rewrite without source GP

Use this for non-GP-originated scores. The writer seeds a default empty archive payload
(`VERSION`, `meta.json`, preferences, stylesheets, score views) and replaces `Content/score.gpif`.

```bash
dotnet run --project Source/Motif.CLI -- score.json output.gp \
  --output-format gp
```

## 5) Extract raw GPIF

```bash
dotnet run --project Source/Motif.CLI -- input.gp score.gpif
```

## Notes

- Formats are inferred from file extensions when possible.
- Use `--input-format` / `--output-format` when extensions are missing or ambiguous.
- `--format` remains an alias for `--output-format`.
- `--from-json` remains supported as a compatibility alias for `--input-format json`.
- Supported end-to-end formats are `json`, `gp`, and `gpif`.
- MusicXML/MXL/MIDI are not part of the v1 CLI surface.
