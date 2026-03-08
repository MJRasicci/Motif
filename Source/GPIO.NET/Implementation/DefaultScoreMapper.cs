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

        var orderedTracks = source.Tracks
            .OrderBy(t => t.Id)
            .ToArray();
        var barSlotStartByTrackId = BuildBarSlotStartByTrackId(orderedTracks);

        var tracks = orderedTracks
            .Select(track =>
            {
                var isStringedTrack = IsStringedTrack(track);
                var measures = MapMeasures(
                    source,
                    track,
                    barSlotStartByTrackId[track.Id],
                    GetTrackStaffCount(track),
                    isStringedTrack);
                ApplyTieDurationStitching(measures);

                return new TrackModel
                {
                    Id = track.Id,
                    Name = track.Name,
                    Metadata = new TrackMetadata
                    {
                        Xml = track.Xml,
                        ShortName = track.ShortName,
                        HasExplicitEmptyShortName = track.HasExplicitEmptyShortName,
                        Color = track.Color,
                        SystemsDefaultLayout = track.SystemsDefaultLayout,
                        SystemsLayout = track.SystemsLayout,
                        HasExplicitEmptySystemsLayout = track.HasExplicitEmptySystemsLayout,
                        PalmMute = track.PalmMute,
                        AutoAccentuation = track.AutoAccentuation,
                        AutoBrush = track.AutoBrush,
                        LetRingThroughout = track.LetRingThroughout,
                        PlayingStyle = track.PlayingStyle,
                        UseOneChannelPerString = track.UseOneChannelPerString,
                        IconId = track.IconId,
                        ForcedSound = track.ForcedSound,
                        TuningPitches = track.TuningPitches,
                        TuningInstrument = track.TuningInstrument,
                        TuningLabel = track.TuningLabel,
                        TuningLabelVisible = track.TuningLabelVisible,
                        HasTrackTuningProperty = track.HasTrackTuningProperty,
                        Properties = track.Properties,
                        InstrumentSetXml = track.InstrumentSetXml,
                        StavesXml = track.StavesXml,
                        SoundsXml = track.SoundsXml,
                        RseXml = track.RseXml,
                        NotationPatchXml = track.NotationPatchXml,
                        InstrumentSet = new InstrumentSetMetadata
                        {
                            Name = track.InstrumentSet.Name,
                            Type = track.InstrumentSet.Type,
                            LineCount = track.InstrumentSet.LineCount,
                            Elements = track.InstrumentSet.Elements.Select(element => new InstrumentElementMetadata
                            {
                                Name = element.Name,
                                Type = element.Type,
                                SoundbankName = element.SoundbankName,
                                Articulations = element.Articulations.Select(articulation => new InstrumentArticulationMetadata
                                {
                                    Name = articulation.Name,
                                    StaffLine = articulation.StaffLine,
                                    Noteheads = articulation.Noteheads,
                                    TechniquePlacement = articulation.TechniquePlacement,
                                    TechniqueSymbol = articulation.TechniqueSymbol,
                                    InputMidiNumbers = articulation.InputMidiNumbers,
                                    OutputRseSound = articulation.OutputRseSound,
                                    OutputMidiNumber = articulation.OutputMidiNumber
                                }).ToArray()
                            }).ToArray()
                        },
                        Sounds = track.Sounds.Select(s => new SoundMetadata
                        {
                            Name = s.Name,
                            Label = s.Label,
                            Path = s.Path,
                            Role = s.Role,
                            MidiLsb = s.MidiLsb,
                            MidiMsb = s.MidiMsb,
                            MidiProgram = s.MidiProgram,
                            Rse = new SoundRseMetadata
                            {
                                SoundbankPatch = s.Rse.SoundbankPatch,
                                SoundbankSet = s.Rse.SoundbankSet,
                                ElementsSettingsXml = s.Rse.ElementsSettingsXml,
                                Pickups = new SoundRsePickupsMetadata
                                {
                                    OverloudPosition = s.Rse.Pickups.OverloudPosition,
                                    Volumes = s.Rse.Pickups.Volumes,
                                    Tones = s.Rse.Pickups.Tones
                                },
                                EffectChain = s.Rse.EffectChain.Select(effect => new RseEffectMetadata
                                {
                                    Id = effect.Id,
                                    Bypass = effect.Bypass,
                                    Parameters = effect.Parameters
                                }).ToArray()
                            }
                        }).ToArray(),
                        Rse = new RseMetadata
                        {
                            Bank = track.ChannelRse.Bank,
                            ChannelStripVersion = track.ChannelRse.ChannelStripVersion,
                            ChannelStripParameters = track.ChannelRse.ChannelStripParameters,
                            Automations = track.ChannelRse.Automations.Select(a => new AutomationMetadata
                            {
                                Type = a.Type,
                                Linear = a.Linear,
                                Bar = a.Bar,
                                Position = a.Position,
                                Visible = a.Visible,
                                Value = a.Value
                            }).ToArray()
                        },
                        PlaybackStateXml = track.PlaybackStateXml,
                        AudioEngineStateXml = track.AudioEngineStateXml,
                        PlaybackState = new PlaybackStateMetadata { Value = track.PlaybackState.Value },
                        AudioEngineState = new AudioEngineStateMetadata { Value = track.AudioEngineState.Value },
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
                        MidiConnection = new MidiConnectionMetadata
                        {
                            Port = track.MidiConnection.Port,
                            PrimaryChannel = track.MidiConnection.PrimaryChannel,
                            SecondaryChannel = track.MidiConnection.SecondaryChannel,
                            ForceOneChannelPerString = track.MidiConnection.ForceOneChannelPerString
                        },
                        Lyrics = new LyricsMetadata
                        {
                            Dispatched = track.Lyrics.Dispatched,
                            Lines = track.Lyrics.Lines.Select(line => new LyricsLineMetadata
                            {
                                Text = line.Text,
                                Offset = line.Offset
                            }).ToArray()
                        },
                        Transpose = new TransposeMetadata
                        {
                            Chromatic = track.Transpose.Chromatic,
                            Octave = track.Transpose.Octave
                        },
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

        var masterAutomations = source.MasterTrack.Automations.Select(a => new AutomationMetadata
        {
            Type = a.Type,
            Linear = a.Linear,
            Bar = a.Bar,
            Position = a.Position,
            Visible = a.Visible,
            Value = a.Value
        }).ToArray();

        var tempoMap = source.MasterTrack.Automations
            .Where(a => string.Equals(a.Type, "Tempo", StringComparison.OrdinalIgnoreCase))
            .Select(ParseTempo)
            .ToArray();

        var score = new GuitarProScore
        {
            Title = source.Score.Title,
            Artist = source.Score.Artist,
            Album = source.Score.Album,
            MasterTrack = new MasterTrackMetadata
            {
                Xml = source.MasterTrack.Xml,
                TrackIds = source.MasterTrack.TrackIds,
                AutomationsXml = source.MasterTrack.AutomationsXml,
                Automations = masterAutomations,
                AutomationTimeline = BuildAutomationTimeline(source),
                DynamicMap = BuildDynamicMap(tracks),
                Anacrusis = source.MasterTrack.Anacrusis,
                RseXml = source.MasterTrack.RseXml,
                Rse = new MasterTrackRseMetadata
                {
                    MasterEffects = source.MasterTrack.Rse.MasterEffects.Select(effect => new RseEffectMetadata
                    {
                        Id = effect.Id,
                        Bypass = effect.Bypass,
                        Parameters = effect.Parameters
                    }).ToArray()
                },
                TempoMap = tempoMap
            },
            Metadata = new ScoreMetadata
            {
                ScoreXml = source.Score.Xml,
                ExplicitEmptyOptionalElements = source.Score.ExplicitEmptyOptionalElements,
                GpVersion = source.GpVersion,
                GpRevisionXml = source.GpRevision.Xml,
                GpRevisionRequired = source.GpRevision.Required,
                GpRevisionRecommended = source.GpRevision.Recommended,
                GpRevisionValue = source.GpRevision.Value,
                EncodingDescription = source.EncodingDescription,
                ScoreViewsXml = source.ScoreViewsXml,
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
                PageSetupXml = source.Score.PageSetupXml,
                MultiVoice = source.Score.MultiVoice,
                BackingTrackXml = source.BackingTrackXml,
                AudioTracksXml = source.AudioTracksXml,
                AssetsXml = source.AssetsXml
            },
            Tracks = tracks,
            PlaybackMasterBarSequence = navigationResolver.BuildPlaybackSequence(source.MasterBars, source.MasterTrack.Anacrusis)
        };

        return ValueTask.FromResult(score);
    }

    private static Dictionary<int, int> BuildBarSlotStartByTrackId(IReadOnlyList<GpifTrack> tracks)
    {
        var slotStartByTrackId = new Dictionary<int, int>();
        var nextSlot = 0;

        foreach (var track in tracks)
        {
            slotStartByTrackId[track.Id] = nextSlot;
            nextSlot += GetTrackStaffCount(track);
        }

        return slotStartByTrackId;
    }

    private static int GetTrackStaffCount(GpifTrack track)
        => Math.Max(1, track.Staffs.Count);

    private static List<MeasureModel> MapMeasures(
        GpifDocument source,
        GpifTrack track,
        int trackBarSlotStart,
        int staffCount,
        bool isStringedTrack)
    {
        var measures = new List<MeasureModel>(source.MasterBars.Count);

        foreach (var masterBar in source.MasterBars.OrderBy(m => m.Index))
        {
            var barRefs = ReferenceListParser.SplitRefs(masterBar.BarsReferenceList);
            var primaryStaff = trackBarSlotStart < barRefs.Count
                ? MapStaffBar(source, track, barRefs[trackBarSlotStart], staffIndex: 0, isStringedTrack)
                : null;
            var additionalStaffBars = new List<MeasureStaffModel>(Math.Max(0, staffCount - 1));
            for (var staffIndex = 1; staffIndex < staffCount; staffIndex++)
            {
                var barSlot = trackBarSlotStart + staffIndex;
                if (barSlot >= barRefs.Count)
                {
                    continue;
                }

                var additionalStaff = MapStaffBar(source, track, barRefs[barSlot], staffIndex, isStringedTrack);
                if (additionalStaff is not null)
                {
                    additionalStaffBars.Add(additionalStaff);
                }
            }

            var voices = primaryStaff?.Voices ?? Array.Empty<MeasureVoiceModel>();
            var beats = primaryStaff?.Beats ?? Array.Empty<BeatModel>();

            measures.Add(new MeasureModel
            {
                MasterBarXml = masterBar.Xml,
                BarXml = primaryStaff?.BarXml ?? string.Empty,
                Index = masterBar.Index,
                TimeSignature = masterBar.Time,
                DoubleBar = masterBar.DoubleBar,
                FreeTime = masterBar.FreeTime,
                TripletFeel = masterBar.TripletFeel,
                SourceBarId = primaryStaff?.SourceBarId ?? -1,
                Clef = primaryStaff?.Clef ?? string.Empty,
                SimileMark = primaryStaff?.SimileMark ?? string.Empty,
                RepeatStart = masterBar.RepeatStart,
                RepeatStartAttributePresent = masterBar.RepeatStartAttributePresent,
                RepeatEnd = masterBar.RepeatEnd,
                RepeatEndAttributePresent = masterBar.RepeatEndAttributePresent,
                RepeatCount = masterBar.RepeatCount,
                RepeatCountAttributePresent = masterBar.RepeatCountAttributePresent,
                AlternateEndings = masterBar.AlternateEndings,
                SectionLetter = masterBar.SectionLetter,
                SectionText = masterBar.SectionText,
                HasExplicitEmptySection = masterBar.HasExplicitEmptySection,
                Jump = masterBar.Jump,
                Target = masterBar.Target,
                DirectionProperties = masterBar.DirectionProperties,
                DirectionsXml = masterBar.DirectionsXml,
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
                MasterBarXPropertiesXml = masterBar.XPropertiesXml,
                BarProperties = primaryStaff?.BarProperties ?? new Dictionary<string, string>(),
                BarXProperties = primaryStaff?.BarXProperties ?? new Dictionary<string, int>(),
                BarXPropertiesXml = primaryStaff?.BarXPropertiesXml ?? string.Empty,
                AdditionalStaffBars = additionalStaffBars,
                Voices = voices,
                Beats = beats
            });
        }

        return measures;
    }

    private static MeasureStaffModel? MapStaffBar(
        GpifDocument source,
        GpifTrack track,
        int barId,
        int staffIndex,
        bool isStringedTrack)
    {
        if (!source.BarsById.TryGetValue(barId, out var bar))
        {
            return null;
        }

        var voices = new List<MeasureVoiceModel>();
        var voiceRefs = ReferenceListParser.SplitRefsPreservePlaceholders(bar.VoicesReferenceList);

        for (var voiceIndex = 0; voiceIndex < voiceRefs.Count; voiceIndex++)
        {
            if (!source.VoicesById.TryGetValue(voiceRefs[voiceIndex], out var voice))
            {
                continue;
            }

            var mappedBeats = MapVoiceBeats(source, track, voice, isStringedTrack);
            voices.Add(new MeasureVoiceModel
            {
                Xml = voice.Xml,
                VoiceIndex = voiceIndex,
                SourceVoiceId = voice.Id,
                Properties = voice.Properties.ToDictionary(kv => kv.Key, kv => kv.Value),
                DirectionTags = voice.DirectionTags.ToArray(),
                Beats = mappedBeats
            });
        }

        var beats = voices.FirstOrDefault(v => v.VoiceIndex == 0)?.Beats
            ?? voices.FirstOrDefault()?.Beats
            ?? Array.Empty<BeatModel>();

        return new MeasureStaffModel
        {
            BarXml = bar.Xml,
            StaffIndex = staffIndex,
            SourceBarId = bar.Id,
            Clef = bar.Clef,
            SimileMark = bar.SimileMark,
            BarProperties = bar.Properties.ToDictionary(kv => kv.Key, kv => kv.Value),
            BarXProperties = bar.XProperties.ToDictionary(kv => kv.Key, kv => kv.Value),
            BarXPropertiesXml = bar.XPropertiesXml,
            Voices = voices,
            Beats = beats
        };
    }

    private static IReadOnlyList<BeatModel> MapVoiceBeats(GpifDocument source, GpifTrack track, GpifVoice voice, bool isStringedTrack)
    {
        var beatRefs = ReferenceListParser.SplitRefs(voice.BeatsReferenceList);
        var voiceProps = voice.Properties.ToDictionary(kv => kv.Key, kv => kv.Value);
        var voiceDirTags = voice.DirectionTags.ToArray();
        var beats = new List<BeatModel>(beatRefs.Count);

        decimal offset = 0;
        for (var beatIndex = 0; beatIndex < beatRefs.Count; beatIndex++)
        {
            var beatId = beatRefs[beatIndex];
            if (!source.BeatsById.TryGetValue(beatId, out var beat))
            {
                continue;
            }

            var duration = ResolveDuration(source, beat.RhythmRef);
            var previousBeat = beatIndex > 0 && source.BeatsById.TryGetValue(beatRefs[beatIndex - 1], out var prev)
                ? prev
                : null;
            var nextBeat = beatIndex + 1 < beatRefs.Count && source.BeatsById.TryGetValue(beatRefs[beatIndex + 1], out var next)
                ? next
                : null;

            var notes = ReferenceListParser.SplitRefs(beat.NotesReferenceList)
                .Where(source.NotesById.ContainsKey)
                .Select(noteRef => source.NotesById[noteRef])
                .Select(n =>
                {
                    var stringNumber = GetStringNumber(n);
                    var hopoOriginNoteId = n.Articulation.HopoDestination
                        ? ResolveHopoCounterpartNoteId(source, previousBeat, stringNumber, isStringedTrack, expectOrigin: true)
                        : null;
                    var hopoDestinationNoteId = n.Articulation.HopoOrigin
                        ? ResolveHopoCounterpartNoteId(source, nextBeat, stringNumber, isStringedTrack, expectOrigin: false)
                        : null;

                    return new NoteModel
                    {
                        Xml = n.Xml,
                        Id = n.Id,
                        Velocity = n.Velocity,
                        MidiPitch = n.MidiPitch,
                        SourceMidiPitch = n.MidiPitch,
                        SourceTransposedMidiPitch = ResolveSourceTransposedMidiPitch(n.MidiPitch, track.Transpose),
                        ConcertPitch = MapPitchValue(n.ConcertPitch),
                        TransposedPitch = MapPitchValue(n.TransposedPitch),
                        SourceFret = n.SourceFret,
                        SourceStringNumber = n.SourceStringNumber,
                        ShowStringNumber = n.ShowStringNumber,
                        StringNumber = stringNumber,
                        XProperties = n.XProperties.ToDictionary(kv => kv.Key, kv => kv.Value),
                        XPropertiesXml = n.XPropertiesXml,
                        Duration = duration,
                        Articulation = new NoteArticulationModel
                        {
                            LeftFingering = n.Articulation.LeftFingering,
                            RightFingering = n.Articulation.RightFingering,
                            Ornament = n.Articulation.Ornament,
                            LetRing = n.Articulation.LetRing,
                            Vibrato = n.Articulation.Vibrato,
                            TieOrigin = n.Articulation.TieOrigin,
                            TieDestination = n.Articulation.TieDestination,
                            Trill = n.Articulation.Trill,
                            TrillSpeed = ArticulationDecoders.DecodeTrillSpeed(n.XProperties),
                            Accent = n.Articulation.Accent,
                            AntiAccent = n.Articulation.AntiAccent,
                            AntiAccentValue = n.Articulation.AntiAccentValue,
                            InstrumentArticulation = n.Articulation.InstrumentArticulation,
                            PalmMuted = n.Articulation.PalmMuted,
                            Muted = n.Articulation.Muted,
                            Tapped = n.Articulation.Tapped,
                            LeftHandTapped = n.Articulation.LeftHandTapped,
                            HopoOrigin = n.Articulation.HopoOrigin,
                            HopoDestination = n.Articulation.HopoDestination,
                            HopoType = InferHopoType(source, n, hopoOriginNoteId, hopoDestinationNoteId),
                            HopoOriginNoteId = hopoOriginNoteId,
                            HopoDestinationNoteId = hopoDestinationNoteId,
                            SlideFlags = n.Articulation.SlideFlags,
                            Slides = ArticulationDecoders.DecodeSlides(n.Articulation.SlideFlags),
                            Bend = ArticulationDecoders.DecodeBend(n.Articulation, n.Articulation.TieDestination),
                            Harmonic = ArticulationDecoders.DecodeHarmonic(n.Articulation)
                        }
                    };
                })
                .ToArray();

            var midi = notes
                .Where(n => n.MidiPitch.HasValue)
                .Select(n => n.MidiPitch!.Value)
                .ToArray();

            beats.Add(new BeatModel
            {
                Xml = beat.Xml,
                Id = beat.Id,
                SourceRhythmId = beat.RhythmRef,
                SourceRhythm = MapRhythmShape(source, beat.RhythmRef),
                GraceType = beat.GraceType,
                Dynamic = beat.Dynamic,
                TransposedPitchStemOrientation = beat.TransposedPitchStemOrientation,
                UserTransposedPitchStemOrientation = beat.UserTransposedPitchStemOrientation,
                HasTransposedPitchStemOrientationUserDefinedElement = beat.HasTransposedPitchStemOrientationUserDefinedElement,
                ConcertPitchStemOrientation = beat.ConcertPitchStemOrientation,
                Wah = beat.Wah,
                Golpe = beat.Golpe,
                Fadding = beat.Fadding,
                Slashed = beat.Slashed,
                Hairpin = beat.Hairpin,
                Variation = beat.Variation,
                Ottavia = beat.Ottavia,
                LegatoOrigin = beat.LegatoOrigin,
                LegatoDestination = beat.LegatoDestination,
                LyricsXml = beat.LyricsXml,
                PickStrokeDirection = beat.PickStrokeDirection,
                VibratoWithTremBarStrength = beat.VibratoWithTremBarStrength,
                Slapped = beat.Slapped,
                Popped = beat.Popped,
                PalmMuted = notes.Any(n => n.Articulation.PalmMuted),
                Brush = beat.Brush,
                BrushIsUp = beat.BrushIsUp,
                Arpeggio = beat.Arpeggio,
                BrushDurationTicks = beat.BrushDurationTicks,
                BrushDurationXPropertyId = beat.BrushDurationXPropertyId,
                HasExplicitBrushDurationXProperty = beat.HasExplicitBrushDurationXProperty,
                Rasgueado = beat.Rasgueado,
                RasgueadoPattern = beat.RasgueadoPattern,
                DeadSlapped = beat.DeadSlapped,
                Tremolo = beat.Tremolo,
                TremoloValue = beat.TremoloValue,
                ChordId = beat.ChordId,
                FreeText = beat.FreeText,
                WhammyBar = ArticulationDecoders.DecodeWhammyBar(beat),
                WhammyUsesElement = beat.WhammyUsesElement,
                WhammyExtendUsesElement = beat.WhammyExtendUsesElement,
                Properties = beat.Properties.ToDictionary(kv => kv.Key, kv => kv.Value),
                XProperties = beat.XProperties.ToDictionary(kv => kv.Key, kv => kv.Value),
                XPropertiesXml = beat.XPropertiesXml,
                VoiceProperties = voiceProps,
                VoiceDirectionTags = voiceDirTags,
                Offset = offset,
                Duration = duration,
                Notes = notes,
                MidiPitches = midi
            });

            offset += duration;
        }

        return beats;
    }

    private static RhythmShapeModel? MapRhythmShape(GpifDocument source, int rhythmRef)
    {
        if (!source.RhythmsById.TryGetValue(rhythmRef, out var rhythm))
        {
            return null;
        }

        return new RhythmShapeModel
        {
            Xml = rhythm.Xml,
            NoteValue = rhythm.NoteValue,
            AugmentationDots = rhythm.AugmentationDots,
            AugmentationDotUsesCountAttribute = rhythm.AugmentationDotUsesCountAttribute,
            AugmentationDotCounts = rhythm.AugmentationDotCounts,
            PrimaryTuplet = ToTupletModel(rhythm.PrimaryTuplet),
            SecondaryTuplet = ToTupletModel(rhythm.SecondaryTuplet)
        };
    }

    private static TupletRatioModel? ToTupletModel(TupletRatio? tuplet)
        => tuplet is null
            ? null
            : new TupletRatioModel
            {
                Numerator = tuplet.Numerator,
                Denominator = tuplet.Denominator
            };

    private static bool IsStringedTrack(GpifTrack track)
        => track.TuningPitches.Length > 0 || track.Staffs.Any(s => s.TuningPitches.Length > 0);

    private static int? GetStringNumber(GpifNote note)
    {
        var stringProperty = note.Properties.FirstOrDefault(p => string.Equals(p.Name, "String", StringComparison.OrdinalIgnoreCase));
        if (stringProperty is null)
        {
            return null;
        }

        if (stringProperty.StringNumber.HasValue)
        {
            return stringProperty.StringNumber;
        }

        return stringProperty.Number;
    }

    private static int? ResolveHopoCounterpartNoteId(
        GpifDocument source,
        GpifBeat? adjacentBeat,
        int? stringNumber,
        bool isStringedTrack,
        bool expectOrigin)
    {
        if (adjacentBeat is null)
        {
            return null;
        }

        var adjacentNotes = ReferenceListParser.SplitRefs(adjacentBeat.NotesReferenceList)
            .Where(source.NotesById.ContainsKey)
            .Select(noteRef => source.NotesById[noteRef])
            .ToArray();

        if (adjacentNotes.Length == 0)
        {
            return null;
        }

        if (isStringedTrack && stringNumber.HasValue)
        {
            return adjacentNotes
                .FirstOrDefault(n => GetStringNumber(n) == stringNumber)
                ?.Id;
        }

        return adjacentNotes
            .FirstOrDefault(n => expectOrigin ? n.Articulation.HopoOrigin : n.Articulation.HopoDestination)
            ?.Id;
    }

    private static HopoTypeKind InferHopoType(
        GpifDocument source,
        GpifNote note,
        int? hopoOriginNoteId,
        int? hopoDestinationNoteId)
    {
        if (!note.Articulation.HopoOrigin && !note.Articulation.HopoDestination)
        {
            return HopoTypeKind.None;
        }

        int? originPitch = null;
        int? destinationPitch = null;

        if (note.Articulation.HopoOrigin)
        {
            originPitch = note.MidiPitch;
            destinationPitch = hopoDestinationNoteId.HasValue && source.NotesById.TryGetValue(hopoDestinationNoteId.Value, out var next)
                ? next.MidiPitch
                : null;
        }
        else if (note.Articulation.HopoDestination)
        {
            originPitch = hopoOriginNoteId.HasValue && source.NotesById.TryGetValue(hopoOriginNoteId.Value, out var previous)
                ? previous.MidiPitch
                : null;
            destinationPitch = note.MidiPitch;
        }

        if (!originPitch.HasValue || !destinationPitch.HasValue)
        {
            return HopoTypeKind.Legato;
        }

        if (destinationPitch.Value > originPitch.Value)
        {
            return HopoTypeKind.HammerOn;
        }

        if (destinationPitch.Value < originPitch.Value)
        {
            return HopoTypeKind.PullOff;
        }

        return HopoTypeKind.Legato;
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
        var (bpm, den) = ParseAutomationValueTokens(a.Value);

        return new TempoEventMetadata
        {
            Bar = a.Bar,
            Position = a.Position,
            Bpm = bpm,
            DenominatorHint = den
        };
    }

    private static IReadOnlyList<AutomationTimelineEventMetadata> BuildAutomationTimeline(GpifDocument source)
    {
        var timeline = new List<AutomationTimelineEventMetadata>();

        foreach (var automation in source.MasterTrack.Automations)
        {
            timeline.Add(ToTimelineEvent(automation, AutomationScopeKind.MasterTrack, trackId: null));
        }

        foreach (var track in source.Tracks)
        {
            foreach (var automation in track.Automations)
            {
                timeline.Add(ToTimelineEvent(automation, AutomationScopeKind.Track, track.Id));
            }
        }

        return timeline
            .OrderBy(e => e.Bar ?? int.MaxValue)
            .ThenBy(e => e.Position ?? int.MaxValue)
            .ThenBy(e => e.Scope)
            .ThenBy(e => e.TrackId ?? int.MaxValue)
            .ThenBy(e => e.Type, StringComparer.OrdinalIgnoreCase)
            .ThenBy(e => e.Value, StringComparer.Ordinal)
            .ToArray();
    }

    private static IReadOnlyList<DynamicEventMetadata> BuildDynamicMap(IReadOnlyList<TrackModel> tracks)
    {
        var map = new List<DynamicEventMetadata>();
        var lastDynamicByTrackVoice = new Dictionary<(int TrackId, int VoiceIndex), string>();

        foreach (var track in tracks.OrderBy(t => t.Id))
        {
            foreach (var measure in track.Measures.OrderBy(m => m.Index))
            {
                if (measure.Voices.Count > 0)
                {
                    foreach (var voice in measure.Voices.OrderBy(v => v.VoiceIndex))
                    {
                        AppendDynamicEvents(
                            map,
                            lastDynamicByTrackVoice,
                            track.Id,
                            measure.Index,
                            voice.VoiceIndex,
                            voice.Beats);
                    }
                }
                else
                {
                    AppendDynamicEvents(
                        map,
                        lastDynamicByTrackVoice,
                        track.Id,
                        measure.Index,
                        voiceIndex: 0,
                        measure.Beats);
                }
            }
        }

        return map
            .OrderBy(e => e.TrackId)
            .ThenBy(e => e.MeasureIndex)
            .ThenBy(e => e.VoiceIndex)
            .ThenBy(e => e.BeatOffset)
            .ThenBy(e => e.BeatId)
            .ToArray();
    }

    private static void AppendDynamicEvents(
        List<DynamicEventMetadata> map,
        Dictionary<(int TrackId, int VoiceIndex), string> lastDynamicByTrackVoice,
        int trackId,
        int measureIndex,
        int voiceIndex,
        IReadOnlyList<BeatModel> beats)
    {
        var key = (trackId, voiceIndex);

        foreach (var beat in beats)
        {
            if (string.IsNullOrWhiteSpace(beat.Dynamic))
            {
                continue;
            }

            if (lastDynamicByTrackVoice.TryGetValue(key, out var previousDynamic)
                && string.Equals(previousDynamic, beat.Dynamic, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            lastDynamicByTrackVoice[key] = beat.Dynamic;

            map.Add(new DynamicEventMetadata
            {
                TrackId = trackId,
                MeasureIndex = measureIndex,
                VoiceIndex = voiceIndex,
                BeatId = beat.Id,
                BeatOffset = beat.Offset,
                Dynamic = beat.Dynamic,
                Kind = ParseDynamicKind(beat.Dynamic)
            });
        }
    }

    private static DynamicKind ParseDynamicKind(string dynamic)
        => dynamic.Trim().ToUpperInvariant() switch
        {
            "PPP" => DynamicKind.PPP,
            "PP" => DynamicKind.PP,
            "P" => DynamicKind.P,
            "MP" => DynamicKind.MP,
            "MF" => DynamicKind.MF,
            "F" => DynamicKind.F,
            "FF" => DynamicKind.FF,
            "FFF" => DynamicKind.FFF,
            _ => DynamicKind.Unknown
        };

    private static AutomationTimelineEventMetadata ToTimelineEvent(
        GpifAutomation automation,
        AutomationScopeKind scope,
        int? trackId)
    {
        var (numericValue, referenceHint) = ParseAutomationValueTokens(automation.Value);

        return new AutomationTimelineEventMetadata
        {
            Scope = scope,
            TrackId = trackId,
            Type = automation.Type,
            Linear = automation.Linear,
            Bar = automation.Bar,
            Position = automation.Position,
            Visible = automation.Visible,
            Value = automation.Value,
            NumericValue = numericValue,
            ReferenceHint = referenceHint,
            Tempo = string.Equals(automation.Type, "Tempo", StringComparison.OrdinalIgnoreCase)
                ? ParseTempo(automation)
                : null
        };
    }

    private static (decimal? NumericValue, int? ReferenceHint) ParseAutomationValueTokens(string? rawValue)
    {
        var parts = (rawValue ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        decimal? numericValue = parts.Length > 0 && decimal.TryParse(parts[0], out var numeric)
            ? numeric
            : null;
        int? referenceHint = parts.Length > 1 && int.TryParse(parts[1], out var reference)
            ? reference
            : null;

        return (numericValue, referenceHint);
    }

    private static PitchValueModel? MapPitchValue(GpifPitchValue? pitch)
        => pitch is null
            ? null
            : new PitchValueModel
            {
                Step = pitch.Step,
                Accidental = pitch.Accidental,
                Octave = pitch.Octave
            };

    private static int? ResolveSourceTransposedMidiPitch(int? midiPitch, GpifTranspose transpose)
    {
        if (!midiPitch.HasValue)
        {
            return null;
        }

        var chromatic = transpose.Chromatic ?? 0;
        var octave = transpose.Octave ?? 0;
        return midiPitch.Value - (octave * 12) + chromatic;
    }

    private static void ApplyTieDurationStitching(List<MeasureModel> measures)
    {
        var carryByPitch = new Dictionary<int, NoteModel>();

        foreach (var note in measures
                     .SelectMany(m => m.Voices.Count > 0
                         ? m.Voices.SelectMany(v => v.Beats)
                         : m.Beats)
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
