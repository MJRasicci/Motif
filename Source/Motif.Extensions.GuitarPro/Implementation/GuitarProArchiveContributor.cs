namespace Motif.Extensions.GuitarPro.Implementation;

using Motif.Extensions.GuitarPro.Models;
using Motif.Extensions.GuitarPro.Serialization;
using Motif.Models;
using System.Text.Json;

internal sealed class GuitarProArchiveContributor : IArchiveContributor
{
    private const string ContributorNamespace = "guitarpro";
    private const string ExtensionEntryPath = "extensions/guitarpro.json";
    private const string ResourcePrefix = "resources/guitarpro/";

    public string ContributorKey => ContributorNamespace;

    public IReadOnlyList<ArchiveEntry> GetArchiveEntries(Score score)
    {
        ArgumentNullException.ThrowIfNull(score);

        var entries = new List<ArchiveEntry>();
        var state = CaptureState(score);
        if (state is not null)
        {
            entries.Add(new ArchiveEntry(
                ExtensionEntryPath,
                JsonSerializer.SerializeToUtf8Bytes(state, GpMotifArchiveJsonContext.Default.GpMotifArchiveState)));
        }

        var archiveResources = score.GetGuitarProArchiveResources();
        if (archiveResources is not null)
        {
            foreach (var resourceEntry in archiveResources.Entries.OrderBy(entry => entry.EntryPath, StringComparer.OrdinalIgnoreCase))
            {
                var normalizedPath = NormalizeResourcePath(resourceEntry.EntryPath);
                entries.Add(new ArchiveEntry(ResourcePrefix + normalizedPath, resourceEntry.Data));
            }
        }

        return entries;
    }

    public void RestoreFromArchive(Score score, IReadOnlyList<ArchiveEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(score);
        ArgumentNullException.ThrowIfNull(entries);

        ArchiveEntry? extensionEntry = null;
        var resourceEntries = new List<GpArchiveResourceEntry>();
        foreach (var entry in entries)
        {
            if (string.Equals(entry.EntryPath, ExtensionEntryPath, StringComparison.OrdinalIgnoreCase))
            {
                extensionEntry = entry;
                continue;
            }

            if (entry.EntryPath.StartsWith(ResourcePrefix, StringComparison.OrdinalIgnoreCase))
            {
                resourceEntries.Add(new GpArchiveResourceEntry(
                    entry.EntryPath[ResourcePrefix.Length..],
                    entry.Data));
            }
        }

        if (extensionEntry is not null)
        {
            RestoreState(score, extensionEntry);
        }

        if (resourceEntries.Count > 0)
        {
            score.SetExtension(new GpArchiveResourcesExtension
            {
                Entries = resourceEntries
                    .Select(entry => new GpArchiveResourceEntry(entry.EntryPath, entry.Data))
                    .ToArray()
            });
        }
    }

    private static string NormalizeResourcePath(string entryPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entryPath);

        var normalized = entryPath.Trim().Replace('\\', '/').TrimStart('/');
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidOperationException("Guitar Pro archive resources must have a non-empty path.");
        }

        if (string.Equals(normalized, "Content/score.gpif", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Guitar Pro archive resources must not include Content/score.gpif.");
        }

        return normalized;
    }

    private static GpMotifArchiveState? CaptureState(Score score)
    {
        var scoreExtension = score.GetGuitarPro();
        var fidelityState = score.GetGuitarProFidelityState();
        var timelineBars = score.TimelineBars
            .Select(CaptureTimelineBarState)
            .OfType<GpTimelineBarArchiveState>()
            .ToArray();
        var tracks = score.Tracks
            .Select(CaptureTrackState)
            .OfType<GpTrackArchiveState>()
            .ToArray();

        if (scoreExtension is null
            && fidelityState is null
            && timelineBars.Length == 0
            && tracks.Length == 0)
        {
            return null;
        }

        return new GpMotifArchiveState
        {
            Score = scoreExtension is null
                ? null
                : new GpScoreExtension
                {
                    Metadata = scoreExtension.Metadata,
                    MasterTrack = scoreExtension.MasterTrack
                },
            FidelityState = fidelityState is null
                ? null
                : new GpFidelityStateExtension
                {
                    HasSourceContext = fidelityState.HasSourceContext,
                    FidelityInvalidated = fidelityState.FidelityInvalidated,
                    LastReattachment = fidelityState.LastReattachment
                },
            TimelineBars = timelineBars,
            Tracks = tracks
        };
    }

    private static GpTimelineBarArchiveState? CaptureTimelineBarState(TimelineBar timelineBar)
    {
        var extension = timelineBar.GetGuitarPro();
        if (extension is null)
        {
            return null;
        }

        return new GpTimelineBarArchiveState
        {
            Index = timelineBar.Index,
            Metadata = extension.Metadata
        };
    }

    private static GpTrackArchiveState? CaptureTrackState(Track track)
    {
        var staves = track.Staves
            .Select(CaptureStaffState)
            .OfType<GpStaffArchiveState>()
            .ToArray();
        var extension = track.GetGuitarPro();
        if (extension is null && staves.Length == 0)
        {
            return null;
        }

        return new GpTrackArchiveState
        {
            TrackId = track.Id,
            Metadata = extension?.Metadata,
            Staves = staves
        };
    }

    private static GpStaffArchiveState? CaptureStaffState(Staff staff)
    {
        var measures = staff.Measures
            .Select(CaptureMeasureState)
            .OfType<GpMeasureArchiveState>()
            .ToArray();
        var extension = staff.GetGuitarPro();
        if (extension is null && measures.Length == 0)
        {
            return null;
        }

        return new GpStaffArchiveState
        {
            StaffIndex = staff.StaffIndex,
            Metadata = extension?.Metadata,
            Measures = measures
        };
    }

    private static GpMeasureArchiveState? CaptureMeasureState(StaffMeasure staffMeasure)
    {
        var voices = staffMeasure.Voices
            .Select(CaptureVoiceState)
            .OfType<GpVoiceArchiveState>()
            .ToArray();
        var beats = staffMeasure.Voices.Count == 0
            ? staffMeasure.Beats.Select(CaptureBeatState).OfType<GpBeatArchiveState>().ToArray()
            : [];
        var extension = staffMeasure.GetGuitarPro();
        if (extension is null && voices.Length == 0 && beats.Length == 0)
        {
            return null;
        }

        return new GpMeasureArchiveState
        {
            Index = staffMeasure.Index,
            Metadata = extension?.Metadata,
            Voices = voices,
            Beats = beats
        };
    }

    private static GpVoiceArchiveState? CaptureVoiceState(Voice voice)
    {
        var beats = voice.Beats
            .Select(CaptureBeatState)
            .OfType<GpBeatArchiveState>()
            .ToArray();
        var extension = voice.GetGuitarPro();
        if (extension is null && beats.Length == 0)
        {
            return null;
        }

        return new GpVoiceArchiveState
        {
            VoiceIndex = voice.VoiceIndex,
            Metadata = extension?.Metadata,
            Beats = beats
        };
    }

    private static GpBeatArchiveState? CaptureBeatState(Beat beat)
    {
        var notes = beat.Notes
            .Select(CaptureNoteState)
            .OfType<GpNoteArchiveState>()
            .ToArray();
        var extension = beat.GetGuitarPro();
        if (extension is null && notes.Length == 0)
        {
            return null;
        }

        return new GpBeatArchiveState
        {
            Id = beat.Id,
            Metadata = extension?.Metadata,
            Notes = notes
        };
    }

    private static GpNoteArchiveState? CaptureNoteState(Note note)
    {
        var extension = note.GetGuitarPro();
        if (extension is null)
        {
            return null;
        }

        return new GpNoteArchiveState
        {
            Id = note.Id,
            Metadata = extension.Metadata
        };
    }

    private static void RestoreState(Score score, ArchiveEntry extensionEntry)
    {
        var state = JsonSerializer.Deserialize(
                extensionEntry.Data.Span,
                GpMotifArchiveJsonContext.Default.GpMotifArchiveState)
            ?? throw new InvalidDataException("Unable to deserialize extensions/guitarpro.json from the .motif archive.");

        if (state.Score is not null)
        {
            score.SetExtension(new GpScoreExtension
            {
                Metadata = state.Score.Metadata,
                MasterTrack = state.Score.MasterTrack
            });
        }

        if (state.FidelityState is not null)
        {
            score.SetExtension(new GpFidelityStateExtension
            {
                HasSourceContext = state.FidelityState.HasSourceContext,
                FidelityInvalidated = state.FidelityState.FidelityInvalidated,
                LastReattachment = state.FidelityState.LastReattachment
            });
        }

        RestoreTimelineBarStates(score, state.TimelineBars);
        RestoreTrackStates(score, state.Tracks);
    }

    private static void RestoreTimelineBarStates(Score score, IReadOnlyList<GpTimelineBarArchiveState> timelineBars)
    {
        if (timelineBars.Count == 0)
        {
            return;
        }

        var timelineBarStatesByIndex = timelineBars
            .Where(state => state.Metadata is not null)
            .GroupBy(state => state.Index)
            .ToDictionary(group => group.Key, group => group.First());

        foreach (var timelineBar in score.TimelineBars)
        {
            if (!timelineBarStatesByIndex.TryGetValue(timelineBar.Index, out var state) || state.Metadata is null)
            {
                continue;
            }

            timelineBar.SetExtension(new GpTimelineBarExtension
            {
                Metadata = state.Metadata
            });
        }
    }

    private static void RestoreTrackStates(Score score, IReadOnlyList<GpTrackArchiveState> tracks)
    {
        if (tracks.Count == 0)
        {
            return;
        }

        var trackStatesById = tracks
            .GroupBy(state => state.TrackId)
            .ToDictionary(group => group.Key, group => group.First());

        foreach (var track in score.Tracks)
        {
            if (!trackStatesById.TryGetValue(track.Id, out var state))
            {
                continue;
            }

            if (state.Metadata is not null)
            {
                track.SetExtension(new GpTrackExtension
                {
                    Metadata = state.Metadata
                });
            }

            RestoreStaffStates(track, state.Staves);
        }
    }

    private static void RestoreStaffStates(Track track, IReadOnlyList<GpStaffArchiveState> staffStates)
    {
        if (staffStates.Count == 0)
        {
            return;
        }

        var staffStatesByIndex = staffStates
            .GroupBy(state => state.StaffIndex)
            .ToDictionary(group => group.Key, group => group.First());

        foreach (var staff in track.Staves)
        {
            if (!staffStatesByIndex.TryGetValue(staff.StaffIndex, out var staffState))
            {
                continue;
            }

            if (staffState.Metadata is not null)
            {
                staff.SetExtension(new GpStaffExtension
                {
                    Metadata = staffState.Metadata
                });
            }

            RestoreMeasureStates(staff, staffState.Measures);
        }
    }

    private static void RestoreMeasureStates(Staff staff, IReadOnlyList<GpMeasureArchiveState> measureStates)
    {
        if (measureStates.Count == 0)
        {
            return;
        }

        var measureStatesByIndex = measureStates
            .GroupBy(state => state.Index)
            .ToDictionary(group => group.Key, group => group.First());

        foreach (var measure in staff.Measures)
        {
            if (!measureStatesByIndex.TryGetValue(measure.Index, out var measureState))
            {
                continue;
            }

            if (measureState.Metadata is not null)
            {
                measure.SetExtension(new GpMeasureStaffExtension
                {
                    Metadata = measureState.Metadata
                });
            }

            RestoreBeatStates(measure.Beats, measureState.Beats);
            RestoreVoiceStates(measure.Voices, measureState.Voices);
        }
    }

    private static void RestoreVoiceStates(IReadOnlyList<Voice> voices, IReadOnlyList<GpVoiceArchiveState> voiceStates)
    {
        if (voiceStates.Count == 0)
        {
            return;
        }

        var voiceStatesByIndex = voiceStates
            .GroupBy(state => state.VoiceIndex)
            .ToDictionary(group => group.Key, group => group.First());

        foreach (var voice in voices)
        {
            if (!voiceStatesByIndex.TryGetValue(voice.VoiceIndex, out var voiceState))
            {
                continue;
            }

            if (voiceState.Metadata is not null)
            {
                voice.SetExtension(new GpVoiceExtension
                {
                    Metadata = voiceState.Metadata
                });
            }

            RestoreBeatStates(voice.Beats, voiceState.Beats);
        }
    }

    private static void RestoreBeatStates(IReadOnlyList<Beat> beats, IReadOnlyList<GpBeatArchiveState> beatStates)
    {
        if (beatStates.Count == 0 || beats.Count == 0)
        {
            return;
        }

        var beatStatesById = BuildItemsByIdQueue(beatStates, static state => state.Id);
        foreach (var beat in beats)
        {
            if (!TryDequeueMatchingItem(beatStatesById, beat.Id, out var beatState))
            {
                continue;
            }

            if (beatState.Metadata is not null)
            {
                beat.SetExtension(new GpBeatExtension
                {
                    Metadata = beatState.Metadata
                });
            }

            RestoreNoteStates(beat.Notes, beatState.Notes);
        }
    }

    private static void RestoreNoteStates(IReadOnlyList<Note> notes, IReadOnlyList<GpNoteArchiveState> noteStates)
    {
        if (noteStates.Count == 0 || notes.Count == 0)
        {
            return;
        }

        var noteStatesById = BuildItemsByIdQueue(noteStates, static state => state.Id);
        foreach (var note in notes)
        {
            if (!TryDequeueMatchingItem(noteStatesById, note.Id, out var noteState) || noteState.Metadata is null)
            {
                continue;
            }

            note.SetExtension(new GpNoteExtension
            {
                Metadata = noteState.Metadata
            });
        }
    }

    private static Dictionary<int, Queue<TItem>> BuildItemsByIdQueue<TItem>(
        IReadOnlyList<TItem> items,
        Func<TItem, int> idSelector)
    {
        var itemsById = new Dictionary<int, Queue<TItem>>();
        foreach (var item in items)
        {
            var id = idSelector(item);
            if (!itemsById.TryGetValue(id, out var queue))
            {
                queue = new Queue<TItem>();
                itemsById[id] = queue;
            }

            queue.Enqueue(item);
        }

        return itemsById;
    }

    private static bool TryDequeueMatchingItem<TItem>(
        Dictionary<int, Queue<TItem>> itemsById,
        int id,
        out TItem item)
    {
        if (itemsById.TryGetValue(id, out var queue) && queue.Count > 0)
        {
            item = queue.Dequeue();
            return true;
        }

        item = default!;
        return false;
    }
}
