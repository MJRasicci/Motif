namespace GPIO.NET.Implementation;

using GPIO.NET.Abstractions;
using GPIO.NET.Models;
using GPIO.NET.Models.Raw;
using GPIO.NET.Models.Write;
using GPIO.NET.Utilities;
using System.Globalization;

public sealed class DefaultScoreUnmapper : IScoreUnmapper
{
    public ValueTask<WriteResult> UnmapAsync(GuitarProScore score, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(score);
        cancellationToken.ThrowIfCancellationRequested();

        var diagnostics = new WriteDiagnostics();

        var tracks = score.Tracks
            .OrderBy(t => t.Id)
            .Select(t => new GpifTrack
            {
                Id = t.Id,
                Name = t.Name,
                ShortName = t.Metadata.ShortName,
                Color = t.Metadata.Color,
                SystemsDefaultLayout = t.Metadata.SystemsDefaultLayout,
                SystemsLayout = t.Metadata.SystemsLayout,
                PalmMute = t.Metadata.PalmMute,
                AutoAccentuation = t.Metadata.AutoAccentuation,
                AutoBrush = t.Metadata.AutoBrush,
                PlayingStyle = t.Metadata.PlayingStyle,
                UseOneChannelPerString = t.Metadata.UseOneChannelPerString,
                IconId = t.Metadata.IconId,
                ForcedSound = t.Metadata.ForcedSound
            })
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

                    rhythms[currentRhythmId] = ToRhythm(beat.Duration, currentRhythmId, diagnostics);

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
                                    SlideFlags = note.Articulation.SlideFlags ?? ArticulationDecoders.EncodeSlides(note.Articulation.Slides),
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
                    VoicesReferenceList = currentVoiceId.ToString(CultureInfo.InvariantCulture)
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
            Score = new ScoreInfo
            {
                Title = score.Title,
                SubTitle = score.Metadata.SubTitle,
                Artist = score.Artist,
                Album = score.Album,
                Words = score.Metadata.Words,
                Music = score.Metadata.Music,
                WordsAndMusic = score.Metadata.WordsAndMusic,
                Copyright = score.Metadata.Copyright,
                Tabber = score.Metadata.Tabber,
                Instructions = score.Metadata.Instructions,
                Notices = score.Metadata.Notices,
                FirstPageHeader = score.Metadata.FirstPageHeader,
                FirstPageFooter = score.Metadata.FirstPageFooter,
                PageHeader = score.Metadata.PageHeader,
                PageFooter = score.Metadata.PageFooter,
                ScoreSystemsDefaultLayout = score.Metadata.ScoreSystemsDefaultLayout,
                ScoreSystemsLayout = score.Metadata.ScoreSystemsLayout,
                ScoreZoomPolicy = score.Metadata.ScoreZoomPolicy,
                ScoreZoom = score.Metadata.ScoreZoom,
                MultiVoice = score.Metadata.MultiVoice
            },
            Tracks = tracks,
            MasterBars = masterBars,
            BarsById = bars,
            VoicesById = voices,
            BeatsById = beats,
            NotesById = notes,
            RhythmsById = rhythms
        };

        return ValueTask.FromResult(new WriteResult
        {
            RawDocument = doc,
            Diagnostics = diagnostics
        });
    }

    private static GpifRhythm ToRhythm(decimal duration, int id, WriteDiagnostics diagnostics)
    {
        var candidates = new[]
        {
            new { Name = "Whole", Base = 1m },
            new { Name = "Half", Base = 1m / 2m },
            new { Name = "Quarter", Base = 1m / 4m },
            new { Name = "Eighth", Base = 1m / 8m },
            new { Name = "16th", Base = 1m / 16m },
            new { Name = "32nd", Base = 1m / 32m },
            new { Name = "64th", Base = 1m / 64m }
        };

        foreach (var c in candidates)
        {
            for (var dots = 0; dots <= 2; dots++)
            {
                var dotFactor = 1m;
                var add = 1m;
                for (var i = 0; i < dots; i++)
                {
                    add /= 2m;
                    dotFactor += add;
                }

                var plain = c.Base * dotFactor;
                if (NearlyEqual(plain, duration))
                {
                    return new GpifRhythm { Id = id, NoteValue = c.Name, AugmentationDots = dots };
                }

                // common tuplet ratios
                foreach (var tr in new[] { (3, 2), (5, 4), (6, 4), (7, 4), (9, 8) })
                {
                    var tupled = plain * ((decimal)tr.Item2 / tr.Item1);
                    if (NearlyEqual(tupled, duration))
                    {
                        return new GpifRhythm
                        {
                            Id = id,
                            NoteValue = c.Name,
                            AugmentationDots = dots,
                            PrimaryTuplet = new TupletRatio { Numerator = tr.Item1, Denominator = tr.Item2 }
                        };
                    }
                }
            }
        }

        diagnostics.Warn(
            code: "RHYTHM_APPROXIMATED",
            category: "Rhythm",
            message: $"Duration {duration} was approximated to quarter note in writer.");
        return new GpifRhythm { Id = id, NoteValue = "Quarter" };
    }

    private static bool NearlyEqual(decimal a, decimal b)
        => Math.Abs(a - b) <= 0.00001m;
}
