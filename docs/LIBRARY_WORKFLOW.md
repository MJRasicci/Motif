# Motif Library Workflow

This document covers the recommended application workflow when using `Motif` and
`Motif.Extensions.GuitarPro`.

## Read, Edit, Write

```csharp
using Motif;

var score = await MotifScore.OpenAsync("song.gp", cancellationToken: cancellationToken);

// Apply edits to the mapped score model.
score.Title = "Edited Title";

// Rebuild derived playback traversal after navigation-affecting edits.
ScoreNavigation.RebuildPlaybackSequence(score);

await MotifScore.SaveAsync(score, "song-edited.gp", cancellationToken);
```

`MotifScore` handles mapped JSON and native `.motif` archives directly, and discovers
extension handlers such as Guitar Pro from referenced Motif assemblies at runtime.
Current `.motif` archives always contain `manifest.json` and `score.json`, and they now
preserve namespaced `extensions/` and `resources/` entries so format packages can attach
supplementary archive data through the new contributor hook. Guitar Pro now uses that
hook to persist its raw metadata plus non-score archive files in
`extensions/guitarpro.json` and `resources/guitarpro/...`.

## When To Use Guitar Pro APIs Directly

Use `GuitarProReader` and `GuitarProWriter` directly, or resolve them through
`MotifScore.CreateReader("gp")` / `MotifScore.CreateWriter("gp")`, when you need
Guitar Pro-specific capabilities instead of the format-agnostic unified API:

- `WriteWithDiagnosticsAsync()` for `WriteDiagnostics`
- Advanced archive-seeding workflows such as the CLI `--source-gp` behavior
- Explicit control over raw GPIF read/write stages during debugging

## Destination Archive Behavior

`GuitarProWriter` writes a valid `.gp` archive in all cases.

- If the score carries restored Guitar Pro archive resources, such as a score opened from
  `.gp` or from a `.motif` archive that originated from `.gp`, the writer rebuilds the
  output archive from those preserved resources and replaces only `Content/score.gpif`.
- If the destination path does not exist, the writer uses the embedded default archive
  template and replaces `Content/score.gpif` when no restored resources are attached.
- If the destination path already exists and is a valid archive, the writer preserves the
  existing non-score entries and replaces only `Content/score.gpif` when no restored
  resources are attached.
- If you need to seed a new output path from a different source archive, use the CLI
  `--source-gp` workflow. That behavior is not exposed as a separate library API today.

## Derived Navigation State

`Score.PlaybackMasterBarSequence` is derived state.

- Call `ScoreNavigation.RebuildPlaybackSequence(score)` after edits that change playback
  traversal.
- Call `ScoreNavigation.InvalidatePlaybackSequence(score)` when you know the cached
  traversal is stale but do not want to rebuild yet.
- Call `ScoreNavigation.EnsurePlaybackSequence(score)` when consuming the cached traversal
  and you want stale data rebuilt on demand.

Typical traversal-affecting edits include:

- Changing `Score.Anacrusis`
- Adding, removing, or reordering measures
- Editing repeats, alternate endings, jump targets, or direction properties

## Timeline Editing

`Score.TimelineBars` is the score-owned master-bar timeline.

- The Guitar Pro reader populates it from source `MasterBar` data.
- `ScoreNavigation` and the Guitar Pro writer consume it as the canonical timeline source.
- `ScoreNavigation.EnsureTimelineBars(score)` returns the current timeline and guarantees a
  non-null list.
- Structural edits go through `Track.Staves[staffIndex].Measures[measureIndex]`; there is
  no `Track.Measures` compatibility path.
- Edit timeline-global state such as repeats, sections, jump targets, key changes, and
  fermatas through `Score.TimelineBars`.

Navigation no longer mirrors timeline state onto measures. Update `Score.TimelineBars`
directly, then rebuild playback traversal when those edits affect navigation.

## Guitar Pro Fidelity Workflow

Scores read from `.gp` carry Guitar Pro extensions that preserve raw-format fidelity where
possible. JSON round-trips do not include those extensions, so fidelity-sensitive edits
should choose one of these workflows deliberately.

### 1. Keep Attached Fidelity

Use this when editing an imported score in memory and you want the writer to reuse as much
source fidelity metadata as possible.

- Edit the core model directly
- Write the score
- Inspect writer diagnostics for fidelity warnings

### 2. Invalidate Before Regenerating

Use this when the edits intentionally break correspondence with the imported GP source and
you want the writer to regenerate from current domain state.

```csharp
score.InvalidateGuitarProExtensions();

// Apply edits that no longer correspond cleanly to the imported raw GP state.
ScoreNavigation.InvalidatePlaybackSequence(score);
```

The writer will emit `RawFidelity` diagnostics such as
`GP_SOURCE_FIDELITY_INVALIDATED`.

### 3. Reattach Source Fidelity

Use this when you round-trip through JSON or rebuild a score from some other process and
want to reuse matching GP fidelity metadata from a known source score.

```csharp
var sourceScore = await MotifScore.OpenAsync("song.gp", cancellationToken: cancellationToken);
var editedScore = /* JSON-deserialized or otherwise rebuilt score */;

var reattachment = editedScore.ReattachGuitarProExtensionsFrom(sourceScore);
```

- Reattachment matches by track id, staff index, measure index, voice index, beat id, note
  id, and timeline-bar index where applicable.
- The returned result reports how much source fidelity was reusable.
- Partial matches still produce valid output, but the writer may emit
  `GP_EXTENSION_REATTACHMENT_PARTIAL`.

## Diagnostics To Watch

Common `RawFidelity` warnings include:

- `GP_SOURCE_FIDELITY_INVALIDATED`
- `GP_EXTENSION_REATTACHMENT_PARTIAL`
- `GP_EXTENSION_GRAPH_PARTIAL`
- `TRACK_STAVES_XML_REGENERATED`
- `NOTE_STRING_FRET_REGENERATED`
- `NOTE_CONCERT_PITCH_REGENERATED`
- `NOTE_TRANSPOSED_PITCH_REGENERATED`
- `RHYTHM_SOURCE_SHAPE_REGENERATED`

These warnings mean the writer produced valid GP output, but some raw XML or reference
fidelity had to be regenerated instead of preserved exactly.
