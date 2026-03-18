namespace Motif.Extensions.GuitarPro.Implementation;

using Motif;
using Motif.Extensions.GuitarPro.Abstractions;
using Motif.Extensions.GuitarPro.Models;
using Motif.Extensions.GuitarPro.Models.Raw;
using Motif.Extensions.GuitarPro.Utilities;
using Motif.Models;
using CoreTupletRatio = Motif.Models.TupletRatio;
using RawTupletRatio = Motif.Extensions.GuitarPro.Models.Raw.TupletRatio;

internal sealed class DefaultScoreMapper : IScoreMapper
{
    public ValueTask<Score> MapAsync(GpifDocument source, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        cancellationToken.ThrowIfCancellationRequested();

        var orderedTracks = source.Tracks
            .OrderBy(t => t.Id)
            .ToArray();
        var timelineBars = source.MasterBars
            .OrderBy(masterBar => masterBar.Index)
            .Select(MapTimelineBar)
            .ToArray();
        var barSlotStartByTrackId = BuildBarSlotStartByTrackId(orderedTracks);

        var tracks = orderedTracks
            .Select(track =>
            {
                var isStringedTrack = IsStringedTrack(track);
                var staves = MapTrackStaves(
                    source,
                    track,
                    barSlotStartByTrackId[track.Id],
                    GetTrackStaffCount(track),
                    isStringedTrack);
                var trackMetadata = new TrackMetadata
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
                        TuningInstrument = s.TuningInstrument,
                        TuningLabel = s.TuningLabel,
                        TuningLabelVisible = s.TuningLabelVisible,
                        EmitTuningFlatElement = s.EmitTuningFlatElement,
                        EmitTuningFlatProperty = s.EmitTuningFlatProperty,
                        CapoFret = s.CapoFret,
                        FretCount = s.FretCount,
                        PartialCapoFret = s.PartialCapoFret,
                        PartialCapoStringFlags = s.PartialCapoStringFlags,
                        EmitChordCollection = s.EmitChordCollection,
                        EmitChordWorkingSet = s.EmitChordWorkingSet,
                        EmitDiagramCollection = s.EmitDiagramCollection,
                        EmitDiagramWorkingSet = s.EmitDiagramWorkingSet,
                        Name = s.Name,
                        Properties = s.Properties,
                        Xml = s.Xml
                    }).ToArray()
                };

                var mappedTrack = new Track
                {
                    Id = track.Id,
                    Name = track.Name,
                    Instrument = GpTrackProfileCatalog.InferInstrument(trackMetadata, track.Name),
                    Transposition = new TrackTransposition
                    {
                        Chromatic = track.Transpose.Chromatic ?? 0,
                        Octave = track.Transpose.Octave ?? 0
                    },
                    Staves = AttachTrackStaffExtensions(trackMetadata, staves)
                };

                mappedTrack.SetExtension(new GpTrackExtension
                {
                    Metadata = trackMetadata
                });

                return mappedTrack;
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
        var masterTrackMetadata = new MasterTrackMetadata
        {
            Xml = source.MasterTrack.Xml,
            TrackIds = source.MasterTrack.TrackIds,
            AutomationsXml = source.MasterTrack.AutomationsXml,
            Automations = masterAutomations,
            AutomationTimeline = BuildAutomationTimeline(source),
            DynamicMap = Array.Empty<DynamicEventMetadata>(),
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
        };
        var scoreMetadata = new ScoreMetadata
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
        };

        var score = new Score
        {
            Title = source.Score.Title,
            Artist = source.Score.Artist,
            Album = source.Score.Album,
            Tracks = tracks,
            TimelineBars = timelineBars,
            Anacrusis = source.MasterTrack.Anacrusis
        };

        PopulateTimelineGeometry(score);
        score.PointControls = BuildPointControls(score, tempoMap);
        score.SpanControls = BuildSpanControls(score);
        masterTrackMetadata.DynamicMap = BuildDynamicMap(score);

        ScoreNavigation.RebuildPlaybackSequence(score);

        score.SetExtension(new GpScoreExtension
        {
            Metadata = scoreMetadata,
            MasterTrack = masterTrackMetadata
        });

        return ValueTask.FromResult(score);
    }

    private static IReadOnlyList<Staff> AttachTrackStaffExtensions(TrackMetadata trackMetadata, IReadOnlyList<Staff> staves)
    {
        for (var staffIndex = 0; staffIndex < staves.Count; staffIndex++)
        {
            var staff = staves[staffIndex];

            if (staffIndex < trackMetadata.Staffs.Count)
            {
                var staffMetadata = CloneStaffMetadata(trackMetadata.Staffs[staffIndex]);
                staff.Tuning = new StaffTuning
                {
                    Pitches = staffMetadata.TuningPitches.ToArray(),
                    Label = string.IsNullOrWhiteSpace(staffMetadata.Name)
                        ? trackMetadata.TuningLabel
                        : staffMetadata.Name
                };
                staff.CapoFret = staffMetadata.CapoFret;

                staff.SetExtension(new GpStaffExtension
                {
                    Metadata = staffMetadata
                });
                continue;
            }

            if (trackMetadata.TuningPitches.Length > 0)
            {
                staff.Tuning = new StaffTuning
                {
                    Pitches = trackMetadata.TuningPitches.ToArray(),
                    Label = trackMetadata.TuningLabel
                };
            }
        }

        return staves;
    }

    private static StaffMeasure CreateEmptyStaffMeasure(int index, int staffIndex)
    {
        var staffMeasure = new StaffMeasure
        {
            Index = index,
            StaffIndex = staffIndex
        };
        staffMeasure.SetExtension(new GpMeasureStaffExtension
        {
            Metadata = new GpMeasureStaffMetadata
            {
                SourceBarId = -1
            }
        });

        return staffMeasure;
    }

    private static StaffMetadata CloneStaffMetadata(StaffMetadata source)
        => new()
        {
            Id = source.Id,
            Cref = source.Cref,
            TuningPitches = source.TuningPitches.ToArray(),
            TuningInstrument = source.TuningInstrument,
            TuningLabel = source.TuningLabel,
            TuningLabelVisible = source.TuningLabelVisible,
            EmitTuningFlatElement = source.EmitTuningFlatElement,
            EmitTuningFlatProperty = source.EmitTuningFlatProperty,
            CapoFret = source.CapoFret,
            FretCount = source.FretCount,
            PartialCapoFret = source.PartialCapoFret,
            PartialCapoStringFlags = source.PartialCapoStringFlags,
            EmitChordCollection = source.EmitChordCollection,
            EmitChordWorkingSet = source.EmitChordWorkingSet,
            EmitDiagramCollection = source.EmitDiagramCollection,
            EmitDiagramWorkingSet = source.EmitDiagramWorkingSet,
            Name = source.Name,
            Properties = source.Properties.ToDictionary(kv => kv.Key, kv => kv.Value),
            Xml = source.Xml
        };

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

    private static IReadOnlyList<Staff> MapTrackStaves(
        GpifDocument source,
        GpifTrack track,
        int trackBarSlotStart,
        int staffCount,
        bool isStringedTrack)
    {
        var staffMeasures = Enumerable.Range(0, Math.Max(1, staffCount))
            .Select(staffIndex => new Staff
            {
                StaffIndex = staffIndex,
                Measures = new List<StaffMeasure>(source.MasterBars.Count)
            })
            .ToArray();

        foreach (var masterBar in source.MasterBars.OrderBy(m => m.Index))
        {
            var barRefs = ReferenceListParser.SplitRefs(masterBar.BarsReferenceList);
            for (var staffIndex = 0; staffIndex < staffMeasures.Length; staffIndex++)
            {
                var barSlot = trackBarSlotStart + staffIndex;
                var staffMeasure = barSlot < barRefs.Count
                    ? MapStaffBar(source, track, barRefs[barSlot], masterBar.Index, staffIndex, isStringedTrack)
                    : null;
                ((List<StaffMeasure>)staffMeasures[staffIndex].Measures).Add(
                    staffMeasure ?? CreateEmptyStaffMeasure(masterBar.Index, staffIndex));
            }
        }

        foreach (var staff in staffMeasures)
        {
            ApplyTieDurationStitching(staff.Measures);
        }

        return staffMeasures;
    }

    private static TimelineBar MapTimelineBar(GpifMasterBar masterBar)
    {
        var timelineBar = new TimelineBar
        {
            Index = masterBar.Index,
            TimeSignature = masterBar.Time,
            DoubleBar = masterBar.DoubleBar,
            FreeTime = masterBar.FreeTime,
            TripletFeel = masterBar.TripletFeel,
            RepeatStart = masterBar.RepeatStart,
            RepeatEnd = masterBar.RepeatEnd,
            RepeatCount = masterBar.RepeatCount,
            AlternateEndings = masterBar.AlternateEndings,
            SectionLetter = masterBar.SectionLetter,
            SectionText = masterBar.SectionText,
            Jump = masterBar.Jump,
            Target = masterBar.Target,
            KeyAccidentalCount = masterBar.KeyAccidentalCount,
            KeyMode = masterBar.KeyMode
        };
        timelineBar.SetExtension(new GpTimelineBarExtension
        {
            Metadata = new GpTimelineBarMetadata
            {
                MasterBarXml = masterBar.Xml,
                DirectionsXml = masterBar.DirectionsXml,
                RepeatStartAttributePresent = masterBar.RepeatStartAttributePresent,
                RepeatEndAttributePresent = masterBar.RepeatEndAttributePresent,
                RepeatCountAttributePresent = masterBar.RepeatCountAttributePresent,
                HasExplicitEmptySection = masterBar.HasExplicitEmptySection,
                KeyTransposeAs = masterBar.KeyTransposeAs,
                DirectionProperties = masterBar.DirectionProperties.ToDictionary(kv => kv.Key, kv => kv.Value),
                Fermatas = masterBar.Fermatas.Select(f => new GpFermataMetadata
                {
                    Type = f.Type,
                    Offset = f.Offset,
                    Length = f.Length
                }).ToArray(),
                XProperties = masterBar.XProperties.ToDictionary(kv => kv.Key, kv => kv.Value),
                MasterBarXPropertiesXml = masterBar.XPropertiesXml
            }
        });
        return timelineBar;
    }

    private static StaffMeasure? MapStaffBar(
        GpifDocument source,
        GpifTrack track,
        int barId,
        int measureIndex,
        int staffIndex,
        bool isStringedTrack)
    {
        if (!source.BarsById.TryGetValue(barId, out var bar))
        {
            return null;
        }

        var voices = new List<Voice>();
        var voiceRefs = ReferenceListParser.SplitRefsPreservePlaceholders(bar.VoicesReferenceList);

        for (var voiceIndex = 0; voiceIndex < voiceRefs.Count; voiceIndex++)
        {
            if (!source.VoicesById.TryGetValue(voiceRefs[voiceIndex], out var voice))
            {
                continue;
            }

            var mappedBeats = MapVoiceBeats(source, track, voice, isStringedTrack);
            var mappedVoice = new Voice
            {
                VoiceIndex = voiceIndex,
                Beats = mappedBeats
            };
            mappedVoice.SetExtension(new GpVoiceExtension
            {
                Metadata = new GpVoiceMetadata
                {
                    Xml = voice.Xml,
                    SourceVoiceId = voice.Id,
                    Properties = voice.Properties.ToDictionary(kv => kv.Key, kv => kv.Value),
                    DirectionTags = voice.DirectionTags.ToArray()
                }
            });
            voices.Add(mappedVoice);
        }

        var beats = voices.FirstOrDefault(v => v.VoiceIndex == 0)?.Beats
            ?? voices.FirstOrDefault()?.Beats
            ?? Array.Empty<Beat>();

        var staffMeasure = new StaffMeasure
        {
            Index = measureIndex,
            StaffIndex = staffIndex,
            Clef = bar.Clef,
            SimileMark = bar.SimileMark,
            Voices = voices,
            Beats = beats
        };
        staffMeasure.SetExtension(new GpMeasureStaffExtension
        {
            Metadata = new GpMeasureStaffMetadata
            {
                BarXml = bar.Xml,
                SourceBarId = bar.Id,
                Properties = bar.Properties.ToDictionary(kv => kv.Key, kv => kv.Value),
                XProperties = bar.XProperties.ToDictionary(kv => kv.Key, kv => kv.Value),
                BarXPropertiesXml = bar.XPropertiesXml
            }
        });
        return staffMeasure;
    }

    private static IReadOnlyList<Beat> MapVoiceBeats(GpifDocument source, GpifTrack track, GpifVoice voice, bool isStringedTrack)
    {
        var beatRefs = ReferenceListParser.SplitRefs(voice.BeatsReferenceList);
        var beats = new List<Beat>(beatRefs.Count);

        var offset = ScoreTime.Zero;
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
                    var slideKinds = ArticulationDecoders.DecodeSlides(n.Articulation.SlideFlags);
                    var tieOriginNoteId = n.Articulation.TieDestination
                        ? ResolveAdjacentNoteId(
                            source,
                            previousBeat,
                            n,
                            stringNumber,
                            isStringedTrack,
                            candidate => candidate.Articulation.TieOrigin,
                            preferMatchingPitch: true)
                        : null;
                    var tieDestinationNoteId = n.Articulation.TieOrigin
                        ? ResolveAdjacentNoteId(
                            source,
                            nextBeat,
                            n,
                            stringNumber,
                            isStringedTrack,
                            candidate => candidate.Articulation.TieDestination,
                            preferMatchingPitch: true)
                        : null;
                    var hopoOriginNoteId = n.Articulation.HopoDestination
                        ? ResolveAdjacentNoteId(
                            source,
                            previousBeat,
                            n,
                            stringNumber,
                            isStringedTrack,
                            candidate => candidate.Articulation.HopoOrigin)
                        : null;
                    var hopoDestinationNoteId = n.Articulation.HopoOrigin
                        ? ResolveAdjacentNoteId(
                            source,
                            nextBeat,
                            n,
                            stringNumber,
                            isStringedTrack,
                            candidate => candidate.Articulation.HopoDestination)
                        : null;
                    var slideOriginNoteId = HasIncomingSlide(slideKinds)
                        ? ResolveAdjacentNoteId(
                            source,
                            previousBeat,
                            n,
                            stringNumber,
                            isStringedTrack,
                            static _ => true)
                        : null;
                    var slideDestinationNoteId = HasOutgoingSlide(slideKinds)
                        ? ResolveAdjacentNoteId(
                            source,
                            nextBeat,
                            n,
                            stringNumber,
                            isStringedTrack,
                            static _ => true)
                        : null;
                    var hopoType = InferHopoType(source, n, hopoOriginNoteId, hopoDestinationNoteId);
                    var soundingPitch = n.MidiPitch.HasValue
                        ? Pitch.FromMidiNumber(n.MidiPitch.Value)
                        : MapPitchValue(n.ConcertPitch);

                    var note = new Note
                    {
                        Id = n.Id,
                        Velocity = n.Velocity,
                        Pitch = soundingPitch,
                        ShowStringNumber = n.ShowStringNumber,
                        StringNumber = stringNumber,
                        Duration = duration,
                        SoundingDuration = duration,
                        Articulation = new NoteArticulation
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
                            PalmMuted = n.Articulation.PalmMuted,
                            Muted = n.Articulation.Muted,
                            Tapped = n.Articulation.Tapped,
                            LeftHandTapped = n.Articulation.LeftHandTapped,
                            HopoOrigin = n.Articulation.HopoOrigin,
                            HopoDestination = n.Articulation.HopoDestination,
                            HopoType = hopoType,
                            Slides = slideKinds,
                            Bend = ArticulationDecoders.DecodeBend(n.Articulation, n.Articulation.TieDestination),
                            Harmonic = ArticulationDecoders.DecodeHarmonic(n.Articulation),
                            Relations = BuildNoteRelations(
                                n,
                                hopoType,
                                tieOriginNoteId,
                                tieDestinationNoteId,
                                hopoOriginNoteId,
                                hopoDestinationNoteId,
                                slideOriginNoteId,
                                slideDestinationNoteId)
                        }
                    };
                    note.SetExtension(new GpNoteExtension
                    {
                        Metadata = new GpNoteMetadata
                        {
                            Xml = n.Xml,
                            SourceMidiPitch = n.MidiPitch,
                            SourceTransposedMidiPitch = ResolveSourceTransposedMidiPitch(n.MidiPitch, track.Transpose),
                            SourceConcertPitch = MapPitchValue(n.ConcertPitch),
                            SourceTransposedPitch = MapPitchValue(n.TransposedPitch),
                            SourceFret = n.SourceFret,
                            SourceStringNumber = n.SourceStringNumber,
                            SourceSlideFlags = n.Articulation.SlideFlags,
                            InstrumentArticulation = n.Articulation.InstrumentArticulation,
                            AntiAccentValue = n.Articulation.AntiAccentValue,
                            XProperties = n.XProperties.ToDictionary(kv => kv.Key, kv => kv.Value),
                            XPropertiesXml = n.XPropertiesXml
                        }
                    });
                    return note;
                })
                .ToArray();

            var mappedBeat = new Beat
            {
                Id = beat.Id,
                GraceType = beat.GraceType,
                Slashed = beat.Slashed,
                Slapped = beat.Slapped,
                Popped = beat.Popped,
                PalmMuted = notes.Any(n => n.Articulation.PalmMuted),
                Brush = beat.Brush,
                BrushIsUp = beat.BrushIsUp,
                Arpeggio = beat.Arpeggio,
                Rasgueado = beat.Rasgueado,
                DeadSlapped = beat.DeadSlapped,
                Tremolo = beat.Tremolo,
                WhammyBar = ArticulationDecoders.DecodeWhammyBar(beat),
                Offset = offset,
                Duration = duration,
                Rhythm = MapRhythmValue(source, beat.RhythmRef),
                Notes = notes
            };
            mappedBeat.SetExtension(new GpBeatExtension
            {
                Metadata = new GpBeatMetadata
                {
                    Xml = beat.Xml,
                    SourceRhythmId = beat.RhythmRef,
                    Dynamic = beat.Dynamic,
                    Golpe = beat.Golpe,
                    Hairpin = beat.Hairpin,
                    Ottavia = beat.Ottavia,
                    LegatoOrigin = beat.LegatoOrigin,
                    LegatoDestination = beat.LegatoDestination,
                    PickStrokeDirection = beat.PickStrokeDirection,
                    BrushDurationTicks = beat.BrushDurationTicks,
                    RasgueadoPattern = beat.RasgueadoPattern,
                    TremoloValue = beat.TremoloValue,
                    FreeText = beat.FreeText,
                    SourceRhythm = MapRhythmShape(source, beat.RhythmRef),
                    TransposedPitchStemOrientation = beat.TransposedPitchStemOrientation,
                    UserTransposedPitchStemOrientation = beat.UserTransposedPitchStemOrientation,
                    HasTransposedPitchStemOrientationUserDefinedElement = beat.HasTransposedPitchStemOrientationUserDefinedElement,
                    ConcertPitchStemOrientation = beat.ConcertPitchStemOrientation,
                    Fadding = beat.Fadding,
                    Variation = beat.Variation,
                    Wah = beat.Wah,
                    ChordId = beat.ChordId,
                    LyricsXml = beat.LyricsXml,
                    VibratoWithTremBarStrength = beat.VibratoWithTremBarStrength,
                    Properties = beat.Properties.ToDictionary(kv => kv.Key, kv => kv.Value),
                    XProperties = beat.XProperties.ToDictionary(kv => kv.Key, kv => kv.Value),
                    BrushDurationXPropertyId = beat.BrushDurationXPropertyId,
                    HasExplicitBrushDurationXProperty = beat.HasExplicitBrushDurationXProperty,
                    WhammyUsesElement = beat.WhammyUsesElement,
                    WhammyExtendUsesElement = beat.WhammyExtendUsesElement,
                    XPropertiesXml = beat.XPropertiesXml
                }
            });
            beats.Add(mappedBeat);

            offset += duration;
        }

        return beats;
    }

    private static GpRhythmShapeMetadata? MapRhythmShape(GpifDocument source, int rhythmRef)
    {
        if (!source.RhythmsById.TryGetValue(rhythmRef, out var rhythm))
        {
            return null;
        }

        return new GpRhythmShapeMetadata
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

    private static RhythmValue? MapRhythmValue(GpifDocument source, int rhythmRef)
    {
        if (!source.RhythmsById.TryGetValue(rhythmRef, out var rhythm))
        {
            return null;
        }

        return new RhythmValue
        {
            BaseValue = ToNoteValueKind(rhythm.NoteValue),
            AugmentationDots = rhythm.AugmentationDots,
            PrimaryTuplet = ToTupletModel(rhythm.PrimaryTuplet),
            SecondaryTuplet = ToTupletModel(rhythm.SecondaryTuplet)
        };
    }

    private static CoreTupletRatio? ToTupletModel(RawTupletRatio? tuplet)
        => tuplet is null
            ? null
            : new CoreTupletRatio
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

    private static int? ResolveAdjacentNoteId(
        GpifDocument source,
        GpifBeat? adjacentBeat,
        GpifNote note,
        int? stringNumber,
        bool isStringedTrack,
        Func<GpifNote, bool> fallbackPredicate,
        bool preferMatchingPitch = false)
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

        if (preferMatchingPitch && note.MidiPitch.HasValue)
        {
            var samePitchOnString = adjacentNotes.FirstOrDefault(candidate =>
                candidate.MidiPitch == note.MidiPitch
                && (!isStringedTrack || !stringNumber.HasValue || GetStringNumber(candidate) == stringNumber));
            if (samePitchOnString is not null)
            {
                return samePitchOnString.Id;
            }
        }

        if (isStringedTrack && stringNumber.HasValue)
        {
            var sameStringMatch = adjacentNotes
                .FirstOrDefault(n => GetStringNumber(n) == stringNumber);
            if (sameStringMatch is not null)
            {
                return sameStringMatch.Id;
            }
        }

        return adjacentNotes
            .FirstOrDefault(fallbackPredicate)
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

    private static ScoreTime ResolveDuration(GpifDocument source, int rhythmRef)
        => RhythmValue.ResolveDuration(MapRhythmValue(source, rhythmRef));

    private static TempoEventMetadata ParseTempo(GpifAutomation a)
    {
        var (bpm, den) = ParseAutomationValueTokens(a.Value);

        return new TempoEventMetadata
        {
            Bar = a.Bar,
            Offset = a.Position,
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
            .ThenBy(e => e.Offset.HasValue ? 0 : 1)
            .ThenBy(e => e.Offset ?? ScoreTime.Zero)
            .ThenBy(e => e.Scope)
            .ThenBy(e => e.TrackId ?? int.MaxValue)
            .ThenBy(e => e.Type, StringComparer.OrdinalIgnoreCase)
            .ThenBy(e => e.Value, StringComparer.Ordinal)
            .ToArray();
    }

    private static IReadOnlyList<DynamicEventMetadata> BuildDynamicMap(Score score)
    {
        var map = new List<DynamicEventMetadata>();
        var lastDynamicByTrackVoice = new Dictionary<(int TrackId, int VoiceIndex), string>();
        var dynamicLookup = score.PointControls
            .Where(control =>
                control.Kind == PointControlKind.Dynamic
                && !string.IsNullOrWhiteSpace(control.Value)
                && control.TrackId.HasValue
                && control.StaffIndex.HasValue
                && control.VoiceIndex.HasValue)
            .GroupBy(control => (
                TrackId: control.TrackId!.Value,
                StaffIndex: control.StaffIndex!.Value,
                VoiceIndex: control.VoiceIndex!.Value,
                BarIndex: control.Position.BarIndex,
                control.Position.Offset))
            .ToDictionary(
                group => group.Key,
                group => group.Last().Value!,
                EqualityComparer<(int TrackId, int StaffIndex, int VoiceIndex, int BarIndex, ScoreTime Offset)>.Default);

        foreach (var track in score.Tracks.OrderBy(t => t.Id))
        {
            foreach (var measure in EnumeratePrimaryStaffMeasures(track))
            {
                if (measure.Voices.Count > 0)
                {
                    foreach (var voice in measure.Voices.OrderBy(v => v.VoiceIndex))
                    {
                        AppendDynamicEvents(
                            map,
                            lastDynamicByTrackVoice,
                            dynamicLookup,
                            track.Id,
                            measure.StaffIndex,
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
                        dynamicLookup,
                        track.Id,
                        measure.StaffIndex,
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

    private static IReadOnlyList<StaffMeasure> EnumeratePrimaryStaffMeasures(Track track)
    {
        var primaryStaff = track.Staves
            .OrderBy(staff => staff.StaffIndex)
            .FirstOrDefault(staff => staff.StaffIndex == 0)
            ?? track.Staves.OrderBy(staff => staff.StaffIndex).FirstOrDefault();

        return primaryStaff?.Measures
            .OrderBy(measure => measure.Index)
            .ToArray()
            ?? Array.Empty<StaffMeasure>();
    }

    private static void AppendDynamicEvents(
        List<DynamicEventMetadata> map,
        Dictionary<(int TrackId, int VoiceIndex), string> lastDynamicByTrackVoice,
        IReadOnlyDictionary<(int TrackId, int StaffIndex, int VoiceIndex, int BarIndex, ScoreTime Offset), string> dynamicLookup,
        int trackId,
        int staffIndex,
        int measureIndex,
        int voiceIndex,
        IReadOnlyList<Beat> beats)
    {
        var key = (trackId, voiceIndex);

        foreach (var beat in beats)
        {
            if (!dynamicLookup.TryGetValue((trackId, staffIndex, voiceIndex, measureIndex, beat.Offset), out var dynamic)
                || string.IsNullOrWhiteSpace(dynamic))
            {
                continue;
            }

            if (lastDynamicByTrackVoice.TryGetValue(key, out var previousDynamic)
                && string.Equals(previousDynamic, dynamic, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            lastDynamicByTrackVoice[key] = dynamic;

            map.Add(new DynamicEventMetadata
            {
                TrackId = trackId,
                MeasureIndex = measureIndex,
                VoiceIndex = voiceIndex,
                BeatId = beat.Id,
                BeatOffset = beat.Offset,
                Dynamic = dynamic,
                Kind = ParseDynamicKind(dynamic)
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
            Offset = automation.Position,
            Visible = automation.Visible,
            Value = automation.Value,
            NumericValue = numericValue,
            ReferenceHint = referenceHint,
            Tempo = string.Equals(automation.Type, "Tempo", StringComparison.OrdinalIgnoreCase)
                ? ParseTempo(automation)
                : null
        };
    }

    private static NoteValueKind ToNoteValueKind(string noteValue)
        => noteValue switch
        {
            "Whole" => NoteValueKind.Whole,
            "Half" => NoteValueKind.Half,
            "Quarter" => NoteValueKind.Quarter,
            "Eighth" => NoteValueKind.Eighth,
            "16th" => NoteValueKind.Sixteenth,
            "32nd" => NoteValueKind.ThirtySecond,
            "64th" => NoteValueKind.SixtyFourth,
            "128th" => NoteValueKind.OneHundredTwentyEighth,
            "256th" => NoteValueKind.TwoHundredFiftySixth,
            _ => NoteValueKind.Unknown
        };

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

    private static Pitch? MapPitchValue(GpifPitchValue? pitch)
        => pitch is null
            ? null
            : new Pitch
            {
                Step = pitch.Step,
                Accidental = pitch.Accidental,
                Octave = pitch.Octave ?? 0
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

    private static IReadOnlyList<NoteRelation> BuildNoteRelations(
        GpifNote note,
        HopoTypeKind hopoType,
        int? tieOriginNoteId,
        int? tieDestinationNoteId,
        int? hopoOriginNoteId,
        int? hopoDestinationNoteId,
        int? slideOriginNoteId,
        int? slideDestinationNoteId)
    {
        var relations = new List<NoteRelation>();

        AddRelation(relations, NoteRelationKind.Tie, tieOriginNoteId);
        AddRelation(relations, NoteRelationKind.Tie, tieDestinationNoteId);

        var hopoRelationKind = hopoType switch
        {
            HopoTypeKind.HammerOn => NoteRelationKind.HammerOn,
            HopoTypeKind.PullOff => NoteRelationKind.PullOff,
            HopoTypeKind.Legato => NoteRelationKind.Legato,
            _ => (NoteRelationKind?)null
        };
        if (hopoRelationKind.HasValue)
        {
            AddRelation(relations, hopoRelationKind.Value, hopoOriginNoteId);
            AddRelation(relations, hopoRelationKind.Value, hopoDestinationNoteId);
        }

        if (note.Articulation.SlideFlags.HasValue)
        {
            AddRelation(relations, NoteRelationKind.Slide, slideOriginNoteId);
            AddRelation(relations, NoteRelationKind.Slide, slideDestinationNoteId);
        }

        return relations;
    }

    private static void AddRelation(List<NoteRelation> relations, NoteRelationKind kind, int? targetNoteId)
    {
        if (!targetNoteId.HasValue || relations.Any(relation => relation.Kind == kind && relation.TargetNoteId == targetNoteId.Value))
        {
            return;
        }

        relations.Add(new NoteRelation
        {
            Kind = kind,
            TargetNoteId = targetNoteId.Value
        });
    }

    private static bool HasIncomingSlide(IReadOnlyList<SlideType> slideKinds)
        => slideKinds.Contains(SlideType.IntoFromAbove)
           || slideKinds.Contains(SlideType.IntoFromBelow);

    private static bool HasOutgoingSlide(IReadOnlyList<SlideType> slideKinds)
        => slideKinds.Contains(SlideType.Shift)
           || slideKinds.Contains(SlideType.Legato)
           || slideKinds.Contains(SlideType.OutDown)
           || slideKinds.Contains(SlideType.OutUp)
           || slideKinds.Contains(SlideType.Unknown64)
           || slideKinds.Contains(SlideType.Unknown128);

    private static void PopulateTimelineGeometry(Score score)
    {
        var durationByBarIndex = new Dictionary<int, ScoreTime>();

        foreach (var track in score.Tracks)
        {
            foreach (var staff in track.Staves)
            {
                foreach (var measure in staff.Measures)
                {
                    var duration = ResolveMeasureDuration(measure);
                    if (durationByBarIndex.TryGetValue(measure.Index, out var existing) && existing >= duration)
                    {
                        continue;
                    }

                    durationByBarIndex[measure.Index] = duration;
                }
            }
        }

        var nextStart = ScoreTime.Zero;
        foreach (var timelineBar in score.TimelineBars.OrderBy(bar => bar.Index))
        {
            var duration = durationByBarIndex.TryGetValue(timelineBar.Index, out var measuredDuration)
                && measuredDuration > ScoreTime.Zero
                ? measuredDuration
                : ParseNominalBarDuration(timelineBar.TimeSignature);

            timelineBar.Start = nextStart;
            timelineBar.Duration = duration;
            nextStart += duration;
        }
    }

    private static ScoreTime ResolveMeasureDuration(StaffMeasure measure)
    {
        if (measure.Voices.Count > 0)
        {
            return measure.Voices
                .Select(voice => ResolveBeatSequenceDuration(voice.Beats))
                .DefaultIfEmpty(ScoreTime.Zero)
                .Max();
        }

        return ResolveBeatSequenceDuration(measure.Beats);
    }

    private static ScoreTime ResolveBeatSequenceDuration(IReadOnlyList<Beat> beats)
        => beats.Select(beat => beat.Offset + beat.Duration)
            .DefaultIfEmpty(ScoreTime.Zero)
            .Max();

    private static ScoreTime ParseNominalBarDuration(string timeSignature)
    {
        var parts = timeSignature.Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 2
               && int.TryParse(parts[0], out var numerator)
               && int.TryParse(parts[1], out var denominator)
               && denominator > 0
            ? new ScoreTime(numerator, denominator)
            : ScoreTime.Zero;
    }

    private static IReadOnlyList<PointControlEvent> BuildPointControls(
        Score score,
        IReadOnlyList<TempoEventMetadata> tempoMap)
    {
        var pointControls = tempoMap
            .Where(tempo => tempo.Bpm.HasValue)
            .Select(tempo => new PointControlEvent
            {
                Kind = PointControlKind.Tempo,
                Scope = ControlScopeKind.Score,
                Position = new WrittenPosition
                {
                    BarIndex = tempo.Bar ?? 0,
                    Offset = tempo.Offset ?? ScoreTime.Zero
                },
                NumericValue = tempo.Bpm!.Value
            })
            .ToList();

        foreach (var track in score.Tracks.OrderBy(track => track.Id))
        {
            foreach (var staff in track.Staves.OrderBy(staff => staff.StaffIndex))
            {
                foreach (var measure in staff.Measures.OrderBy(measure => measure.Index))
                {
                    var voices = measure.Voices.Count > 0
                        ? measure.Voices.OrderBy(voice => voice.VoiceIndex).ToArray()
                        : [new Voice { VoiceIndex = 0, Beats = measure.Beats }];

                    foreach (var voice in voices)
                    {
                        foreach (var beat in voice.Beats)
                        {
                            var beatMetadata = beat.GetGuitarPro()?.Metadata;
                            if (string.IsNullOrWhiteSpace(beatMetadata?.Dynamic))
                            {
                                continue;
                            }

                            pointControls.Add(new PointControlEvent
                            {
                                Kind = PointControlKind.Dynamic,
                                Scope = ControlScopeKind.Voice,
                                TrackId = track.Id,
                                StaffIndex = staff.StaffIndex,
                                VoiceIndex = voice.VoiceIndex,
                                Position = new WrittenPosition
                                {
                                    BarIndex = measure.Index,
                                    Offset = beat.Offset
                                },
                                Value = beatMetadata.Dynamic
                            });
                        }
                    }
                }
            }
        }

        foreach (var timelineBar in score.TimelineBars.OrderBy(bar => bar.Index))
        {
            foreach (var fermata in timelineBar.GetGuitarPro()?.Metadata.Fermatas ?? Array.Empty<GpFermataMetadata>())
            {
                pointControls.Add(new PointControlEvent
                {
                    Kind = PointControlKind.Fermata,
                    Scope = ControlScopeKind.Score,
                    Position = new WrittenPosition
                    {
                        BarIndex = timelineBar.Index,
                        Offset = ResolveFermataOffset(fermata.Offset, timelineBar.Duration)
                    },
                    Value = fermata.Type,
                    Placement = fermata.Offset,
                    Length = fermata.Length
                });
            }
        }

        return pointControls;
    }

    private static IReadOnlyList<SpanControlEvent> BuildSpanControls(Score score)
    {
        var spanControls = new List<SpanControlEvent>();
        var seenLegatoSpans = new HashSet<(int TrackId, int StaffIndex, int VoiceIndex, int StartBarIndex, ScoreTime StartOffset, int EndBarIndex, ScoreTime EndOffset)>();

        foreach (var track in score.Tracks.OrderBy(track => track.Id))
        {
            foreach (var staff in track.Staves.OrderBy(staff => staff.StaffIndex))
            {
                foreach (var measure in staff.Measures.OrderBy(measure => measure.Index))
                {
                    var voices = measure.Voices.Count > 0
                        ? measure.Voices.OrderBy(voice => voice.VoiceIndex).ToArray()
                        : [new Voice { VoiceIndex = 0, Beats = measure.Beats }];

                    foreach (var voice in voices)
                    {
                        for (var beatIndex = 0; beatIndex < voice.Beats.Count; beatIndex++)
                        {
                            var beat = voice.Beats[beatIndex];
                            var beatExtension = beat.GetGuitarPro();
                            var nextBeat = beatIndex + 1 < voice.Beats.Count ? voice.Beats[beatIndex + 1] : null;
                            var position = new WrittenPosition
                            {
                                BarIndex = measure.Index,
                                Offset = beat.Offset
                            };

                            if (!string.IsNullOrWhiteSpace(beatExtension?.Metadata.Hairpin))
                            {
                                spanControls.Add(new SpanControlEvent
                                {
                                    Kind = SpanControlKind.Hairpin,
                                    Scope = ControlScopeKind.Voice,
                                    TrackId = track.Id,
                                    StaffIndex = staff.StaffIndex,
                                    VoiceIndex = voice.VoiceIndex,
                                    Start = position,
                                    Value = beatExtension.Metadata.Hairpin
                                });
                            }

                            if (!string.IsNullOrWhiteSpace(beatExtension?.Metadata.Ottavia))
                            {
                                spanControls.Add(new SpanControlEvent
                                {
                                    Kind = SpanControlKind.Ottava,
                                    Scope = ControlScopeKind.Voice,
                                    TrackId = track.Id,
                                    StaffIndex = staff.StaffIndex,
                                    VoiceIndex = voice.VoiceIndex,
                                    Start = position,
                                    Value = beatExtension.Metadata.Ottavia
                                });
                            }

                            var legatoOrigin = beatExtension?.Metadata.LegatoOrigin == true;
                            var legatoDestination = beatExtension?.Metadata.LegatoDestination == true;

                            if (legatoOrigin)
                            {
                                var end = nextBeat is null
                                    ? null
                                    : new WrittenPosition
                                    {
                                        BarIndex = measure.Index,
                                        Offset = nextBeat.Offset
                                    };
                                if (!RememberLegatoSpan(
                                        seenLegatoSpans,
                                        track.Id,
                                        staff.StaffIndex,
                                        voice.VoiceIndex,
                                        position,
                                        end))
                                {
                                    continue;
                                }

                                spanControls.Add(new SpanControlEvent
                                {
                                    Kind = SpanControlKind.Legato,
                                    Scope = ControlScopeKind.Voice,
                                    TrackId = track.Id,
                                    StaffIndex = staff.StaffIndex,
                                    VoiceIndex = voice.VoiceIndex,
                                    Start = position,
                                    End = end
                                });
                            }
                            else if (legatoDestination && beatIndex > 0)
                            {
                                var start = new WrittenPosition
                                {
                                    BarIndex = measure.Index,
                                    Offset = voice.Beats[beatIndex - 1].Offset
                                };
                                if (!RememberLegatoSpan(
                                        seenLegatoSpans,
                                        track.Id,
                                        staff.StaffIndex,
                                        voice.VoiceIndex,
                                        start,
                                        position))
                                {
                                    continue;
                                }

                                spanControls.Add(new SpanControlEvent
                                {
                                    Kind = SpanControlKind.Legato,
                                    Scope = ControlScopeKind.Voice,
                                    TrackId = track.Id,
                                    StaffIndex = staff.StaffIndex,
                                    VoiceIndex = voice.VoiceIndex,
                                    Start = start,
                                    End = position
                                });
                            }
                        }
                    }
                }
            }
        }

        return spanControls;
    }

    private static bool RememberLegatoSpan(
        HashSet<(int TrackId, int StaffIndex, int VoiceIndex, int StartBarIndex, ScoreTime StartOffset, int EndBarIndex, ScoreTime EndOffset)> seenLegatoSpans,
        int trackId,
        int staffIndex,
        int voiceIndex,
        WrittenPosition start,
        WrittenPosition? end)
        => seenLegatoSpans.Add((
            trackId,
            staffIndex,
            voiceIndex,
            start.BarIndex,
            start.Offset,
            end?.BarIndex ?? -1,
            end?.Offset ?? ScoreTime.Zero));

    private static ScoreTime ResolveFermataOffset(string offset, ScoreTime barDuration)
        => offset.Trim().ToUpperInvariant() switch
        {
            "START" => ScoreTime.Zero,
            "MIDDLE" or "CENTER" => barDuration.Multiply(1, 2),
            "END" => barDuration,
            _ => ScoreTime.Zero
        };

    private static void ApplyTieDurationStitching(IReadOnlyList<StaffMeasure> measures)
    {
        var orderedNotes = measures
            .SelectMany(m => m.Voices.Count > 0
                ? m.Voices.SelectMany(v => v.Beats)
                : m.Beats)
            .SelectMany(b => b.Notes)
            .Where(n => n.Pitch is not null)
            .ToArray();
        var noteById = new Dictionary<int, Note>();
        foreach (var note in orderedNotes)
        {
            noteById.TryAdd(note.Id, note);
        }
        var carryByPitch = new Dictionary<int, Note>();

        foreach (var note in orderedNotes)
        {
            var pitch = note.Pitch!.MidiNumber;
            var explicitTieOrigin = note.Articulation.Relations
                .FirstOrDefault(relation => relation.Kind == NoteRelationKind.Tie);

            if (note.Articulation.TieDestination
                && explicitTieOrigin is not null
                && noteById.TryGetValue(explicitTieOrigin.TargetNoteId, out var explicitPrevious))
            {
                explicitPrevious.SoundingDuration += note.Duration;
            }
            else if (note.Articulation.TieDestination && carryByPitch.TryGetValue(pitch, out var previous))
            {
                previous.SoundingDuration += note.Duration;
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
