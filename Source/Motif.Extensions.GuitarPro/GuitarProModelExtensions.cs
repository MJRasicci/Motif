namespace Motif.Extensions.GuitarPro;

using Motif.Extensions.GuitarPro.Models;
using Motif.Models;

public static class GuitarProModelExtensions
{
    public static GpScoreExtension? GetGuitarPro(this GuitarProScore score)
    {
        ArgumentNullException.ThrowIfNull(score);

        return score.GetExtension<GpScoreExtension>();
    }

    public static GpScoreExtension GetRequiredGuitarPro(this GuitarProScore score)
    {
        ArgumentNullException.ThrowIfNull(score);

        return score.GetRequiredExtension<GpScoreExtension>();
    }

    public static GpScoreExtension GetOrCreateGuitarPro(this GuitarProScore score)
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

    public static GpTrackExtension? GetGuitarPro(this TrackModel track)
    {
        ArgumentNullException.ThrowIfNull(track);

        return track.GetExtension<GpTrackExtension>();
    }

    public static GpTrackExtension GetRequiredGuitarPro(this TrackModel track)
    {
        ArgumentNullException.ThrowIfNull(track);

        return track.GetRequiredExtension<GpTrackExtension>();
    }

    public static GpTrackExtension GetOrCreateGuitarPro(this TrackModel track)
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

    public static GpMeasureExtension? GetGuitarPro(this MeasureModel measure)
    {
        ArgumentNullException.ThrowIfNull(measure);

        return measure.GetExtension<GpMeasureExtension>();
    }

    public static GpMeasureExtension GetRequiredGuitarPro(this MeasureModel measure)
    {
        ArgumentNullException.ThrowIfNull(measure);

        return measure.GetRequiredExtension<GpMeasureExtension>();
    }

    public static GpMeasureExtension GetOrCreateGuitarPro(this MeasureModel measure)
    {
        ArgumentNullException.ThrowIfNull(measure);

        var extension = measure.GetGuitarPro();
        if (extension is not null)
        {
            return extension;
        }

        extension = new GpMeasureExtension
        {
            Metadata = new GpMeasureMetadata()
        };
        measure.SetExtension(extension);
        return extension;
    }

    public static GpMeasureStaffExtension? GetGuitarPro(this MeasureStaffModel staff)
    {
        ArgumentNullException.ThrowIfNull(staff);

        return staff.GetExtension<GpMeasureStaffExtension>();
    }

    public static GpMeasureStaffExtension GetRequiredGuitarPro(this MeasureStaffModel staff)
    {
        ArgumentNullException.ThrowIfNull(staff);

        return staff.GetRequiredExtension<GpMeasureStaffExtension>();
    }

    public static GpMeasureStaffExtension GetOrCreateGuitarPro(this MeasureStaffModel staff)
    {
        ArgumentNullException.ThrowIfNull(staff);

        var extension = staff.GetGuitarPro();
        if (extension is not null)
        {
            return extension;
        }

        extension = new GpMeasureStaffExtension
        {
            Metadata = new GpMeasureStaffMetadata()
        };
        staff.SetExtension(extension);
        return extension;
    }

    public static GpVoiceExtension? GetGuitarPro(this MeasureVoiceModel voice)
    {
        ArgumentNullException.ThrowIfNull(voice);

        return voice.GetExtension<GpVoiceExtension>();
    }

    public static GpVoiceExtension GetRequiredGuitarPro(this MeasureVoiceModel voice)
    {
        ArgumentNullException.ThrowIfNull(voice);

        return voice.GetRequiredExtension<GpVoiceExtension>();
    }

    public static GpVoiceExtension GetOrCreateGuitarPro(this MeasureVoiceModel voice)
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

    public static void ReattachGuitarProExtensionsFrom(this GuitarProScore target, GuitarProScore source)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(source);

        var scoreExtension = source.GetGuitarPro();
        if (scoreExtension is not null)
        {
            target.SetExtension(new GpScoreExtension
            {
                Metadata = scoreExtension.Metadata,
                MasterTrack = scoreExtension.MasterTrack
            });
        }

        var sourceTracksById = new Dictionary<int, TrackModel>();
        foreach (var sourceTrack in source.Tracks)
        {
            sourceTracksById[sourceTrack.Id] = sourceTrack;
        }

        foreach (var targetTrack in target.Tracks)
        {
            if (!sourceTracksById.TryGetValue(targetTrack.Id, out var sourceTrack))
            {
                continue;
            }

            var trackExtension = sourceTrack.GetGuitarPro();
            if (trackExtension is null)
            {
                continue;
            }

            targetTrack.SetExtension(new GpTrackExtension
            {
                Metadata = trackExtension.Metadata
            });

            ReattachMeasureHierarchyExtensions(targetTrack, sourceTrack);
        }
    }

    private static void ReattachMeasureHierarchyExtensions(TrackModel targetTrack, TrackModel sourceTrack)
    {
        for (var measureIndex = 0; measureIndex < targetTrack.Measures.Count && measureIndex < sourceTrack.Measures.Count; measureIndex++)
        {
            var targetMeasure = targetTrack.Measures[measureIndex];
            var sourceMeasure = sourceTrack.Measures[measureIndex];
            var measureExtension = sourceMeasure.GetGuitarPro();
            if (measureExtension is not null)
            {
                targetMeasure.SetExtension(new GpMeasureExtension
                {
                    Metadata = measureExtension.Metadata
                });
            }

            var sourceStaffByIndex = sourceMeasure.AdditionalStaffBars
                .ToDictionary(staff => staff.StaffIndex);
            foreach (var targetStaff in targetMeasure.AdditionalStaffBars)
            {
                if (!sourceStaffByIndex.TryGetValue(targetStaff.StaffIndex, out var sourceStaff))
                {
                    continue;
                }

                var staffExtension = sourceStaff.GetGuitarPro();
                if (staffExtension is null)
                {
                    continue;
                }

                targetStaff.SetExtension(new GpMeasureStaffExtension
                {
                    Metadata = staffExtension.Metadata
                });
            }

            var sourceVoiceByIndex = sourceMeasure.Voices
                .ToDictionary(voice => voice.VoiceIndex);
            foreach (var targetVoice in targetMeasure.Voices)
            {
                if (!sourceVoiceByIndex.TryGetValue(targetVoice.VoiceIndex, out var sourceVoice))
                {
                    continue;
                }

                var voiceExtension = sourceVoice.GetGuitarPro();
                if (voiceExtension is null)
                {
                    continue;
                }

                targetVoice.SetExtension(new GpVoiceExtension
                {
                    Metadata = voiceExtension.Metadata
                });
            }
        }
    }
}
