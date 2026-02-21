# GPIO.NET CLI Workflow

## 1) Export a GP file to editable mapped JSON

```bash
dotnet run --project Source/GPIO.NET.Tool -- input.gp score.json --format json
```

## 2) Edit `score.json`

Typical safe edits currently supported:
- existing note pitch changes
- articulation flag changes (let ring, palm mute, muted, hopo, slide flags)
- note insertions into existing beats
- note reordering within a beat
- beat insertion/appends
- note and beat deletions

## 3) Plan patch only (optional)

```bash
dotnet run --project Source/GPIO.NET.Tool -- score.json patch-plan.json \
  --from-json --patch-from-json --source-gp input.gp --format json --plan-only
```

## 4) Apply patch to create output GP

```bash
dotnet run --project Source/GPIO.NET.Tool -- score.json output.gp \
  --from-json --patch-from-json --source-gp input.gp --format json \
  --diagnostics-out patch-diagnostics.json --diagnostics-json
```

## Strict mode

Fail fast if planner detects unsupported edits:

```bash
--strict
```

## Single-file publish (Windows x64)

```powershell
./scripts/publish-win-x64.ps1
```

Output location:
- `artifacts/publish/GPIO.NET.Tool/release_win-x64/`
