namespace GPIO.NET.Implementation;

using GPIO.NET.Abstractions;
using GPIO.NET.Models;
using GPIO.NET.Models.Raw;

public sealed class DefaultScoreMapper : IScoreMapper
{
    public ValueTask<GuitarProScore> MapAsync(GpifDocument source, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        cancellationToken.ThrowIfCancellationRequested();

        var tracks = source.Tracks
            .OrderBy(t => t.Id)
            .Select((track, trackOrdinal) => new TrackModel
            {
                Id = track.Id,
                Name = track.Name,
                Measures = MapMeasures(source, trackOrdinal)
            })
            .ToArray();

        var score = new GuitarProScore
        {
            Title = source.Score.Title,
            Artist = source.Score.Artist,
            Album = source.Score.Album,
            Tracks = tracks
        };

        return ValueTask.FromResult(score);
    }

    private static List<MeasureModel> MapMeasures(GpifDocument source, int trackOrdinal)
    {
        var measures = new List<MeasureModel>(source.MasterBars.Count);

        foreach (var masterBar in source.MasterBars.OrderBy(m => m.Index))
        {
            var barRefs = SplitRefs(masterBar.BarsReferenceList);
            var beats = new List<BeatModel>();

            if (trackOrdinal < barRefs.Count && source.BarsById.TryGetValue(barRefs[trackOrdinal], out var bar))
            {
                var voiceRefs = SplitRefs(bar.VoicesReferenceList);
                if (voiceRefs.Count > 0 && source.VoicesById.TryGetValue(voiceRefs[0], out var voice))
                {
                    var beatRefs = SplitRefs(voice.BeatsReferenceList);
                    decimal offset = 0;
                    foreach (var beatId in beatRefs)
                    {
                        if (!source.BeatsById.TryGetValue(beatId, out var beat))
                        {
                            continue;
                        }

                        var duration = ResolveDuration(source, beat.RhythmRef);
                        var midi = SplitRefs(beat.NotesReferenceList)
                            .Select(id => source.NotesById.TryGetValue(id, out var n) ? n.MidiPitch : null)
                            .Where(p => p.HasValue)
                            .Select(p => p!.Value)
                            .ToArray();

                        beats.Add(new BeatModel
                        {
                            Offset = offset,
                            Duration = duration,
                            MidiPitches = midi
                        });

                        offset += duration;
                    }
                }
            }

            measures.Add(new MeasureModel
            {
                Index = masterBar.Index,
                TimeSignature = masterBar.Time,
                Beats = beats
            });
        }

        return measures;
    }

    private static decimal ResolveDuration(GpifDocument source, int rhythmRef)
    {
        if (!source.RhythmsById.TryGetValue(rhythmRef, out var rhythm))
        {
            return 0m;
        }

        return rhythm.NoteValue switch
        {
            "Whole" => 1m,
            "Half" => 1m / 2m,
            "Quarter" => 1m / 4m,
            "Eighth" => 1m / 8m,
            "16th" => 1m / 16m,
            "32nd" => 1m / 32m,
            "64th" => 1m / 64m,
            _ => 0m
        };
    }

    private static List<int> SplitRefs(string refs)
        => refs
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(value => int.TryParse(value, out var parsed) ? parsed : -1)
            .Where(value => value >= 0)
            .ToList();
}
