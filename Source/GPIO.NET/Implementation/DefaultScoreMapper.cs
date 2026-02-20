namespace GPIO.NET.Implementation;

using GPIO.NET.Abstractions;
using GPIO.NET.Models;
using GPIO.NET.Models.Raw;
using GPIO.NET.Utilities;

public sealed class DefaultScoreMapper : IScoreMapper
{
    private readonly INavigationResolver navigationResolver;

    public DefaultScoreMapper()
        : this(new DefaultNavigationResolver())
    {
    }

    public DefaultScoreMapper(INavigationResolver navigationResolver)
    {
        this.navigationResolver = navigationResolver;
    }

    public ValueTask<GuitarProScore> MapAsync(GpifDocument source, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        cancellationToken.ThrowIfCancellationRequested();

        var tracks = source.Tracks
            .OrderBy(t => t.Id)
            .Select((track, trackOrdinal) =>
            {
                var measures = MapMeasures(source, trackOrdinal);
                ApplyTieDurationStitching(measures);

                return new TrackModel
                {
                    Id = track.Id,
                    Name = track.Name,
                    Metadata = new TrackMetadata
                    {
                        ShortName = track.ShortName,
                        Color = track.Color,
                        SystemsDefaultLayout = track.SystemsDefaultLayout,
                        SystemsLayout = track.SystemsLayout,
                        PalmMute = track.PalmMute,
                        AutoAccentuation = track.AutoAccentuation,
                        AutoBrush = track.AutoBrush,
                        PlayingStyle = track.PlayingStyle,
                        UseOneChannelPerString = track.UseOneChannelPerString,
                        IconId = track.IconId,
                        ForcedSound = track.ForcedSound
                    },
                    Measures = measures
                };
            })
            .ToArray();

        var score = new GuitarProScore
        {
            Title = source.Score.Title,
            Artist = source.Score.Artist,
            Album = source.Score.Album,
            Metadata = new ScoreMetadata
            {
                SubTitle = source.Score.SubTitle,
                Words = source.Score.Words,
                Music = source.Score.Music,
                WordsAndMusic = source.Score.WordsAndMusic,
                Copyright = source.Score.Copyright,
                Tabber = source.Score.Tabber,
                Instructions = source.Score.Instructions,
                Notices = source.Score.Notices,
                FirstPageHeader = source.Score.FirstPageHeader,
                FirstPageFooter = source.Score.FirstPageFooter,
                PageHeader = source.Score.PageHeader,
                PageFooter = source.Score.PageFooter,
                ScoreSystemsDefaultLayout = source.Score.ScoreSystemsDefaultLayout,
                ScoreSystemsLayout = source.Score.ScoreSystemsLayout,
                ScoreZoomPolicy = source.Score.ScoreZoomPolicy,
                ScoreZoom = source.Score.ScoreZoom,
                MultiVoice = source.Score.MultiVoice
            },
            Tracks = tracks,
            PlaybackMasterBarSequence = navigationResolver.BuildPlaybackSequence(source.MasterBars)
        };

        return ValueTask.FromResult(score);
    }

    private static List<MeasureModel> MapMeasures(GpifDocument source, int trackOrdinal)
    {
        var measures = new List<MeasureModel>(source.MasterBars.Count);

        foreach (var masterBar in source.MasterBars.OrderBy(m => m.Index))
        {
            var barRefs = ReferenceListParser.SplitRefs(masterBar.BarsReferenceList);
            var beats = new List<BeatModel>();
            var sourceBarId = -1;

            if (trackOrdinal < barRefs.Count && source.BarsById.TryGetValue(barRefs[trackOrdinal], out var bar))
            {
                sourceBarId = bar.Id;
                var voiceRefs = ReferenceListParser.SplitRefs(bar.VoicesReferenceList);
                if (voiceRefs.Count > 0 && source.VoicesById.TryGetValue(voiceRefs[0], out var voice))
                {
                    var beatRefs = ReferenceListParser.SplitRefs(voice.BeatsReferenceList);
                    decimal offset = 0;
                    foreach (var beatId in beatRefs)
                    {
                        if (!source.BeatsById.TryGetValue(beatId, out var beat))
                        {
                            continue;
                        }

                        var duration = ResolveDuration(source, beat.RhythmRef);
                        var notes = ReferenceListParser.SplitRefs(beat.NotesReferenceList)
                            .Where(source.NotesById.ContainsKey)
                            .Select(id => source.NotesById[id])
                            .Select(n => new NoteModel
                            {
                                Id = n.Id,
                                MidiPitch = n.MidiPitch,
                                Duration = duration,
                                Articulation = new NoteArticulationModel
                                {
                                    LetRing = n.Articulation.LetRing,
                                    Vibrato = n.Articulation.Vibrato,
                                    TieOrigin = n.Articulation.TieOrigin,
                                    TieDestination = n.Articulation.TieDestination,
                                    Trill = n.Articulation.Trill,
                                    Accent = n.Articulation.Accent,
                                    AntiAccent = n.Articulation.AntiAccent,
                                    InstrumentArticulation = n.Articulation.InstrumentArticulation,
                                    PalmMuted = n.Articulation.PalmMuted,
                                    Muted = n.Articulation.Muted,
                                    Tapped = n.Articulation.Tapped,
                                    LeftHandTapped = n.Articulation.LeftHandTapped,
                                    HopoOrigin = n.Articulation.HopoOrigin,
                                    HopoDestination = n.Articulation.HopoDestination,
                                    SlideFlags = n.Articulation.SlideFlags,
                                    Slides = ArticulationDecoders.DecodeSlides(n.Articulation.SlideFlags),
                                    Bend = ArticulationDecoders.DecodeBend(n.Articulation),
                                    Harmonic = ArticulationDecoders.DecodeHarmonic(n.Articulation)
                                }
                            })
                            .ToArray();

                        var midi = notes
                            .Where(n => n.MidiPitch.HasValue)
                            .Select(n => n.MidiPitch!.Value)
                            .ToArray();

                        beats.Add(new BeatModel
                        {
                            Id = beat.Id,
                            Offset = offset,
                            Duration = duration,
                            Notes = notes,
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
                SourceBarId = sourceBarId,
                RepeatStart = masterBar.RepeatStart,
                RepeatEnd = masterBar.RepeatEnd,
                RepeatCount = masterBar.RepeatCount,
                AlternateEndings = masterBar.AlternateEndings,
                SectionLetter = masterBar.SectionLetter,
                SectionText = masterBar.SectionText,
                Jump = masterBar.Jump,
                Target = masterBar.Target,
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

        var baseDuration = rhythm.NoteValue switch
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

        if (baseDuration <= 0m)
        {
            return 0m;
        }

        var dotFactor = 1m;
        var add = 1m;
        for (var i = 0; i < rhythm.AugmentationDots; i++)
        {
            add /= 2m;
            dotFactor += add;
        }

        var duration = baseDuration * dotFactor;
        duration *= TupletFactor(rhythm.PrimaryTuplet);
        duration *= TupletFactor(rhythm.SecondaryTuplet);
        return duration;
    }

    private static decimal TupletFactor(TupletRatio? tuplet)
    {
        if (tuplet is null || tuplet.Numerator <= 0 || tuplet.Denominator <= 0)
        {
            return 1m;
        }

        return ((decimal)tuplet.Denominator) / tuplet.Numerator;
    }

    private static void ApplyTieDurationStitching(List<MeasureModel> measures)
    {
        var carryByPitch = new Dictionary<int, NoteModel>();

        foreach (var note in measures
                     .SelectMany(m => m.Beats)
                     .SelectMany(b => b.Notes)
                     .Where(n => n.MidiPitch.HasValue))
        {
            var pitch = note.MidiPitch!.Value;

            if (note.Articulation.TieDestination && carryByPitch.TryGetValue(pitch, out var previous))
            {
                previous.Duration += note.Duration;
                note.TieExtendedFromPrevious = true;
            }

            if (note.Articulation.TieOrigin)
            {
                carryByPitch[pitch] = note;
            }
            else if (!note.Articulation.TieDestination)
            {
                carryByPitch.Remove(pitch);
            }
        }
    }
}
