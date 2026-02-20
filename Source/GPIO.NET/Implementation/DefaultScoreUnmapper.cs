namespace GPIO.NET.Implementation;

using GPIO.NET.Abstractions;
using GPIO.NET.Models;
using GPIO.NET.Models.Raw;
using GPIO.NET.Utilities;

public sealed class DefaultScoreUnmapper : IScoreUnmapper
{
    public ValueTask<GpifDocument> UnmapAsync(GuitarProScore score, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(score);
        cancellationToken.ThrowIfCancellationRequested();

        var tracks = score.Tracks
            .OrderBy(t => t.Id)
            .Select(t => new GpifTrack { Id = t.Id, Name = t.Name })
            .ToArray();

        var barId = 1;
        var voiceId = 1;
        var beatId = 1;
        var noteId = 1;
        var rhythmId = 1;

        var bars = new Dictionary<int, GpifBar>();
        var voices = new Dictionary<int, GpifVoice>();
        var beats = new Dictionary<int, GpifBeat>();
        var notes = new Dictionary<int, GpifNote>();
        var rhythms = new Dictionary<int, GpifRhythm>();
        var masterBars = new List<GpifMasterBar>();

        var maxMeasures = score.Tracks.Select(t => t.Measures.Count).DefaultIfEmpty(0).Max();

        for (var m = 0; m < maxMeasures; m++)
        {
            var measureBarIds = new List<int>();

            foreach (var track in score.Tracks.OrderBy(t => t.Id))
            {
                if (m >= track.Measures.Count)
                {
                    continue;
                }

                var measure = track.Measures[m];
                var currentBarId = barId++;
                var currentVoiceId = voiceId++;

                var beatIds = new List<int>();
                foreach (var beat in measure.Beats)
                {
                    var currentBeatId = beatId++;
                    var currentRhythmId = rhythmId++;
                    var noteRefs = new List<int>();

                    rhythms[currentRhythmId] = new GpifRhythm
                    {
                        Id = currentRhythmId,
                        NoteValue = ToRawNoteValue(beat.Duration)
                    };

                    if (beat.Notes.Count > 0)
                    {
                        foreach (var note in beat.Notes)
                        {
                            var currentNoteId = noteId++;
                            noteRefs.Add(currentNoteId);
                            notes[currentNoteId] = new GpifNote
                            {
                                Id = currentNoteId,
                                MidiPitch = note.MidiPitch,
                                Articulation = new GpifNoteArticulation
                                {
                                    LetRing = note.Articulation.LetRing,
                                    Vibrato = note.Articulation.Vibrato,
                                    TieOrigin = note.Articulation.TieOrigin,
                                    TieDestination = note.Articulation.TieDestination,
                                    Trill = note.Articulation.Trill,
                                    Accent = note.Articulation.Accent,
                                    AntiAccent = note.Articulation.AntiAccent,
                                    InstrumentArticulation = note.Articulation.InstrumentArticulation,
                                    PalmMuted = note.Articulation.PalmMuted,
                                    Muted = note.Articulation.Muted,
                                    Tapped = note.Articulation.Tapped,
                                    LeftHandTapped = note.Articulation.LeftHandTapped,
                                    HopoOrigin = note.Articulation.HopoOrigin,
                                    HopoDestination = note.Articulation.HopoDestination,
                                    SlideFlags = note.Articulation.SlideFlags,
                                    BendEnabled = note.Articulation.Bend?.Enabled ?? false,
                                    BendOriginOffset = note.Articulation.Bend?.OriginOffset,
                                    BendOriginValue = note.Articulation.Bend?.OriginValue,
                                    BendMiddleOffset1 = note.Articulation.Bend?.MiddleOffset1,
                                    BendMiddleOffset2 = note.Articulation.Bend?.MiddleOffset2,
                                    BendMiddleValue = note.Articulation.Bend?.MiddleValue,
                                    BendDestinationOffset = note.Articulation.Bend?.DestinationOffset,
                                    BendDestinationValue = note.Articulation.Bend?.DestinationValue,
                                    HarmonicEnabled = note.Articulation.Harmonic?.Enabled ?? false,
                                    HarmonicType = note.Articulation.Harmonic?.Type,
                                    HarmonicFret = note.Articulation.Harmonic?.Fret
                                }
                            };
                        }
                    }

                    beats[currentBeatId] = new GpifBeat
                    {
                        Id = currentBeatId,
                        RhythmRef = currentRhythmId,
                        NotesReferenceList = ReferenceListFormatter.JoinRefs(noteRefs)
                    };

                    beatIds.Add(currentBeatId);
                }

                voices[currentVoiceId] = new GpifVoice
                {
                    Id = currentVoiceId,
                    BeatsReferenceList = ReferenceListFormatter.JoinRefs(beatIds)
                };

                bars[currentBarId] = new GpifBar
                {
                    Id = currentBarId,
                    VoicesReferenceList = currentVoiceId.ToString()
                };

                measureBarIds.Add(currentBarId);

                if (masterBars.Count <= m)
                {
                    masterBars.Add(new GpifMasterBar
                    {
                        Index = m,
                        Time = measure.TimeSignature,
                        RepeatStart = measure.RepeatStart,
                        RepeatEnd = measure.RepeatEnd,
                        RepeatCount = measure.RepeatCount,
                        AlternateEndings = measure.AlternateEndings,
                        SectionLetter = measure.SectionLetter,
                        SectionText = measure.SectionText,
                        Jump = measure.Jump,
                        Target = measure.Target
                    });
                }
            }

            if (masterBars.Count > m)
            {
                var existing = masterBars[m];
                masterBars[m] = new GpifMasterBar
                {
                    Index = existing.Index,
                    Time = existing.Time,
                    BarsReferenceList = ReferenceListFormatter.JoinRefs(measureBarIds),
                    AlternateEndings = existing.AlternateEndings,
                    RepeatStart = existing.RepeatStart,
                    RepeatEnd = existing.RepeatEnd,
                    RepeatCount = existing.RepeatCount,
                    SectionLetter = existing.SectionLetter,
                    SectionText = existing.SectionText,
                    Jump = existing.Jump,
                    Target = existing.Target
                };
            }
        }

        var doc = new GpifDocument
        {
            Score = new ScoreInfo { Title = score.Title, Artist = score.Artist, Album = score.Album },
            Tracks = tracks,
            MasterBars = masterBars,
            BarsById = bars,
            VoicesById = voices,
            BeatsById = beats,
            NotesById = notes,
            RhythmsById = rhythms
        };

        return ValueTask.FromResult(doc);
    }

    private static string ToRawNoteValue(decimal duration)
    {
        if (duration >= 1m) return "Whole";
        if (duration >= 0.5m) return "Half";
        if (duration >= 0.25m) return "Quarter";
        if (duration >= 0.125m) return "Eighth";
        if (duration >= 0.0625m) return "16th";
        if (duration >= 0.03125m) return "32nd";
        return "64th";
    }
}
