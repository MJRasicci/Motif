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
                        ForcedSound = track.ForcedSound,
                        TuningPitches = track.TuningPitches,
                        TuningInstrument = track.TuningInstrument,
                        TuningLabel = track.TuningLabel,
                        TuningLabelVisible = track.TuningLabelVisible,
                        Properties = track.Properties,
                        InstrumentSetXml = track.InstrumentSetXml,
                        StavesXml = track.StavesXml,
                        SoundsXml = track.SoundsXml,
                        RseXml = track.RseXml,
                        InstrumentSet = new InstrumentSetMetadata
                        {
                            Name = track.InstrumentSet.Name,
                            Type = track.InstrumentSet.Type,
                            LineCount = track.InstrumentSet.LineCount
                        },
                        Sounds = track.Sounds.Select(s => new SoundMetadata
                        {
                            Name = s.Name,
                            Label = s.Label,
                            Path = s.Path,
                            Role = s.Role,
                            MidiLsb = s.MidiLsb,
                            MidiMsb = s.MidiMsb,
                            MidiProgram = s.MidiProgram
                        }).ToArray(),
                        Rse = new RseMetadata
                        {
                            ChannelStripVersion = track.ChannelRse.ChannelStripVersion,
                            ChannelStripParameters = track.ChannelRse.ChannelStripParameters
                        },
                        PlaybackStateXml = track.PlaybackStateXml,
                        AudioEngineStateXml = track.AudioEngineStateXml,
                        PlaybackState = new PlaybackStateMetadata { Value = track.PlaybackState.Value },
                        MidiConnectionXml = track.MidiConnectionXml,
                        LyricsXml = track.LyricsXml,
                        AutomationsXml = track.AutomationsXml,
                        Automations = track.Automations.Select(a => new AutomationMetadata
                        {
                            Type = a.Type,
                            Linear = a.Linear,
                            Bar = a.Bar,
                            Position = a.Position,
                            Visible = a.Visible,
                            Value = a.Value
                        }).ToArray(),
                        TransposeXml = track.TransposeXml,
                        Staffs = track.Staffs.Select(s => new StaffMetadata
                        {
                            Id = s.Id,
                            Cref = s.Cref,
                            TuningPitches = s.TuningPitches,
                            CapoFret = s.CapoFret,
                            Properties = s.Properties,
                            Xml = s.Xml
                        }).ToArray()
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
            MasterTrack = new MasterTrackMetadata
            {
                TrackIds = source.MasterTrack.TrackIds,
                Automations = source.MasterTrack.Automations.Select(a => new AutomationMetadata
                {
                    Type = a.Type,
                    Linear = a.Linear,
                    Bar = a.Bar,
                    Position = a.Position,
                    Visible = a.Visible,
                    Value = a.Value
                }).ToArray(),
                RseXml = source.MasterTrack.RseXml,
                TempoMap = source.MasterTrack.Automations
                    .Where(a => string.Equals(a.Type, "Tempo", StringComparison.OrdinalIgnoreCase))
                    .Select(a => ParseTempo(a))
                    .ToArray()
            },
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
            var clef = string.Empty;
            var barProperties = new Dictionary<string, string>();

            if (trackOrdinal < barRefs.Count && source.BarsById.TryGetValue(barRefs[trackOrdinal], out var bar))
            {
                sourceBarId = bar.Id;
                clef = bar.Clef;
                barProperties = bar.Properties.ToDictionary(kv => kv.Key, kv => kv.Value);
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
                Clef = clef,
                RepeatStart = masterBar.RepeatStart,
                RepeatEnd = masterBar.RepeatEnd,
                RepeatCount = masterBar.RepeatCount,
                AlternateEndings = masterBar.AlternateEndings,
                SectionLetter = masterBar.SectionLetter,
                SectionText = masterBar.SectionText,
                Jump = masterBar.Jump,
                Target = masterBar.Target,
                KeyAccidentalCount = masterBar.KeyAccidentalCount,
                KeyMode = masterBar.KeyMode,
                KeyTransposeAs = masterBar.KeyTransposeAs,
                Fermatas = masterBar.Fermatas.Select(f => new FermataMetadata
                {
                    Type = f.Type,
                    Offset = f.Offset,
                    Length = f.Length
                }).ToArray(),
                XProperties = masterBar.XProperties,
                BarProperties = barProperties,
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

    private static TempoEventMetadata ParseTempo(GpifAutomation a)
    {
        var parts = (a.Value ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        decimal? bpm = parts.Length > 0 && decimal.TryParse(parts[0], out var b) ? b : null;
        int? den = parts.Length > 1 && int.TryParse(parts[1], out var d) ? d : null;

        return new TempoEventMetadata
        {
            Bar = a.Bar,
            Position = a.Position,
            Bpm = bpm,
            DenominatorHint = den
        };
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
