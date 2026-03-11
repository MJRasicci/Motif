# Motif Library Workflow

This document covers the recommended edit workflow when using `Motif.Core` and
`Motif.Extensions.GuitarPro` together in application code.

## Read, Edit, Write

```csharp
using Motif;
using Motif.Extensions.GuitarPro;

var reader = new GuitarProReader();
var writer = new GuitarProWriter();

var score = await reader.ReadAsync("song.gp", cancellationToken: cancellationToken);

// Apply edits to the Core score model.
score.Title = "Edited Title";

// Rebuild derived playback traversal after navigation-affecting edits.
ScoreNavigation.RebuildPlaybackSequence(score);

var diagnostics = await writer.WriteWithDiagnosticsAsync(
    score,
    "song-edited.gp",
    cancellationToken);
```

## Derived Navigation State

`Score.PlaybackMasterBarSequence` is derived state.

- Call `ScoreNavigation.RebuildPlaybackSequence(score)` after edits that change playback traversal.
- Call `ScoreNavigation.InvalidatePlaybackSequence(score)` when you know the cached traversal is stale but are not ready to rebuild yet.
- Call `ScoreNavigation.EnsurePlaybackSequence(score)` when consuming the cached traversal and you want stale data to be rebuilt on demand.

Typical traversal-affecting edits include:

- Changing `Score.Anacrusis`
- Adding, removing, or reordering measures
- Editing repeat starts/ends, alternate endings, jump targets, or direction properties

## Score Timeline

`Score.TimelineBars` is the score-owned master-bar timeline.

- The Guitar Pro reader populates it from source `MasterBar` data.
- `ScoreNavigation` and the Guitar Pro writer consume it as the canonical timeline source.
- `ScoreNavigation.EnsureTimelineBars(score)` returns the current score-owned timeline when you need a non-null list.
- Structural edits now flow through `Track.Staves[staffIndex].Measures[measureIndex]`; there is no compatibility `Track.Measures` path anymore.
- Prefer editing timeline-global state such as repeats, sections, jump targets, key changes, and fermatas through `Score.TimelineBars`.

Navigation no longer mirrors or promotes measure-local timeline fields. Update `Score.TimelineBars`
directly, then rebuild playback traversal when those edits affect navigation.

## Guitar Pro Fidelity Workflow

Scores read from `.gp` carry Guitar Pro extensions that preserve raw-format fidelity where possible.

When edits can invalidate that source fidelity, choose one of these workflows deliberately:

### 1. Keep Attached Fidelity

Use this when editing an in-memory score that still retains its imported GP extensions.

- Edit the Core model directly.
- Write the score.
- Inspect writer diagnostics for any fidelity warnings.

### 2. Invalidate Before Regenerating

Use this when your edits intentionally discard raw GP fidelity and you want the writer to regenerate from Core state.

```csharp
score.InvalidateGuitarProExtensions();

// Apply edits that no longer correspond cleanly to the imported raw GP state.
ScoreNavigation.InvalidatePlaybackSequence(score);
```

The writer will emit `RawFidelity` diagnostics such as `GP_SOURCE_FIDELITY_INVALIDATED`.

### 3. Reattach Source Fidelity

Use this when you round-trip through Core-only JSON or construct an edited score from a known source score and want to reuse matching GP fidelity where possible.

```csharp
var sourceScore = await reader.ReadAsync("song.gp", cancellationToken: cancellationToken);
var editedScore = /* JSON-deserialized or otherwise rebuilt score */;

var reattachment = editedScore.ReattachGuitarProExtensionsFrom(sourceScore);
```

- `ReattachGuitarProExtensionsFrom` matches by track id, staff index, measure index, voice index, beat id, and note id where applicable.
- The returned result reports how much of the source fidelity was reusable.
- If reattachment is partial, the writer will emit `GP_EXTENSION_REATTACHMENT_PARTIAL`.

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

These warnings mean the writer produced a valid GP output, but some raw XML or reference fidelity had to be regenerated instead of preserved exactly.
