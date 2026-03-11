namespace Motif.Extensions.GuitarPro;

using Motif.Extensions.GuitarPro.Models;
using Motif.Models;

public static class GuitarProModelExtensions
{
    public static GpScoreExtension? GetGuitarPro(this Score score)
    {
        ArgumentNullException.ThrowIfNull(score);

        return score.GetExtension<GpScoreExtension>();
    }

    public static GpScoreExtension GetRequiredGuitarPro(this Score score)
    {
        ArgumentNullException.ThrowIfNull(score);

        return score.GetRequiredExtension<GpScoreExtension>();
    }

    public static GpScoreExtension GetOrCreateGuitarPro(this Score score)
    {
        ArgumentNullException.ThrowIfNull(score);

        var extension = score.GetGuitarPro();
        if (extension is not null)
        {
            return extension;
        }

        extension = new GpScoreExtension
        {
            Metadata = new ScoreMetadata(),
            MasterTrack = new MasterTrackMetadata()
        };
        score.SetExtension(extension);
        return extension;
    }

    public static GpTrackExtension? GetGuitarPro(this Track track)
    {
        ArgumentNullException.ThrowIfNull(track);

        return track.GetExtension<GpTrackExtension>();
    }

    public static GpTrackExtension GetRequiredGuitarPro(this Track track)
    {
        ArgumentNullException.ThrowIfNull(track);

        return track.GetRequiredExtension<GpTrackExtension>();
    }

    public static GpTrackExtension GetOrCreateGuitarPro(this Track track)
    {
        ArgumentNullException.ThrowIfNull(track);

        var extension = track.GetGuitarPro();
        if (extension is not null)
        {
            return extension;
        }

        extension = new GpTrackExtension
        {
            Metadata = new TrackMetadata()
        };
        track.SetExtension(extension);
        return extension;
    }

    public static GpTimelineBarExtension? GetGuitarPro(this TimelineBar timelineBar)
    {
        ArgumentNullException.ThrowIfNull(timelineBar);

        return timelineBar.GetExtension<GpTimelineBarExtension>();
    }

    public static GpTimelineBarExtension GetRequiredGuitarPro(this TimelineBar timelineBar)
    {
        ArgumentNullException.ThrowIfNull(timelineBar);

        return timelineBar.GetRequiredExtension<GpTimelineBarExtension>();
    }

    public static GpTimelineBarExtension GetOrCreateGuitarPro(this TimelineBar timelineBar)
    {
        ArgumentNullException.ThrowIfNull(timelineBar);

        var extension = timelineBar.GetGuitarPro();
        if (extension is not null)
        {
            return extension;
        }

        extension = new GpTimelineBarExtension
        {
            Metadata = new GpTimelineBarMetadata()
        };
        timelineBar.SetExtension(extension);
        return extension;
    }

    public static GpStaffExtension? GetGuitarPro(this Staff staff)
    {
        ArgumentNullException.ThrowIfNull(staff);

        return staff.GetExtension<GpStaffExtension>();
    }

    public static GpStaffExtension GetRequiredGuitarPro(this Staff staff)
    {
        ArgumentNullException.ThrowIfNull(staff);

        return staff.GetRequiredExtension<GpStaffExtension>();
    }

    public static GpStaffExtension GetOrCreateGuitarPro(this Staff staff)
    {
        ArgumentNullException.ThrowIfNull(staff);

        var extension = staff.GetGuitarPro();
        if (extension is not null)
        {
            return extension;
        }

        extension = new GpStaffExtension
        {
            Metadata = new StaffMetadata()
        };
        staff.SetExtension(extension);
        return extension;
    }

    public static GpMeasureStaffExtension? GetGuitarPro(this StaffMeasure staffMeasure)
    {
        ArgumentNullException.ThrowIfNull(staffMeasure);

        return staffMeasure.GetExtension<GpMeasureStaffExtension>();
    }

    public static GpMeasureStaffExtension GetRequiredGuitarPro(this StaffMeasure staffMeasure)
    {
        ArgumentNullException.ThrowIfNull(staffMeasure);

        return staffMeasure.GetRequiredExtension<GpMeasureStaffExtension>();
    }

    public static GpMeasureStaffExtension GetOrCreateGuitarPro(this StaffMeasure staffMeasure)
    {
        ArgumentNullException.ThrowIfNull(staffMeasure);

        var extension = staffMeasure.GetGuitarPro();
        if (extension is not null)
        {
            return extension;
        }

        extension = new GpMeasureStaffExtension
        {
            Metadata = new GpMeasureStaffMetadata()
        };
        staffMeasure.SetExtension(extension);
        return extension;
    }

    public static GpVoiceExtension? GetGuitarPro(this Voice voice)
    {
        ArgumentNullException.ThrowIfNull(voice);

        return voice.GetExtension<GpVoiceExtension>();
    }

    public static GpVoiceExtension GetRequiredGuitarPro(this Voice voice)
    {
        ArgumentNullException.ThrowIfNull(voice);

        return voice.GetRequiredExtension<GpVoiceExtension>();
    }

    public static GpVoiceExtension GetOrCreateGuitarPro(this Voice voice)
    {
        ArgumentNullException.ThrowIfNull(voice);

        var extension = voice.GetGuitarPro();
        if (extension is not null)
        {
            return extension;
        }

        extension = new GpVoiceExtension
        {
            Metadata = new GpVoiceMetadata()
        };
        voice.SetExtension(extension);
        return extension;
    }

    public static GpBeatExtension? GetGuitarPro(this Beat beat)
    {
        ArgumentNullException.ThrowIfNull(beat);

        return beat.GetExtension<GpBeatExtension>();
    }

    public static GpBeatExtension GetRequiredGuitarPro(this Beat beat)
    {
        ArgumentNullException.ThrowIfNull(beat);

        return beat.GetRequiredExtension<GpBeatExtension>();
    }

    public static GpBeatExtension GetOrCreateGuitarPro(this Beat beat)
    {
        ArgumentNullException.ThrowIfNull(beat);

        var extension = beat.GetGuitarPro();
        if (extension is not null)
        {
            return extension;
        }

        extension = new GpBeatExtension
        {
            Metadata = new GpBeatMetadata()
        };
        beat.SetExtension(extension);
        return extension;
    }

    public static GpNoteExtension? GetGuitarPro(this Note note)
    {
        ArgumentNullException.ThrowIfNull(note);

        return note.GetExtension<GpNoteExtension>();
    }

    public static GpNoteExtension GetRequiredGuitarPro(this Note note)
    {
        ArgumentNullException.ThrowIfNull(note);

        return note.GetRequiredExtension<GpNoteExtension>();
    }

    public static GpNoteExtension GetOrCreateGuitarPro(this Note note)
    {
        ArgumentNullException.ThrowIfNull(note);

        var extension = note.GetGuitarPro();
        if (extension is not null)
        {
            return extension;
        }

        extension = new GpNoteExtension
        {
            Metadata = new GpNoteMetadata()
        };
        note.SetExtension(extension);
        return extension;
    }

    public static void InvalidateGuitarProExtensions(this Score score)
    {
        ArgumentNullException.ThrowIfNull(score);

        var removedAny = score.RemoveExtension<GpScoreExtension>();
        removedAny |= InvalidateTimelineBarExtensions(score);

        foreach (var track in score.Tracks)
        {
            removedAny |= track.RemoveExtension<GpTrackExtension>();
            removedAny |= InvalidateTrackStaffExtensions(track);
        }

        var fidelityState = score.GetExtension<GpFidelityStateExtension>();
        if (removedAny || fidelityState?.HasSourceContext == true)
        {
            fidelityState ??= new GpFidelityStateExtension();
            fidelityState.HasSourceContext = true;
            fidelityState.FidelityInvalidated = true;
            fidelityState.LastReattachment = null;
            score.SetExtension(fidelityState);
        }
    }

    public static GpExtensionReattachmentResult ReattachGuitarProExtensionsFrom(this Score target, Score source)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(source);

        if (!ReferenceEquals(target, source))
        {
            target.InvalidateGuitarProExtensions();
        }

        var result = new GpExtensionReattachmentResult();
        var scoreExtension = source.GetGuitarPro();
        if (scoreExtension is not null)
        {
            target.SetExtension(new GpScoreExtension
            {
                Metadata = scoreExtension.Metadata,
                MasterTrack = scoreExtension.MasterTrack
            });

            result.ScoreAttached = true;
        }
        else
        {
            result.ScoreUnmatched = true;
        }

        ReattachTimelineBarExtensions(target, source, result);

        var sourceTracksById = new Dictionary<int, Track>();
        foreach (var sourceTrack in source.Tracks)
        {
            sourceTracksById[sourceTrack.Id] = sourceTrack;
        }

        foreach (var targetTrack in target.Tracks)
        {
            if (!sourceTracksById.TryGetValue(targetTrack.Id, out var sourceTrack))
            {
                result.TracksUnmatched++;
                CountUnmatchedTrackSubtree(targetTrack, result);
                continue;
            }

            var trackExtension = sourceTrack.GetGuitarPro();
            if (trackExtension is null)
            {
                result.TracksUnmatched++;
            }
            else
            {
                targetTrack.SetExtension(new GpTrackExtension
                {
                    Metadata = trackExtension.Metadata
                });

                result.TracksAttached++;
            }

            ReattachTrackStaffHierarchyExtensions(targetTrack, sourceTrack, result);
        }

        var fidelityState = target.GetExtension<GpFidelityStateExtension>() ?? new GpFidelityStateExtension();
        fidelityState.HasSourceContext = true;
        fidelityState.FidelityInvalidated = false;
        fidelityState.LastReattachment = result;
        target.SetExtension(fidelityState);

        return result;
    }

    internal static GpFidelityStateExtension? GetGuitarProFidelityState(this Score score)
    {
        ArgumentNullException.ThrowIfNull(score);

        return score.GetExtension<GpFidelityStateExtension>();
    }

    internal static string FormatPartialReattachmentMessage(GpExtensionReattachmentResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return $"Source GP fidelity only partially reattached before write. "
               + $"Unmatched targets: score={(result.ScoreUnmatched ? 1 : 0)}, "
               + $"timelineBars={result.TimelineBarsUnmatched}, "
               + $"tracks={result.TracksUnmatched}, "
               + $"staffs={result.StaffsUnmatched}, voices={result.VoicesUnmatched}, "
               + $"beats={result.BeatsUnmatched}, notes={result.NotesUnmatched}.";
    }

    private static void ReattachTimelineBarExtensions(Score target, Score source, GpExtensionReattachmentResult result)
    {
        var sourceTimelineBarsByIndex = source.TimelineBars
            .GroupBy(timelineBar => timelineBar.Index)
            .ToDictionary(group => group.Key, group => group.First());

        foreach (var targetTimelineBar in target.TimelineBars)
        {
            if (!sourceTimelineBarsByIndex.TryGetValue(targetTimelineBar.Index, out var sourceTimelineBar))
            {
                result.TimelineBarsUnmatched++;
                continue;
            }

            var timelineBarExtension = sourceTimelineBar.GetGuitarPro();
            if (timelineBarExtension is null)
            {
                result.TimelineBarsUnmatched++;
                continue;
            }

            targetTimelineBar.SetExtension(new GpTimelineBarExtension
            {
                Metadata = timelineBarExtension.Metadata
            });

            result.TimelineBarsAttached++;
        }
    }

    private static void ReattachBeatExtensions(
        IReadOnlyList<Beat> targetBeats,
        IReadOnlyList<Beat> sourceBeats,
        GpExtensionReattachmentResult result)
    {
        var sourceBeatsById = BuildItemsByIdQueue(sourceBeats, static beat => beat.Id);
        foreach (var targetBeat in targetBeats)
        {
            if (!TryDequeueMatchingItem(sourceBeatsById, targetBeat.Id, out var sourceBeat))
            {
                CountUnmatchedBeatSubtree(targetBeat, result);
                continue;
            }

            var beatExtension = sourceBeat.GetGuitarPro();
            if (beatExtension is not null)
            {
                targetBeat.SetExtension(new GpBeatExtension
                {
                    Metadata = beatExtension.Metadata
                });

                result.BeatsAttached++;
            }
            else
            {
                result.BeatsUnmatched++;
            }

            var sourceNotesById = BuildItemsByIdQueue(sourceBeat.Notes, static note => note.Id);
            foreach (var targetNote in targetBeat.Notes)
            {
                if (!TryDequeueMatchingItem(sourceNotesById, targetNote.Id, out var sourceNote))
                {
                    result.NotesUnmatched++;
                    continue;
                }

                var noteExtension = sourceNote.GetGuitarPro();
                if (noteExtension is null)
                {
                    result.NotesUnmatched++;
                    continue;
                }

                targetNote.SetExtension(new GpNoteExtension
                {
                    Metadata = noteExtension.Metadata
                });

                result.NotesAttached++;
            }
        }
    }

    private static bool InvalidateTimelineBarExtensions(Score score)
    {
        var removedAny = false;

        foreach (var timelineBar in score.TimelineBars)
        {
            removedAny |= timelineBar.RemoveExtension<GpTimelineBarExtension>();
        }

        return removedAny;
    }

    private static bool InvalidateTrackStaffExtensions(Track track)
    {
        var removedAny = false;

        foreach (var staff in track.Staves)
        {
            removedAny |= staff.RemoveExtension<GpStaffExtension>();

            foreach (var staffMeasure in staff.Measures)
            {
                removedAny |= staffMeasure.RemoveExtension<GpMeasureStaffExtension>();

                foreach (var voice in staffMeasure.Voices)
                {
                    removedAny |= voice.RemoveExtension<GpVoiceExtension>();
                    removedAny |= InvalidateBeatExtensions(voice.Beats);
                }

                removedAny |= InvalidateBeatExtensions(staffMeasure.Beats);
            }
        }

        return removedAny;
    }

    private static bool InvalidateBeatExtensions(IReadOnlyList<Beat> beats)
    {
        var removedAny = false;

        foreach (var beat in beats)
        {
            removedAny |= beat.RemoveExtension<GpBeatExtension>();

            foreach (var note in beat.Notes)
            {
                removedAny |= note.RemoveExtension<GpNoteExtension>();
            }
        }

        return removedAny;
    }

    private static void CountUnmatchedTrackSubtree(Track track, GpExtensionReattachmentResult result)
    {
        foreach (var staff in track.Staves)
        {
            foreach (var staffMeasure in staff.Measures)
            {
                CountUnmatchedStaffMeasureSubtree(staffMeasure, result);
            }
        }
    }

    private static void CountUnmatchedStaffMeasureSubtree(StaffMeasure staffMeasure, GpExtensionReattachmentResult result)
    {
        result.StaffsUnmatched++;

        foreach (var targetVoice in staffMeasure.Voices)
        {
            CountUnmatchedVoiceSubtree(targetVoice, result);
        }

        CountUnmatchedBeatList(staffMeasure.Beats, result);
    }

    private static void CountUnmatchedVoiceSubtree(Voice voice, GpExtensionReattachmentResult result)
    {
        result.VoicesUnmatched++;
        CountUnmatchedBeatList(voice.Beats, result);
    }

    private static void CountUnmatchedBeatSubtree(Beat beat, GpExtensionReattachmentResult result)
    {
        result.BeatsUnmatched++;
        result.NotesUnmatched += beat.Notes.Count;
    }

    private static void CountUnmatchedBeatList(IReadOnlyList<Beat> beats, GpExtensionReattachmentResult result)
    {
        foreach (var beat in beats)
        {
            CountUnmatchedBeatSubtree(beat, result);
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

    private static void ReattachTrackStaffHierarchyExtensions(Track targetTrack, Track sourceTrack, GpExtensionReattachmentResult result)
    {
        if (targetTrack.Staves.Count == 0)
        {
            return;
        }

        var sourceStaffsByIndex = sourceTrack.Staves
            .ToDictionary(staff => staff.StaffIndex);

        foreach (var targetStaff in targetTrack.Staves)
        {
            if (!sourceStaffsByIndex.TryGetValue(targetStaff.StaffIndex, out var sourceStaff))
            {
                foreach (var targetStaffMeasure in targetStaff.Measures)
                {
                    CountUnmatchedStaffMeasureSubtree(targetStaffMeasure, result);
                }

                continue;
            }

            var staffExtension = sourceStaff.GetGuitarPro();
            if (staffExtension is not null)
            {
                targetStaff.SetExtension(new GpStaffExtension
                {
                    Metadata = staffExtension.Metadata
                });
            }

            var sourceMeasuresByIndex = sourceStaff.Measures
                .GroupBy(measure => measure.Index)
                .ToDictionary(group => group.Key, group => group.First());

            foreach (var targetStaffMeasure in targetStaff.Measures)
            {
                if (!sourceMeasuresByIndex.TryGetValue(targetStaffMeasure.Index, out var sourceStaffMeasure))
                {
                    CountUnmatchedStaffMeasureSubtree(targetStaffMeasure, result);

                    continue;
                }

                var staffMeasureExtension = sourceStaffMeasure.GetGuitarPro();
                if (staffMeasureExtension is not null)
                {
                    targetStaffMeasure.SetExtension(new GpMeasureStaffExtension
                    {
                        Metadata = staffMeasureExtension.Metadata
                    });

                    result.StaffsAttached++;
                }
                else
                {
                    result.StaffsUnmatched++;
                }

                ReattachBeatExtensions(targetStaffMeasure.Beats, sourceStaffMeasure.Beats, result);

                var sourceVoicesByIndex = sourceStaffMeasure.Voices
                    .ToDictionary(voice => voice.VoiceIndex);
                foreach (var targetVoice in targetStaffMeasure.Voices)
                {
                    if (!sourceVoicesByIndex.TryGetValue(targetVoice.VoiceIndex, out var sourceVoice))
                    {
                        CountUnmatchedVoiceSubtree(targetVoice, result);
                        continue;
                    }

                    var voiceExtension = sourceVoice.GetGuitarPro();
                    if (voiceExtension is null)
                    {
                        result.VoicesUnmatched++;
                    }
                    else
                    {
                        targetVoice.SetExtension(new GpVoiceExtension
                        {
                            Metadata = voiceExtension.Metadata
                        });

                        result.VoicesAttached++;
                    }

                    ReattachBeatExtensions(targetVoice.Beats, sourceVoice.Beats, result);
                }
            }
        }
    }
}
