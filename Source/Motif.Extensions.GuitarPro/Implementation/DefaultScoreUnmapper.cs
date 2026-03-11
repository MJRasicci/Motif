namespace Motif.Extensions.GuitarPro.Implementation;

using Motif.Extensions.GuitarPro.Abstractions;
using Motif.Extensions.GuitarPro.Models;
using Motif.Extensions.GuitarPro.Models.Raw;
using Motif.Extensions.GuitarPro.Models.Write;
using Motif.Extensions.GuitarPro.Utilities;
using Motif.Models;
using System.Xml.Linq;
using CoreTupletRatio = Motif.Models.TupletRatio;
using RawTupletRatio = Motif.Extensions.GuitarPro.Models.Raw.TupletRatio;

internal sealed class DefaultScoreUnmapper : IScoreUnmapper
{
    public ValueTask<WriteResult> UnmapAsync(Score score, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(score);
        cancellationToken.ThrowIfCancellationRequested();

        var diagnostics = new WriteDiagnostics();
        AppendGuitarProFidelityDiagnostics(score, diagnostics);
        var hasScoreLevelSourceContext = HasScoreLevelSourceContext(score);
        var regenerationDiagnostics = hasScoreLevelSourceContext
            ? new RegenerationDiagnostics()
            : null;
        var scoreExtension = score.GetGuitarPro();
        var scoreMetadata = scoreExtension?.Metadata ?? new ScoreMetadata();
        var masterTrackMetadata = scoreExtension?.MasterTrack ?? new MasterTrackMetadata();
        var orderedTracks = score.Tracks
            .OrderBy(t => t.Id)
            .ToArray();
        var orderedTimelineBars = score.TimelineBars
            .OrderBy(timelineBar => timelineBar.Index)
            .ToArray();
        var masterTrackIds = masterTrackMetadata.TrackIds.Length > 0
            ? masterTrackMetadata.TrackIds
            : orderedTracks.Select(t => t.Id).ToArray();

        var tracks = orderedTracks
            .Select(t =>
            {
                var metadata = GetTrackMetadata(t);
                var currentStaffMetadata = ResolveTrackStaffMetadata(t);
                var stavesXml = ResolveStavesXml(metadata, currentStaffMetadata);
                if (regenerationDiagnostics is not null
                    && HasSourceStavesXml(metadata)
                    && string.IsNullOrEmpty(stavesXml))
                {
                    regenerationDiagnostics.RecordTrackStavesXml(FormatTrackPath(t));
                }

                var structuredStaffs = ShouldEmitStructuredStaffs(metadata, currentStaffMetadata)
                    ? currentStaffMetadata.Select(s => new GpifStaff
                    {
                        Id = s.Id,
                        Cref = s.Cref,
                        TuningPitches = s.TuningPitches,
                        CapoFret = s.CapoFret,
                        Properties = s.Properties,
                        Xml = s.Xml
                    }).ToArray()
                    : Array.Empty<GpifStaff>();

                return new GpifTrack
                {
                    Xml = metadata.Xml,
                    Id = t.Id,
                    Name = t.Name,
                    ShortName = metadata.ShortName,
                    HasExplicitEmptyShortName = metadata.HasExplicitEmptyShortName,
                    Color = metadata.Color,
                    SystemsDefaultLayout = metadata.SystemsDefaultLayout,
                    SystemsLayout = metadata.SystemsLayout,
                    HasExplicitEmptySystemsLayout = metadata.HasExplicitEmptySystemsLayout,
                    PalmMute = metadata.PalmMute,
                    AutoAccentuation = metadata.AutoAccentuation,
                    AutoBrush = metadata.AutoBrush,
                    LetRingThroughout = metadata.LetRingThroughout,
                    PlayingStyle = metadata.PlayingStyle,
                    UseOneChannelPerString = metadata.UseOneChannelPerString,
                    IconId = metadata.IconId,
                    ForcedSound = metadata.ForcedSound,
                    TuningPitches = metadata.TuningPitches,
                    TuningInstrument = metadata.TuningInstrument,
                    TuningLabel = metadata.TuningLabel,
                    TuningLabelVisible = metadata.TuningLabelVisible,
                    HasTrackTuningProperty = metadata.HasTrackTuningProperty,
                    Properties = metadata.Properties,
                    InstrumentSetXml = metadata.InstrumentSetXml,
                    StavesXml = stavesXml,
                    SoundsXml = metadata.SoundsXml,
                    RseXml = metadata.RseXml,
                    NotationPatchXml = metadata.NotationPatchXml,
                    InstrumentSet = new GpifInstrumentSet
                    {
                        Name = metadata.InstrumentSet.Name,
                        Type = metadata.InstrumentSet.Type,
                        LineCount = metadata.InstrumentSet.LineCount,
                        Elements = metadata.InstrumentSet.Elements.Select(element => new GpifInstrumentElement
                        {
                            Name = element.Name,
                            Type = element.Type,
                            SoundbankName = element.SoundbankName,
                            Articulations = element.Articulations.Select(articulation => new GpifInstrumentArticulation
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
                    Sounds = metadata.Sounds.Select(s => new GpifSound
                    {
                        Name = s.Name,
                        Label = s.Label,
                        Path = s.Path,
                        Role = s.Role,
                        MidiLsb = s.MidiLsb,
                        MidiMsb = s.MidiMsb,
                        MidiProgram = s.MidiProgram,
                        Rse = new GpifSoundRse
                        {
                            SoundbankPatch = s.Rse.SoundbankPatch,
                            SoundbankSet = s.Rse.SoundbankSet,
                            ElementsSettingsXml = s.Rse.ElementsSettingsXml,
                            Pickups = new GpifSoundRsePickups
                            {
                                OverloudPosition = s.Rse.Pickups.OverloudPosition,
                                Volumes = s.Rse.Pickups.Volumes,
                                Tones = s.Rse.Pickups.Tones
                            },
                            EffectChain = s.Rse.EffectChain.Select(effect => new GpifRseEffect
                            {
                                Id = effect.Id,
                                Bypass = effect.Bypass,
                                Parameters = effect.Parameters
                            }).ToArray()
                        }
                    }).ToArray(),
                    ChannelRse = new GpifRse
                    {
                        Bank = metadata.Rse.Bank,
                        ChannelStripVersion = metadata.Rse.ChannelStripVersion,
                        ChannelStripParameters = metadata.Rse.ChannelStripParameters,
                        Automations = metadata.Rse.Automations.Select(a => new GpifAutomation
                        {
                            Type = a.Type,
                            Linear = a.Linear,
                            Bar = a.Bar,
                            Position = a.Position,
                            Visible = a.Visible,
                            Value = a.Value
                        }).ToArray()
                    },
                    PlaybackStateXml = metadata.PlaybackStateXml,
                    AudioEngineStateXml = metadata.AudioEngineStateXml,
                    PlaybackState = new GpifPlaybackState { Value = metadata.PlaybackState.Value },
                    AudioEngineState = new GpifAudioEngineState { Value = metadata.AudioEngineState.Value },
                    MidiConnectionXml = metadata.MidiConnectionXml,
                    LyricsXml = metadata.LyricsXml,
                    AutomationsXml = metadata.AutomationsXml,
                    Automations = metadata.Automations.Select(a => new GpifAutomation
                    {
                        Type = a.Type,
                        Linear = a.Linear,
                        Bar = a.Bar,
                        Position = a.Position,
                        Visible = a.Visible,
                        Value = a.Value
                    }).ToArray(),
                    TransposeXml = metadata.TransposeXml,
                    MidiConnection = new GpifMidiConnection
                    {
                        Port = metadata.MidiConnection.Port,
                        PrimaryChannel = metadata.MidiConnection.PrimaryChannel,
                        SecondaryChannel = metadata.MidiConnection.SecondaryChannel,
                        ForceOneChannelPerString = metadata.MidiConnection.ForceOneChannelPerString
                    },
                    Lyrics = new GpifLyrics
                    {
                        Dispatched = metadata.Lyrics.Dispatched,
                        Lines = metadata.Lyrics.Lines.Select(line => new GpifLyricsLine
                        {
                            Text = line.Text,
                            Offset = line.Offset
                        }).ToArray()
                    },
                    Transpose = new GpifTranspose
                    {
                        Chromatic = metadata.Transpose.Chromatic,
                        Octave = metadata.Transpose.Octave
                    },
                    Staffs = structuredStaffs
                };
            })
            .ToArray();

        var barId = NextIdAfter(orderedTracks
            .SelectMany(EnumeratePreferredStaffMeasures)
            .Select(staff => GetMeasureStaffMetadata(staff).SourceBarId));
        var voiceId = NextIdAfter(orderedTracks
            .SelectMany(EnumeratePreferredVoices)
            .Select(v => GetVoiceMetadata(v).SourceVoiceId));
        var beatId = NextIdAfter(orderedTracks.SelectMany(EnumeratePreferredBeats).Select(b => b.Id));
        var noteId = NextIdAfter(orderedTracks.SelectMany(EnumeratePreferredBeats).SelectMany(b => b.Notes).Select(n => n.Id));
        var rhythmId = 0;

        var bars = new Dictionary<int, GpifBar>();
        var voices = new Dictionary<int, GpifVoice>();
        var beats = new Dictionary<int, GpifBeat>();
        var notes = new Dictionary<int, GpifNote>();
        var rhythms = new Dictionary<int, GpifRhythm>();
        var remappedBarIdsBySourceId = new Dictionary<int, List<int>>();
        var remappedVoiceIdsBySourceId = new Dictionary<int, List<int>>();
        var remappedBeatIdsBySourceId = new Dictionary<int, List<int>>();
        var remappedNoteIdsBySourceId = new Dictionary<int, List<int>>();
        var remappedRhythmIdsBySourceId = new Dictionary<int, List<int>>();
        var rhythmIdsBySignature = new Dictionary<RhythmSignature, int>();
        var masterBars = new List<GpifMasterBar>();

        var maxMeasures = Math.Max(
            orderedTracks.Select(GetTrackMeasureCount).DefaultIfEmpty(0).Max(),
            orderedTimelineBars.Length);

        for (var m = 0; m < maxMeasures; m++)
        {
            var measureBarIds = new List<int>();
            var timelineBar = m < orderedTimelineBars.Length
                ? orderedTimelineBars[m]
                : null;
            var timelineIndex = timelineBar?.Index
                ?? GetScoreBarIndexAtPosition(orderedTracks, m)
                ?? m;
            var currentTimelineBar = timelineBar ?? CreateDefaultTimelineBar(timelineIndex);
            masterBars.Add(CreateMasterBar(currentTimelineBar));

            foreach (var track in orderedTracks)
            {
                var staffBars = ResolveTrackStaffMeasures(track, timelineIndex, m);

                foreach (var staffBar in staffBars)
                {
                    var staffMetadata = GetMeasureStaffMetadata(staffBar);
                    var currentBarId = staffMetadata.SourceBarId;
                    var measureVoices = ResolveMeasureVoices(staffBar.Voices, staffBar.Beats);
                    var voiceSlots = CreateVoiceSlots(measureVoices);

                    foreach (var measureVoice in measureVoices)
                    {
                        var beatIds = new List<int>();

                        for (var beatIndex = 0; beatIndex < measureVoice.Beats.Count; beatIndex++)
                        {
                            var beat = measureVoice.Beats[beatIndex];
                            var noteRefs = new List<int>();
                            var encodedWhammy = ArticulationDecoders.EncodeWhammyBar(beat.WhammyBar);
                            var beatMetadata = GetBeatMetadata(beat);
                            var beatXProperties = beatMetadata.XProperties.ToDictionary(kv => kv.Key, kv => kv.Value);
                            if (!BrushDurationXPropertiesMatch(beat, beatXProperties))
                            {
                                beatXProperties.Remove("687931393");
                                beatXProperties.Remove("687935489");
                            }

                            if (beat.BrushDurationTicks.HasValue)
                            {
                                var shouldWriteBrushDurationXProperty = beatMetadata.HasExplicitBrushDurationXProperty
                                    || beatMetadata.XProperties.ContainsKey("687931393")
                                    || beatMetadata.XProperties.ContainsKey("687935489")
                                    || beat.BrushDurationTicks.Value != 60;

                                if (shouldWriteBrushDurationXProperty)
                                {
                                    var brushDurationXPropertyId = !string.IsNullOrWhiteSpace(beatMetadata.BrushDurationXPropertyId)
                                        ? beatMetadata.BrushDurationXPropertyId
                                        : beat.Arpeggio
                                            ? "687931393"
                                            : "687935489";
                                    beatXProperties[brushDurationXPropertyId] = beat.BrushDurationTicks.Value;
                                }
                            }
                            if (beat.Notes.Count > 0)
                            {
                                for (var noteIndex = 0; noteIndex < beat.Notes.Count; noteIndex++)
                                {
                                    var note = beat.Notes[noteIndex];
                                    var bend = ArticulationDecoders.EncodeBend(note.Articulation.Bend);
                                    var harmonic = ArticulationDecoders.EncodeHarmonic(note.Articulation.Harmonic);
                                    var noteMetadata = GetNoteMetadata(note);
                                    var preserveSourceStringAndFret = ShouldPreserveSourceStringAndFret(note, track, staffBar.StaffIndex);
                                    if (regenerationDiagnostics is not null
                                        && HasSourceStringAndFret(noteMetadata)
                                        && !preserveSourceStringAndFret)
                                    {
                                        regenerationDiagnostics.RecordNoteStringFret(
                                            FormatNotePath(track, timelineIndex, staffBar, measureVoice, beatIndex, noteIndex));
                                    }

                                    var (resolvedStringNumber, resolvedFret) = ResolveStringAndFret(note, track, staffBar.StaffIndex, preserveSourceStringAndFret);
                                    var transposedMidiPitch = ResolveTransposedMidiPitch(note, track);
                                    var preserveSourceConcertPitch = ShouldPreserveSourceConcertPitch(note);
                                    if (regenerationDiagnostics is not null
                                        && noteMetadata.HadSourceConcertPitch
                                        && !preserveSourceConcertPitch)
                                    {
                                        regenerationDiagnostics.RecordNoteConcertPitch(
                                            FormatNotePath(track, timelineIndex, staffBar, measureVoice, beatIndex, noteIndex));
                                    }

                                    var preserveSourceTransposedPitch = ShouldPreserveSourceTransposedPitch(note, transposedMidiPitch);
                                    if (regenerationDiagnostics is not null
                                        && noteMetadata.HadSourceTransposedPitch
                                        && !preserveSourceTransposedPitch)
                                    {
                                        regenerationDiagnostics.RecordNoteTransposedPitch(
                                            FormatNotePath(track, timelineIndex, staffBar, measureVoice, beatIndex, noteIndex));
                                    }

                                    var noteXProperties = noteMetadata.XProperties.ToDictionary(kv => kv.Key, kv => kv.Value);
                                    noteXProperties.Remove("688062467");
                                    var encodedTrillSpeed = ResolveTrillSpeedXPropertyValue(note);
                                    if (encodedTrillSpeed.HasValue)
                                    {
                                        noteXProperties["688062467"] = encodedTrillSpeed.Value;
                                    }

                                    var noteCandidate = new GpifNote
                                    {
                                        Xml = noteMetadata.Xml,
                                        Id = 0,
                                        Velocity = note.Velocity,
                                        MidiPitch = note.MidiPitch,
                                        TransposedMidiPitch = transposedMidiPitch,
                                        ConcertPitch = preserveSourceConcertPitch
                                            ? ToRawPitchValue(note.ConcertPitch)
                                            : null,
                                        TransposedPitch = preserveSourceTransposedPitch
                                            ? ToRawPitchValue(note.TransposedPitch)
                                            : null,
                                        SourceFret = noteMetadata.SourceFret,
                                        SourceStringNumber = noteMetadata.SourceStringNumber,
                                        ShowStringNumber = note.ShowStringNumber,
                                        Properties = BuildCoreNoteProperties(note.MidiPitch, resolvedStringNumber, resolvedFret),
                                        XProperties = noteXProperties,
                                        XPropertiesXml = noteMetadata.XPropertiesXml,
                                        Articulation = new GpifNoteArticulation
                                        {
                                            LeftFingering = note.Articulation.LeftFingering,
                                            RightFingering = note.Articulation.RightFingering,
                                            Ornament = note.Articulation.Ornament,
                                            LetRing = note.Articulation.LetRing,
                                            Vibrato = note.Articulation.Vibrato,
                                            TieOrigin = note.Articulation.TieOrigin,
                                            TieDestination = note.Articulation.TieDestination,
                                            Trill = note.Articulation.Trill,
                                            Accent = note.Articulation.Accent,
                                            AntiAccent = note.Articulation.AntiAccent,
                                            AntiAccentValue = noteMetadata.AntiAccentValue,
                                            InstrumentArticulation = noteMetadata.InstrumentArticulation,
                                            PalmMuted = ResolveNotePalmMuted(note, beat),
                                            Muted = note.Articulation.Muted,
                                            Tapped = note.Articulation.Tapped,
                                            LeftHandTapped = note.Articulation.LeftHandTapped,
                                            HopoOrigin = note.Articulation.HopoOrigin,
                                            HopoDestination = note.Articulation.HopoDestination,
                                            SlideFlags = ResolveSlideFlags(note),
                                            BendEnabled = bend.Enabled,
                                            BendOriginOffset = bend.OriginOffset,
                                            BendOriginValue = bend.OriginValue,
                                            BendMiddleOffset1 = bend.MiddleOffset1,
                                            BendMiddleOffset2 = bend.MiddleOffset2,
                                            BendMiddleValue = bend.MiddleValue,
                                            BendDestinationOffset = bend.DestinationOffset,
                                            BendDestinationValue = bend.DestinationValue,
                                            HarmonicEnabled = harmonic.Enabled,
                                            HarmonicType = harmonic.TypeNumber,
                                            HarmonicTypeText = harmonic.TypeText,
                                            HarmonicFret = harmonic.Fret
                                        }
                                    };

                                    var currentNoteId = AllocateId(
                                        note.Id,
                                        noteCandidate,
                                        notes,
                                        ref noteId,
                                        NotesEqual,
                                        remappedNoteIdsBySourceId,
                                        diagnostics,
                                        code: "NOTE_ID_CONFLICT",
                                        category: "ReferenceReuse",
                                        message: $"Note id {note.Id} appeared with different content; a new raw note id was allocated.");

                                    noteRefs.Add(currentNoteId);
                                    if (!notes.ContainsKey(currentNoteId))
                                    {
                                        notes[currentNoteId] = WithNoteId(noteCandidate, currentNoteId);
                                    }
                                }
                            }

                            var preservedSourceRhythm = TryPreserveSourceRhythmShape(beat);
                            if (regenerationDiagnostics is not null
                                && beatMetadata.SourceRhythm is not null
                                && preservedSourceRhythm is null)
                            {
                                regenerationDiagnostics.RecordRhythmSourceShape(
                                    FormatBeatPath(track, timelineIndex, staffBar, measureVoice, beatIndex));
                            }

                            var rhythmCandidate = preservedSourceRhythm
                                ?? ToRhythm(beat.Duration, id: 0, diagnostics);
                            var rhythmSignature = new RhythmSignature(
                                rhythmCandidate.NoteValue,
                                rhythmCandidate.AugmentationDots,
                                rhythmCandidate.AugmentationDotUsesCountAttribute,
                                string.Join(",", rhythmCandidate.AugmentationDotCounts),
                                rhythmCandidate.PrimaryTuplet?.Numerator,
                                rhythmCandidate.PrimaryTuplet?.Denominator,
                                rhythmCandidate.SecondaryTuplet?.Numerator,
                                rhythmCandidate.SecondaryTuplet?.Denominator);
                            int currentRhythmId;
                            if (beatMetadata.SourceRhythmId >= 0)
                            {
                                currentRhythmId = AllocateId(
                                    beatMetadata.SourceRhythmId,
                                    rhythmCandidate,
                                    rhythms,
                                    ref rhythmId,
                                    RhythmsEqual,
                                    remappedRhythmIdsBySourceId,
                                    diagnostics,
                                    code: "RHYTHM_ID_CONFLICT",
                                    category: "ReferenceReuse",
                                    message: $"Rhythm id {beatMetadata.SourceRhythmId} appeared with different content; a new raw rhythm id was allocated.");
                            }
                            else if (!rhythmIdsBySignature.TryGetValue(rhythmSignature, out currentRhythmId))
                            {
                                currentRhythmId = NextAvailableId(rhythms, ref rhythmId);
                                rhythmIdsBySignature[rhythmSignature] = currentRhythmId;
                            }

                            if (!rhythms.ContainsKey(currentRhythmId))
                            {
                                rhythms[currentRhythmId] = WithRhythmId(rhythmCandidate, currentRhythmId);
                            }

                            var beatCandidate = new GpifBeat
                            {
                                Xml = beatMetadata.Xml,
                                Id = 0,
                                RhythmRef = currentRhythmId,
                                NotesReferenceList = ReferenceListFormatter.JoinRefs(noteRefs),
                                GraceType = beat.GraceType,
                                Dynamic = beat.Dynamic,
                                TransposedPitchStemOrientation = beatMetadata.TransposedPitchStemOrientation,
                                UserTransposedPitchStemOrientation = beatMetadata.UserTransposedPitchStemOrientation,
                                HasTransposedPitchStemOrientationUserDefinedElement = beatMetadata.HasTransposedPitchStemOrientationUserDefinedElement,
                                ConcertPitchStemOrientation = beatMetadata.ConcertPitchStemOrientation,
                                Wah = beatMetadata.Wah,
                                Golpe = beat.Golpe,
                                Fadding = beatMetadata.Fadding,
                                Slashed = beat.Slashed,
                                Hairpin = beat.Hairpin,
                                Variation = beatMetadata.Variation,
                                Ottavia = beat.Ottavia,
                                LegatoOrigin = beat.LegatoOrigin,
                                LegatoDestination = beat.LegatoDestination,
                                LyricsXml = beatMetadata.LyricsXml,
                                PickStrokeDirection = beat.PickStrokeDirection,
                                VibratoWithTremBarStrength = beatMetadata.VibratoWithTremBarStrength,
                                Slapped = beat.Slapped,
                                Popped = beat.Popped,
                                Brush = beat.Brush,
                                BrushIsUp = beat.BrushIsUp,
                                Arpeggio = beat.Arpeggio,
                                BrushDurationTicks = beat.BrushDurationTicks,
                                BrushDurationXPropertyId = beatMetadata.BrushDurationXPropertyId,
                                HasExplicitBrushDurationXProperty = beatMetadata.HasExplicitBrushDurationXProperty,
                                Rasgueado = beat.Rasgueado,
                                RasgueadoPattern = beat.RasgueadoPattern,
                                DeadSlapped = beat.DeadSlapped,
                                Tremolo = beat.Tremolo,
                                TremoloValue = beat.TremoloValue,
                                ChordId = beatMetadata.ChordId,
                                FreeText = beat.FreeText,
                                WhammyBar = encodedWhammy.Enabled,
                                WhammyBarExtended = encodedWhammy.Extended,
                                WhammyBarOriginValue = encodedWhammy.OriginValue,
                                WhammyBarMiddleValue = encodedWhammy.MiddleValue,
                                WhammyBarDestinationValue = encodedWhammy.DestinationValue,
                                WhammyBarOriginOffset = encodedWhammy.OriginOffset,
                                WhammyBarMiddleOffset1 = encodedWhammy.MiddleOffset1,
                                WhammyBarMiddleOffset2 = encodedWhammy.MiddleOffset2,
                                WhammyBarDestinationOffset = encodedWhammy.DestinationOffset,
                                WhammyUsesElement = beatMetadata.WhammyUsesElement,
                                WhammyExtendUsesElement = beatMetadata.WhammyExtendUsesElement,
                                Properties = beatMetadata.Properties.ToDictionary(kv => kv.Key, kv => kv.Value),
                                XProperties = beatXProperties,
                                XPropertiesXml = beatMetadata.XPropertiesXml
                            };

                            var currentBeatId = AllocateId(
                                beat.Id,
                                beatCandidate,
                                beats,
                                ref beatId,
                                BeatsEqual,
                                remappedBeatIdsBySourceId,
                                diagnostics,
                                code: "BEAT_ID_CONFLICT",
                                category: "ReferenceReuse",
                                message: $"Beat id {beat.Id} appeared with different content; a new raw beat id was allocated.");

                            if (!beats.ContainsKey(currentBeatId))
                            {
                                beats[currentBeatId] = WithBeatId(beatCandidate, currentBeatId);
                            }

                            beatIds.Add(currentBeatId);
                        }

                        var voiceMetadata = GetVoiceMetadata(measureVoice);
                        var voiceCandidate = new GpifVoice
                        {
                            Xml = voiceMetadata.Xml,
                            Id = 0,
                            BeatsReferenceList = ReferenceListFormatter.JoinRefs(beatIds),
                            Properties = voiceMetadata.Properties,
                            DirectionTags = voiceMetadata.DirectionTags.ToArray()
                        };

                        var currentVoiceId = AllocateId(
                            voiceMetadata.SourceVoiceId,
                            voiceCandidate,
                            voices,
                            ref voiceId,
                            VoicesEqual,
                            remappedVoiceIdsBySourceId,
                            diagnostics,
                            code: "VOICE_ID_CONFLICT",
                            category: "ReferenceReuse",
                            message: $"Voice id {voiceMetadata.SourceVoiceId} appeared with different content; a new raw voice id was allocated.");

                        if (!voices.ContainsKey(currentVoiceId))
                        {
                            voices[currentVoiceId] = WithVoiceId(voiceCandidate, currentVoiceId);
                        }

                        SetVoiceSlot(voiceSlots, measureVoice.VoiceIndex, currentVoiceId);
                    }

                    var barCandidate = new GpifBar
                    {
                        Xml = staffMetadata.BarXml,
                        Id = 0,
                        VoicesReferenceList = ReferenceListFormatter.JoinRefs(voiceSlots),
                        Clef = staffBar.Clef,
                        SimileMark = staffBar.SimileMark,
                        Properties = staffMetadata.Properties,
                        XProperties = staffMetadata.XProperties,
                        XPropertiesXml = staffMetadata.BarXPropertiesXml
                    };

                    currentBarId = AllocateId(
                        staffMetadata.SourceBarId,
                        barCandidate,
                        bars,
                        ref barId,
                        BarsEqual,
                        remappedBarIdsBySourceId,
                        diagnostics,
                        code: "BAR_ID_CONFLICT",
                        category: "ReferenceReuse",
                        message: $"Bar id {staffMetadata.SourceBarId} appeared with different content; a new raw bar id was allocated.");

                    if (!bars.ContainsKey(currentBarId))
                    {
                        bars[currentBarId] = WithBarId(barCandidate, currentBarId);
                    }

                    measureBarIds.Add(currentBarId);
                }

            }

            if (masterBars.Count > m)
            {
                var existing = masterBars[m];
                masterBars[m] = new GpifMasterBar
                {
                    Xml = existing.Xml,
                    Index = existing.Index,
                    Time = existing.Time,
                    DoubleBar = existing.DoubleBar,
                    FreeTime = existing.FreeTime,
                    TripletFeel = existing.TripletFeel,
                    BarsReferenceList = ReferenceListFormatter.JoinRefs(measureBarIds),
                    AlternateEndings = existing.AlternateEndings,
                    RepeatStart = existing.RepeatStart,
                    RepeatStartAttributePresent = existing.RepeatStartAttributePresent,
                    RepeatEnd = existing.RepeatEnd,
                    RepeatEndAttributePresent = existing.RepeatEndAttributePresent,
                    RepeatCount = existing.RepeatCount,
                    RepeatCountAttributePresent = existing.RepeatCountAttributePresent,
                    SectionLetter = existing.SectionLetter,
                    SectionText = existing.SectionText,
                    HasExplicitEmptySection = existing.HasExplicitEmptySection,
                    Jump = existing.Jump,
                    Target = existing.Target,
                    DirectionProperties = existing.DirectionProperties,
                    DirectionsXml = existing.DirectionsXml,
                    KeyAccidentalCount = existing.KeyAccidentalCount,
                    KeyMode = existing.KeyMode,
                    KeyTransposeAs = existing.KeyTransposeAs,
                    Fermatas = existing.Fermatas,
                    XProperties = existing.XProperties,
                    XPropertiesXml = existing.XPropertiesXml
                };
            }
        }

        var doc = new GpifDocument
        {
            GpVersion = scoreMetadata.GpVersion,
            GpRevision = new GpifRevisionInfo
            {
                Xml = scoreMetadata.GpRevisionXml,
                Required = scoreMetadata.GpRevisionRequired,
                Recommended = scoreMetadata.GpRevisionRecommended,
                Value = scoreMetadata.GpRevisionValue
            },
            EncodingDescription = scoreMetadata.EncodingDescription,
            Score = new ScoreInfo
            {
                Xml = scoreMetadata.ScoreXml,
                ExplicitEmptyOptionalElements = scoreMetadata.ExplicitEmptyOptionalElements,
                Title = score.Title,
                SubTitle = scoreMetadata.SubTitle,
                Artist = score.Artist,
                Album = score.Album,
                Words = scoreMetadata.Words,
                Music = scoreMetadata.Music,
                WordsAndMusic = scoreMetadata.WordsAndMusic,
                Copyright = scoreMetadata.Copyright,
                Tabber = scoreMetadata.Tabber,
                Instructions = scoreMetadata.Instructions,
                Notices = scoreMetadata.Notices,
                FirstPageHeader = scoreMetadata.FirstPageHeader,
                FirstPageFooter = scoreMetadata.FirstPageFooter,
                PageHeader = scoreMetadata.PageHeader,
                PageFooter = scoreMetadata.PageFooter,
                ScoreSystemsDefaultLayout = scoreMetadata.ScoreSystemsDefaultLayout,
                ScoreSystemsLayout = scoreMetadata.ScoreSystemsLayout,
                ScoreZoomPolicy = scoreMetadata.ScoreZoomPolicy,
                ScoreZoom = scoreMetadata.ScoreZoom,
                PageSetupXml = scoreMetadata.PageSetupXml,
                MultiVoice = scoreMetadata.MultiVoice
            },
            MasterTrack = new GpifMasterTrack
            {
                Xml = masterTrackMetadata.Xml,
                TrackIds = masterTrackIds,
                AutomationsXml = masterTrackMetadata.AutomationsXml,
                Automations = masterTrackMetadata.Automations.Select(a => new GpifAutomation
                {
                    Type = a.Type,
                    Linear = a.Linear,
                    Bar = a.Bar,
                    Position = a.Position,
                    Visible = a.Visible,
                    Value = a.Value
                }).ToArray(),
                Anacrusis = masterTrackMetadata.Anacrusis,
                RseXml = masterTrackMetadata.RseXml,
                Rse = new GpifMasterRse
                {
                    MasterEffects = masterTrackMetadata.Rse.MasterEffects.Select(effect => new GpifRseEffect
                    {
                        Id = effect.Id,
                        Bypass = effect.Bypass,
                        Parameters = effect.Parameters
                    }).ToArray()
                }
            },
            Tracks = tracks,
            MasterBars = masterBars,
            BarsById = bars,
            VoicesById = voices,
            BeatsById = beats,
            NotesById = notes,
            RhythmsById = rhythms,
            BackingTrackXml = scoreMetadata.BackingTrackXml,
            AudioTracksXml = scoreMetadata.AudioTracksXml,
            AssetsXml = scoreMetadata.AssetsXml,
            ScoreViewsXml = scoreMetadata.ScoreViewsXml
        };

        regenerationDiagnostics?.AppendTo(diagnostics);

        return ValueTask.FromResult(new WriteResult
        {
            RawDocument = doc,
            Diagnostics = diagnostics
        });
    }

    private static GpifMasterBar CreateMasterBar(TimelineBar timelineBar)
    {
        var timelineMetadata = GetTimelineBarMetadata(timelineBar);
        var jump = ResolveDirectionValue(timelineBar.Jump, timelineBar.DirectionProperties, "Jump");
        var target = ResolveDirectionValue(timelineBar.Target, timelineBar.DirectionProperties, "Target");

        return new GpifMasterBar
        {
            Xml = timelineMetadata.MasterBarXml,
            Index = timelineBar.Index,
            Time = timelineBar.TimeSignature,
            DoubleBar = timelineBar.DoubleBar,
            FreeTime = timelineBar.FreeTime,
            TripletFeel = timelineBar.TripletFeel,
            RepeatStart = timelineBar.RepeatStart,
            RepeatStartAttributePresent = timelineBar.RepeatStartAttributePresent,
            RepeatEnd = timelineBar.RepeatEnd,
            RepeatEndAttributePresent = timelineBar.RepeatEndAttributePresent,
            RepeatCount = timelineBar.RepeatCount,
            RepeatCountAttributePresent = timelineBar.RepeatCountAttributePresent,
            AlternateEndings = timelineBar.AlternateEndings,
            SectionLetter = timelineBar.SectionLetter,
            SectionText = timelineBar.SectionText,
            HasExplicitEmptySection = timelineBar.HasExplicitEmptySection,
            Jump = jump,
            Target = target,
            DirectionProperties = timelineBar.DirectionProperties,
            DirectionsXml = timelineMetadata.DirectionsXml,
            KeyAccidentalCount = timelineBar.KeyAccidentalCount,
            KeyMode = timelineBar.KeyMode,
            KeyTransposeAs = timelineBar.KeyTransposeAs,
            Fermatas = timelineBar.Fermatas.Select(f => new GpifFermata
            {
                Type = f.Type,
                Offset = f.Offset,
                Length = f.Length
            }).ToArray(),
            XProperties = timelineMetadata.XProperties,
            XPropertiesXml = timelineMetadata.MasterBarXPropertiesXml
        };
    }

    private static TimelineBar CreateDefaultTimelineBar(int index)
        => new()
        {
            Index = index,
            TimeSignature = "4/4"
        };

    private static void AppendGuitarProFidelityDiagnostics(Score score, WriteDiagnostics diagnostics)
    {
        if (!HasScoreLevelSourceContext(score))
        {
            return;
        }

        var fidelityState = score.GetGuitarProFidelityState();
        if (fidelityState?.FidelityInvalidated == true)
        {
            diagnostics.Warn(
                code: "GP_SOURCE_FIDELITY_INVALIDATED",
                category: "RawFidelity",
                message: "Guitar Pro source fidelity extensions were invalidated before write; raw GPIF metadata will be regenerated from Core data where exact source preservation is no longer available.");
        }

        var lastReattachment = fidelityState?.LastReattachment;
        if (lastReattachment?.HasUnmatchedTargets == true)
        {
            diagnostics.Warn(
                code: "GP_EXTENSION_REATTACHMENT_PARTIAL",
                category: "RawFidelity",
                message: GuitarProModelExtensions.FormatPartialReattachmentMessage(lastReattachment));
        }

        if (fidelityState?.FidelityInvalidated == true || lastReattachment?.HasUnmatchedTargets == true)
        {
            return;
        }

        var coverage = MeasureGuitarProExtensionCoverage(score);
        var partialCoverage = coverage.GetPartialKinds().ToArray();
        if (partialCoverage.Length == 0)
        {
            return;
        }

        diagnostics.Warn(
            code: "GP_EXTENSION_GRAPH_PARTIAL",
            category: "RawFidelity",
            message: $"Guitar Pro source fidelity is only attached to part of the score tree; writer will regenerate missing raw metadata for {string.Join(", ", partialCoverage)}.");
    }

    private static bool HasScoreLevelSourceContext(Score score)
    {
        var fidelityState = score.GetGuitarProFidelityState();
        return fidelityState?.HasSourceContext == true || score.GetGuitarPro() is not null;
    }

    private static ExtensionCoverage MeasureGuitarProExtensionCoverage(Score score)
    {
        var coverage = new ExtensionCoverage
        {
            TimelineBarsTotal = score.TimelineBars.Count,
            TimelineBarsAttached = score.TimelineBars.Count(timelineBar => timelineBar.GetGuitarPro() is not null)
        };

        foreach (var track in score.Tracks)
        {
            coverage.TracksTotal++;
            if (track.GetGuitarPro() is not null)
            {
                coverage.TracksAttached++;
            }

            foreach (var staffBar in EnumeratePreferredStaffMeasures(track))
            {
                coverage.StaffsTotal++;
                if (staffBar.GetGuitarPro() is not null)
                {
                    coverage.StaffsAttached++;
                }

                AppendMeasureContentCoverage(staffBar.Voices, staffBar.Beats, coverage);
            }
        }

        return coverage;
    }

    private static void AppendMeasureContentCoverage(
        IReadOnlyList<Voice> voices,
        IReadOnlyList<Beat> beats,
        ExtensionCoverage coverage)
    {
        if (voices.Count > 0)
        {
            AppendVoiceCoverage(voices, coverage);
            return;
        }

        AppendBeatCoverage(beats, coverage);
    }

    private static void AppendVoiceCoverage(IReadOnlyList<Voice> voices, ExtensionCoverage coverage)
    {
        foreach (var voice in voices)
        {
            coverage.VoicesTotal++;
            if (voice.GetGuitarPro() is not null)
            {
                coverage.VoicesAttached++;
            }

            AppendBeatCoverage(voice.Beats, coverage);
        }
    }

    private static void AppendBeatCoverage(IReadOnlyList<Beat> beats, ExtensionCoverage coverage)
    {
        foreach (var beat in beats)
        {
            coverage.BeatsTotal++;
            if (beat.GetGuitarPro() is not null)
            {
                coverage.BeatsAttached++;
            }

            foreach (var note in beat.Notes)
            {
                coverage.NotesTotal++;
                if (note.GetGuitarPro() is not null)
                {
                    coverage.NotesAttached++;
                }
            }
        }
    }

    private sealed class ExtensionCoverage
    {
        public int TimelineBarsTotal { get; set; }

        public int TimelineBarsAttached { get; set; }

        public int TracksTotal { get; set; }

        public int TracksAttached { get; set; }

        public int StaffsTotal { get; set; }

        public int StaffsAttached { get; set; }

        public int VoicesTotal { get; set; }

        public int VoicesAttached { get; set; }

        public int BeatsTotal { get; set; }

        public int BeatsAttached { get; set; }

        public int NotesTotal { get; set; }

        public int NotesAttached { get; set; }

        public IEnumerable<string> GetPartialKinds()
        {
            if (IsPartial(TimelineBarsAttached, TimelineBarsTotal))
            {
                yield return $"timeline bars ({TimelineBarsAttached}/{TimelineBarsTotal})";
            }

            if (IsPartial(TracksAttached, TracksTotal))
            {
                yield return $"tracks ({TracksAttached}/{TracksTotal})";
            }

            if (IsPartial(StaffsAttached, StaffsTotal))
            {
                yield return $"staff bars ({StaffsAttached}/{StaffsTotal})";
            }

            if (IsPartial(VoicesAttached, VoicesTotal))
            {
                yield return $"voices ({VoicesAttached}/{VoicesTotal})";
            }

            if (IsPartial(BeatsAttached, BeatsTotal))
            {
                yield return $"beats ({BeatsAttached}/{BeatsTotal})";
            }

            if (IsPartial(NotesAttached, NotesTotal))
            {
                yield return $"notes ({NotesAttached}/{NotesTotal})";
            }
        }

        private static bool IsPartial(int attached, int total)
            => total > 0 && attached < total;
    }

    private sealed class RegenerationDiagnostics
    {
        private readonly List<string> trackStavesXmlPaths = [];
        private readonly List<string> noteStringFretPaths = [];
        private readonly List<string> noteConcertPitchPaths = [];
        private readonly List<string> noteTransposedPitchPaths = [];
        private readonly List<string> rhythmSourceShapePaths = [];

        public void RecordTrackStavesXml(string path)
            => trackStavesXmlPaths.Add(path);

        public void RecordNoteStringFret(string path)
            => noteStringFretPaths.Add(path);

        public void RecordNoteConcertPitch(string path)
            => noteConcertPitchPaths.Add(path);

        public void RecordNoteTransposedPitch(string path)
            => noteTransposedPitchPaths.Add(path);

        public void RecordRhythmSourceShape(string path)
            => rhythmSourceShapePaths.Add(path);

        public void AppendTo(WriteDiagnostics diagnostics)
        {
            AppendWarning(
                diagnostics,
                trackStavesXmlPaths,
                code: "TRACK_STAVES_XML_REGENERATED",
                message: "Writer regenerated track <Staves> XML from structured staff metadata because the imported source staff XML no longer matched the current track state.");
            AppendWarning(
                diagnostics,
                noteStringFretPaths,
                code: "NOTE_STRING_FRET_REGENERATED",
                message: "Writer regenerated note string/fret properties because the imported source string placement no longer matched the current pitch or string context.");
            AppendWarning(
                diagnostics,
                noteConcertPitchPaths,
                code: "NOTE_CONCERT_PITCH_REGENERATED",
                message: "Writer regenerated note ConcertPitch payloads from current MIDI values because the imported source spelling was no longer reusable.");
            AppendWarning(
                diagnostics,
                noteTransposedPitchPaths,
                code: "NOTE_TRANSPOSED_PITCH_REGENERATED",
                message: "Writer regenerated note TransposedPitch payloads from current MIDI/transpose values because the imported source spelling was no longer reusable.");
            AppendWarning(
                diagnostics,
                rhythmSourceShapePaths,
                code: "RHYTHM_SOURCE_SHAPE_REGENERATED",
                message: "Writer regenerated rhythm shapes from beat durations because the imported source rhythm shape was no longer reusable.");
        }

        private static void AppendWarning(
            WriteDiagnostics diagnostics,
            IReadOnlyList<string> paths,
            string code,
            string message)
        {
            if (paths.Count == 0)
            {
                return;
            }

            diagnostics.Warn(
                code: code,
                category: "RawFidelity",
                message: $"{message} Affected nodes: {paths.Count}. Samples: {FormatSamplePaths(paths)}.",
                path: paths[0]);
        }
    }

    private static string FormatSamplePaths(IReadOnlyList<string> paths)
    {
        const int maxSamples = 3;
        var samples = paths.Take(maxSamples).ToArray();
        return paths.Count > maxSamples
            ? $"{string.Join(", ", samples)}, ..."
            : string.Join(", ", samples);
    }

    private static string FormatTrackPath(Track track)
        => $"/Score/Tracks[@id='{track.Id}']";

    private static string FormatVoicePath(
        Track track,
        int measureIndex,
        StaffMeasure staffBar,
        Voice measureVoice)
        => $"{FormatTrackPath(track)}/Staves[@index='{staffBar.StaffIndex}']/Measures[@index='{measureIndex}']/Voices[@index='{measureVoice.VoiceIndex}']";

    private static string FormatBeatPath(
        Track track,
        int measureIndex,
        StaffMeasure staffBar,
        Voice measureVoice,
        int beatIndex)
        => $"{FormatVoicePath(track, measureIndex, staffBar, measureVoice)}/Beats[{beatIndex}]";

    private static string FormatNotePath(
        Track track,
        int measureIndex,
        StaffMeasure staffBar,
        Voice measureVoice,
        int beatIndex,
        int noteIndex)
        => $"{FormatBeatPath(track, measureIndex, staffBar, measureVoice, beatIndex)}/Notes[{noteIndex}]";

    private static IReadOnlyList<GpifNoteProperty> BuildCoreNoteProperties(int? midiPitch, int? stringNumber, int? fret)
    {
        var properties = new List<GpifNoteProperty>(3);

        if (fret.HasValue)
        {
            properties.Add(new GpifNoteProperty
            {
                Name = "Fret",
                Fret = fret.Value
            });
        }

        if (midiPitch.HasValue)
        {
            properties.Add(new GpifNoteProperty
            {
                Name = "Midi",
                Number = midiPitch.Value
            });
        }

        if (stringNumber.HasValue)
        {
            properties.Add(new GpifNoteProperty
            {
                Name = "String",
                StringNumber = stringNumber.Value
            });
        }

        return properties;
    }

    private static (int? StringNumber, int? Fret) ResolveStringAndFret(
        Note note,
        Track track,
        int staffIndex,
        bool preserveSourceStringAndFret)
    {
        var noteMetadata = GetNoteMetadata(note);
        if (preserveSourceStringAndFret)
        {
            return (noteMetadata.SourceStringNumber ?? note.StringNumber, noteMetadata.SourceFret);
        }

        if (!note.MidiPitch.HasValue)
        {
            return (note.StringNumber, null);
        }

        var tuning = ResolveTuningPitches(track, staffIndex);
        if (tuning.Length == 0)
        {
            return (note.StringNumber, null);
        }

        var midi = note.MidiPitch.Value;

        if (note.StringNumber is >= 0 and < int.MaxValue)
        {
            var candidateString = note.StringNumber.Value;
            if (candidateString < tuning.Length)
            {
                var candidateFret = midi - tuning[candidateString];
                if (candidateFret >= 0)
                {
                    return (candidateString, candidateFret);
                }
            }
        }

        int? bestString = null;
        int? bestFret = null;
        for (var i = 0; i < tuning.Length; i++)
        {
            var candidateFret = midi - tuning[i];
            if (candidateFret < 0)
            {
                continue;
            }

            if (!bestFret.HasValue || candidateFret < bestFret.Value)
            {
                bestFret = candidateFret;
                bestString = i;
            }
        }

        return (bestString ?? note.StringNumber, bestFret);
    }

    private static bool ResolveNotePalmMuted(Note note, Beat beat)
    {
        if (note.Articulation.PalmMuted)
        {
            return true;
        }

        if (!beat.PalmMuted)
        {
            return false;
        }

        // Beat.PalmMuted is a derived aggregate on read, so only fan it back out when the caller
        // set the beat-level flag without any explicit note-level palm-muted articulations.
        return beat.Notes.Count > 0 && !beat.Notes.Any(candidate => candidate.Articulation.PalmMuted);
    }

    private static bool BrushDurationXPropertiesMatch(Beat beat, IReadOnlyDictionary<string, int> beatXProperties)
    {
        if (!beat.BrushDurationTicks.HasValue)
        {
            return !beatXProperties.ContainsKey("687931393")
                && !beatXProperties.ContainsKey("687935489");
        }

        var beatMetadata = GetBeatMetadata(beat);
        if (!string.IsNullOrWhiteSpace(beatMetadata.BrushDurationXPropertyId))
        {
            return beatXProperties.TryGetValue(beatMetadata.BrushDurationXPropertyId, out var value)
                && value == beat.BrushDurationTicks.Value;
        }

        return beatXProperties.Any(kv =>
            (kv.Key == "687931393" || kv.Key == "687935489")
            && kv.Value == beat.BrushDurationTicks.Value);
    }

    private static TrackMetadata GetTrackMetadata(Track track)
        => track.GetGuitarPro()?.Metadata ?? new TrackMetadata();

    private static GpTimelineBarMetadata GetTimelineBarMetadata(TimelineBar timelineBar)
        => timelineBar.GetGuitarPro()?.Metadata ?? new GpTimelineBarMetadata();

    private static GpMeasureStaffMetadata GetMeasureStaffMetadata(StaffMeasure staffMeasure)
        => staffMeasure.GetGuitarPro()?.Metadata ?? new GpMeasureStaffMetadata();

    private static GpVoiceMetadata GetVoiceMetadata(Voice voice)
        => voice.GetGuitarPro()?.Metadata ?? new GpVoiceMetadata();

    private static GpBeatMetadata GetBeatMetadata(Beat beat)
        => beat.GetGuitarPro()?.Metadata ?? new GpBeatMetadata();

    private static GpNoteMetadata GetNoteMetadata(Note note)
        => note.GetGuitarPro()?.Metadata ?? new GpNoteMetadata();

    private static IReadOnlyList<StaffMetadata> ResolveTrackStaffMetadata(Track track)
    {
        var trackMetadata = GetTrackMetadata(track);
        if (track.Staves.Count == 0)
        {
            return trackMetadata.Staffs;
        }

        var current = new List<StaffMetadata>(track.Staves.Count);
        foreach (var staff in track.Staves.OrderBy(s => s.StaffIndex))
        {
            var staffExtension = staff.GetGuitarPro();
            if (staffExtension is not null)
            {
                current.Add(CloneStaffMetadata(staffExtension.Metadata));
                continue;
            }

            if (staff.StaffIndex >= 0 && staff.StaffIndex < trackMetadata.Staffs.Count)
            {
                current.Add(CloneStaffMetadata(trackMetadata.Staffs[staff.StaffIndex]));
            }
            else
            {
                current.Add(new StaffMetadata());
            }
        }

        return current;
    }

    private static StaffMetadata CloneStaffMetadata(StaffMetadata source)
        => new()
        {
            Id = source.Id,
            Cref = source.Cref,
            TuningPitches = source.TuningPitches.ToArray(),
            CapoFret = source.CapoFret,
            Properties = source.Properties.ToDictionary(kv => kv.Key, kv => kv.Value),
            Xml = source.Xml
        };

    private static int[] ResolveTuningPitches(Track track, int staffIndex)
    {
        var metadata = GetTrackMetadata(track);
        var currentStaffMetadata = ResolveTrackStaffMetadata(track);

        if (staffIndex >= 0 && staffIndex < currentStaffMetadata.Count)
        {
            var staffTuning = currentStaffMetadata[staffIndex].TuningPitches;
            if (staffTuning.Length > 0)
            {
                return staffTuning;
            }
        }

        return metadata.TuningPitches;
    }

    private static bool ShouldPreserveSourceStringAndFret(Note note, Track track, int staffIndex)
    {
        var noteMetadata = GetNoteMetadata(note);
        if (!HasSourceStringAndFret(noteMetadata))
        {
            return false;
        }

        if (note.MidiPitch != noteMetadata.SourceMidiPitch
            || note.StringNumber != noteMetadata.SourceStringNumber
            || ResolveTransposedMidiPitch(note, track) != noteMetadata.SourceTransposedMidiPitch)
        {
            return false;
        }

        return SourceStringContextMatches(track, staffIndex);
    }

    private static bool HasSourceStringAndFret(GpNoteMetadata noteMetadata)
        => noteMetadata.SourceFret.HasValue || noteMetadata.SourceStringNumber.HasValue;

    private static bool SourceStringContextMatches(Track track, int staffIndex)
    {
        var source = ResolveSourceStringContext(track, staffIndex);
        if (source is null)
        {
            return true;
        }

        return ResolveTuningPitches(track, staffIndex).SequenceEqual(source.TuningPitches)
               && ResolveCapoFret(track, staffIndex) == source.CapoFret;
    }

    private static SourceStringContext? ResolveSourceStringContext(Track track, int staffIndex)
    {
        var metadata = GetTrackMetadata(track);
        var currentStaffMetadata = ResolveTrackStaffMetadata(track);
        if (staffIndex >= 0 && staffIndex < currentStaffMetadata.Count)
        {
            var staff = currentStaffMetadata[staffIndex];
            var staffTuning = ParseTuningPitches(staff.Properties);
            var staffCapo = ParseCapoFret(staff.Properties);
            if (staffTuning.Length > 0 || staffCapo.HasValue)
            {
                return new SourceStringContext(staffTuning, staffCapo);
            }
        }

        var trackTuning = ParseTuningPitches(metadata.Properties);
        var trackCapo = ParseCapoFret(metadata.Properties);
        if (trackTuning.Length > 0 || trackCapo.HasValue)
        {
            return new SourceStringContext(trackTuning, trackCapo);
        }

        return null;
    }

    private static int[] ParseTuningPitches(IReadOnlyDictionary<string, string> properties)
        => properties.TryGetValue("Tuning", out var tuningRaw)
            ? SplitInts(tuningRaw)
            : Array.Empty<int>();

    private static int? ParseCapoFret(IReadOnlyDictionary<string, string> properties)
        => properties.TryGetValue("CapoFret", out var capoRaw)
            ? TryParseNullableInt(capoRaw)
            : null;

    private static int? ResolveCapoFret(Track track, int staffIndex)
    {
        var metadata = GetTrackMetadata(track);
        var currentStaffMetadata = ResolveTrackStaffMetadata(track);

        if (staffIndex >= 0 && staffIndex < currentStaffMetadata.Count)
        {
            var staffCapo = currentStaffMetadata[staffIndex].CapoFret;
            if (staffCapo.HasValue)
            {
                return staffCapo;
            }
        }

        return ParseCapoFret(metadata.Properties);
    }

    private static string ResolveStavesXml(
        TrackMetadata metadata,
        IReadOnlyList<StaffMetadata> currentStaffMetadata)
        => ShouldPreserveSourceStavesXml(metadata, currentStaffMetadata)
            ? metadata.StavesXml
            : string.Empty;

    private static bool HasSourceStavesXml(TrackMetadata metadata)
        => !string.IsNullOrWhiteSpace(metadata.StavesXml);

    private static bool ShouldEmitStructuredStaffs(
        TrackMetadata metadata,
        IReadOnlyList<StaffMetadata> currentStaffMetadata)
    {
        if (currentStaffMetadata.Count == 0)
        {
            return false;
        }

        if (HasSourceStavesXml(metadata))
        {
            return true;
        }

        return currentStaffMetadata.Count != 1 || !IsImplicitDefaultStaff(currentStaffMetadata[0]);
    }

    private static bool ShouldPreserveSourceStavesXml(
        TrackMetadata metadata,
        IReadOnlyList<StaffMetadata> currentStaffMetadata)
    {
        if (string.IsNullOrWhiteSpace(metadata.StavesXml))
        {
            return false;
        }

        try
        {
            var sourceRoot = XElement.Parse(metadata.StavesXml);
            var sourceStaffs = sourceRoot.Elements("Staff")
                .Select(ParseSourceStaff)
                .ToArray();

            if (sourceStaffs.Length != currentStaffMetadata.Count)
            {
                return false;
            }

            return sourceStaffs.Zip(currentStaffMetadata)
                .All(pair => StaffMatchesSource(pair.Second, pair.First));
        }
        catch
        {
            return false;
        }
    }

    private static int? ResolveTransposedMidiPitch(Note note, Track track)
    {
        if (!note.MidiPitch.HasValue)
        {
            return null;
        }

        var metadata = GetTrackMetadata(track);
        var chromatic = metadata.Transpose.Chromatic ?? 0;
        var octave = metadata.Transpose.Octave ?? 0;
        return note.MidiPitch.Value - (octave * 12) + chromatic;
    }

    private static int? ResolveTrillSpeedXPropertyValue(Note note)
    {
        var encodedTrillSpeed = ArticulationDecoders.EncodeTrillSpeed(note.Articulation.TrillSpeed);
        if (!encodedTrillSpeed.HasValue)
        {
            return null;
        }

        var noteMetadata = GetNoteMetadata(note);
        if (noteMetadata.XProperties.TryGetValue("688062467", out var sourceValue)
            && ArticulationDecoders.DecodeTrillSpeed(
                new Dictionary<string, int>
                {
                    ["688062467"] = sourceValue
                }) == note.Articulation.TrillSpeed)
        {
            return sourceValue;
        }

        return encodedTrillSpeed.Value;
    }

    private static int? ResolveSlideFlags(Note note)
    {
        var encodedSlideFlags = ArticulationDecoders.EncodeSlides(note.Articulation.Slides);
        if (!encodedSlideFlags.HasValue)
        {
            return null;
        }

        var noteMetadata = GetNoteMetadata(note);
        if (noteMetadata.SourceSlideFlags.HasValue
            && ArticulationDecoders.DecodeSlides(noteMetadata.SourceSlideFlags.Value)
                .SequenceEqual(note.Articulation.Slides))
        {
            return noteMetadata.SourceSlideFlags.Value;
        }

        return encodedSlideFlags.Value;
    }

    private static bool ShouldPreserveSourceConcertPitch(Note note)
    {
        var noteMetadata = GetNoteMetadata(note);
        return note.ConcertPitch is not null
               && note.MidiPitch == noteMetadata.SourceMidiPitch;
    }

    private static bool ShouldPreserveSourceTransposedPitch(Note note, int? transposedMidiPitch)
    {
        var noteMetadata = GetNoteMetadata(note);
        return note.TransposedPitch is not null
               && transposedMidiPitch == noteMetadata.SourceTransposedMidiPitch;
    }

    private static GpifPitchValue? ToRawPitchValue(PitchValue? pitch)
        => pitch is null
            ? null
            : new GpifPitchValue
            {
                Step = pitch.Step,
                Accidental = pitch.Accidental,
                Octave = pitch.Octave
            };

    private static bool StaffMatchesSource(StaffMetadata staff, SourceStaffShape source)
        => staff.Id == source.Id
           && string.Equals(staff.Cref, source.Cref, StringComparison.Ordinal)
           && staff.CapoFret == source.CapoFret
           && staff.TuningPitches.SequenceEqual(source.TuningPitches)
           && DictionariesEqual(staff.Properties, source.Properties);

    private static bool IsImplicitDefaultStaff(StaffMetadata staff)
        => !staff.Id.HasValue
           && string.IsNullOrWhiteSpace(staff.Cref)
           && staff.TuningPitches.Length == 0
           && !staff.CapoFret.HasValue
           && staff.Properties.Count == 0
           && string.IsNullOrWhiteSpace(staff.Xml);

    private static SourceStaffShape ParseSourceStaff(XElement staff)
    {
        var properties = ParseSourcePropertyDictionary(staff.Element("Properties"));
        properties.TryGetValue("Tuning", out var tuningRaw);
        properties.TryGetValue("CapoFret", out var capoRaw);

        return new SourceStaffShape(
            Id: TryParseNullableInt(staff.Attribute("id")?.Value),
            Cref: staff.Attribute("cref")?.Value ?? string.Empty,
            TuningPitches: SplitInts(tuningRaw),
            CapoFret: TryParseNullableInt(capoRaw),
            Properties: properties);
    }

    private static Dictionary<string, string> ParseSourcePropertyDictionary(XElement? propertiesElement)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (propertiesElement is null)
        {
            return values;
        }

        foreach (var property in propertiesElement.Elements("Property"))
        {
            var name = property.Attribute("name")?.Value;
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            values[name] = ParseSourcePropertyValue(property);
        }

        return values;
    }

    private static string ParseSourcePropertyValue(XElement property)
    {
        var preferred = property.Element("Value")?.Value
            ?? property.Element("Pitches")?.Value
            ?? property.Element("Number")?.Value
            ?? property.Element("Fret")?.Value
            ?? property.Element("Direction")?.Value
            ?? property.Element("Strength")?.Value
            ?? property.Element("Float")?.Value
            ?? property.Element("Bitset")?.Value
            ?? property.Element("String")?.Value;

        if (!string.IsNullOrWhiteSpace(preferred))
        {
            return preferred.Trim();
        }

        if (property.Element("Enable") is not null)
        {
            return "true";
        }

        return property.Value?.Trim() ?? string.Empty;
    }

    private static int[] SplitInts(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? Array.Empty<int>()
            : value.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(v => int.TryParse(v, out var i) ? i : int.MinValue)
                .Where(i => i != int.MinValue)
                .ToArray();

    private static int? TryParseNullableInt(string? value)
        => int.TryParse(value, out var parsed) ? parsed : null;

    private static string ResolveDirectionValue(
        string explicitValue,
        IReadOnlyDictionary<string, string> directionProperties,
        string directionKey)
    {
        if (!string.IsNullOrWhiteSpace(explicitValue))
        {
            return explicitValue;
        }

        return directionProperties.TryGetValue(directionKey, out var value)
            ? value
            : string.Empty;
    }

    private static int GetTrackMeasureCount(Track track)
        => track.Staves
            .Select(staff => staff.Measures.Count)
            .DefaultIfEmpty(0)
            .Max();

    private static int? GetScoreBarIndexAtPosition(IReadOnlyList<Track> tracks, int measurePosition)
    {
        foreach (var track in tracks)
        {
            var trackIndex = GetTrackBarIndexAtPosition(track, measurePosition);
            if (trackIndex.HasValue)
            {
                return trackIndex;
            }
        }

        return null;
    }

    private static int? GetTrackBarIndexAtPosition(Track track, int measurePosition)
    {
        foreach (var staff in track.Staves.OrderBy(staff => staff.StaffIndex))
        {
            if (measurePosition >= 0 && measurePosition < staff.Measures.Count)
            {
                return staff.Measures[measurePosition].Index;
            }
        }

        return null;
    }

    private static IReadOnlyList<StaffMeasure> ResolveTrackStaffMeasures(
        Track track,
        int timelineIndex,
        int measurePosition)
    {
        var totalStaffCount = GetTrackStaffCount(track);
        if (track.Staves.Count == 0)
        {
            return CreateEmptyStaffMeasures(totalStaffCount, timelineIndex);
        }

        var trackStaffsByIndex = track.Staves.ToDictionary(staff => staff.StaffIndex);
        var resolved = new List<StaffMeasure>(totalStaffCount);
        for (var staffIndex = 0; staffIndex < totalStaffCount; staffIndex++)
        {
            if (trackStaffsByIndex.TryGetValue(staffIndex, out var staff)
                && TryResolveStaffMeasure(staff, timelineIndex, measurePosition, out var staffMeasure))
            {
                resolved.Add(staffMeasure);
                continue;
            }

            resolved.Add(CreateEmptyStaffMeasure(timelineIndex, staffIndex));
        }

        return resolved;
    }

    private static int GetTrackStaffCount(Track track)
    {
        var currentStaffMetadata = ResolveTrackStaffMetadata(track);
        var highestStaffIndex = track.Staves
            .Select(staff => staff.StaffIndex)
            .DefaultIfEmpty(-1)
            .Max();

        return Math.Max(1, Math.Max(currentStaffMetadata.Count, highestStaffIndex + 1));
    }

    private static bool TryResolveStaffMeasure(
        Staff staff,
        int timelineIndex,
        int measurePosition,
        out StaffMeasure staffMeasure)
    {
        staffMeasure = staff.Measures.FirstOrDefault(measure => measure.Index == timelineIndex)
            ?? (measurePosition >= 0 && measurePosition < staff.Measures.Count ? staff.Measures[measurePosition] : null!);

        return staffMeasure is not null;
    }

    private static IReadOnlyList<StaffMeasure> CreateEmptyStaffMeasures(int count, int timelineIndex)
    {
        var staffBars = new List<StaffMeasure>(Math.Max(1, count));
        for (var staffIndex = 0; staffIndex < Math.Max(1, count); staffIndex++)
        {
            staffBars.Add(CreateEmptyStaffMeasure(timelineIndex, staffIndex));
        }

        return staffBars;
    }

    private static StaffMeasure CreateEmptyStaffMeasure(int timelineIndex, int staffIndex)
    {
        var staffBar = new StaffMeasure
        {
            Index = timelineIndex,
            StaffIndex = staffIndex
        };
        staffBar.SetExtension(new GpMeasureStaffExtension
        {
            Metadata = new GpMeasureStaffMetadata
            {
                SourceBarId = -1
            }
        });

        return staffBar;
    }

    private static IEnumerable<StaffMeasure> EnumeratePreferredStaffMeasures(Track track)
    {
        for (var measurePosition = 0; measurePosition < GetTrackMeasureCount(track); measurePosition++)
        {
            var timelineIndex = GetTrackBarIndexAtPosition(track, measurePosition) ?? measurePosition;
            foreach (var staffBar in ResolveTrackStaffMeasures(track, timelineIndex, measurePosition))
            {
                yield return staffBar;
            }
        }
    }

    private static IEnumerable<Voice> EnumeratePreferredVoices(Track track)
        => EnumeratePreferredStaffMeasures(track)
            .SelectMany(staffBar => ResolveMeasureVoices(staffBar.Voices, staffBar.Beats));

    private static IEnumerable<Beat> EnumeratePreferredBeats(Track track)
        => EnumeratePreferredStaffMeasures(track)
            .SelectMany(staffBar => ResolveMeasureVoices(staffBar.Voices, staffBar.Beats))
            .SelectMany(voice => voice.Beats);

    private static IReadOnlyList<Voice> ResolveMeasureVoices(IReadOnlyList<Voice> measureVoices, IReadOnlyList<Beat> measureBeats)
    {
        if (measureVoices.Count > 0)
        {
            return measureVoices
                .OrderBy(v => v.VoiceIndex)
                .ToArray();
        }

        if (measureBeats.Count == 0)
        {
            return Array.Empty<Voice>();
        }

        var voice = new Voice
        {
            VoiceIndex = 0,
            Beats = measureBeats
        };
        voice.SetExtension(new GpVoiceExtension
        {
            Metadata = new GpVoiceMetadata
            {
                SourceVoiceId = 0,
                Properties = new Dictionary<string, string>(),
                DirectionTags = Array.Empty<string>()
            }
        });
        return [voice];
    }

    private static List<int> CreateVoiceSlots(IReadOnlyList<Voice> measureVoices)
    {
        var highestSlotIndex = measureVoices.Count == 0
            ? -1
            : measureVoices.Max(voice => Math.Max(voice.VoiceIndex, 0));
        var slotCount = Math.Max(4, highestSlotIndex + 1);
        return Enumerable.Repeat(-1, slotCount).ToList();
    }

    private static void SetVoiceSlot(List<int> voiceSlots, int voiceIndex, int voiceId)
    {
        var slotIndex = Math.Max(voiceIndex, 0);
        while (voiceSlots.Count <= slotIndex)
        {
            voiceSlots.Add(-1);
        }

        voiceSlots[slotIndex] = voiceId;
    }

    private static int NextIdAfter(IEnumerable<int> ids)
        => ids.Where(id => id >= 0).DefaultIfEmpty(-1).Max() + 1;

    private static int AllocateId<T>(
        int preferredId,
        T candidate,
        Dictionary<int, T> existingItems,
        ref int nextId,
        Func<T, T, bool> structurallyEqual,
        Dictionary<int, List<int>> remappedIdsBySourceId,
        WriteDiagnostics diagnostics,
        string code,
        string category,
        string message)
    {
        if (preferredId >= 0)
        {
            if (existingItems.TryGetValue(preferredId, out var existing))
            {
                if (structurallyEqual(existing, candidate))
                {
                    return preferredId;
                }

                if (TryReuseRemappedId(preferredId, candidate, existingItems, remappedIdsBySourceId, structurallyEqual, out var remappedId))
                {
                    return remappedId;
                }

                diagnostics.Warn(code, category, message);
                var allocatedId = NextAvailableId(existingItems, ref nextId);
                RegisterRemappedId(preferredId, allocatedId, remappedIdsBySourceId);
                return allocatedId;
            }
            else
            {
                nextId = Math.Max(nextId, preferredId + 1);
                return preferredId;
            }
        }

        return NextAvailableId(existingItems, ref nextId);
    }

    private static bool TryReuseRemappedId<T>(
        int preferredId,
        T candidate,
        IReadOnlyDictionary<int, T> existingItems,
        IReadOnlyDictionary<int, List<int>> remappedIdsBySourceId,
        Func<T, T, bool> structurallyEqual,
        out int remappedId)
    {
        if (remappedIdsBySourceId.TryGetValue(preferredId, out var remappedIds))
        {
            foreach (var candidateId in remappedIds)
            {
                if (existingItems.TryGetValue(candidateId, out var existing) && structurallyEqual(existing, candidate))
                {
                    remappedId = candidateId;
                    return true;
                }
            }
        }

        remappedId = -1;
        return false;
    }

    private static void RegisterRemappedId(
        int preferredId,
        int allocatedId,
        IDictionary<int, List<int>> remappedIdsBySourceId)
    {
        if (!remappedIdsBySourceId.TryGetValue(preferredId, out var remappedIds))
        {
            remappedIds = [];
            remappedIdsBySourceId[preferredId] = remappedIds;
        }

        remappedIds.Add(allocatedId);
    }

    private static int NextAvailableId<T>(Dictionary<int, T> existingItems, ref int nextId)
    {
        while (existingItems.ContainsKey(nextId))
        {
            nextId++;
        }

        return nextId++;
    }

    private static GpifBar WithBarId(GpifBar bar, int id)
        => new()
        {
            Xml = bar.Xml,
            Id = id,
            VoicesReferenceList = bar.VoicesReferenceList,
            Clef = bar.Clef,
            SimileMark = bar.SimileMark,
            XProperties = bar.XProperties,
            XPropertiesXml = bar.XPropertiesXml,
            Properties = bar.Properties
        };

    private static GpifVoice WithVoiceId(GpifVoice voice, int id)
        => new()
        {
            Xml = voice.Xml,
            Id = id,
            BeatsReferenceList = voice.BeatsReferenceList,
            Properties = voice.Properties,
            DirectionTags = voice.DirectionTags
        };

    private static GpifBeat WithBeatId(GpifBeat beat, int id)
        => new()
        {
            Xml = beat.Xml,
            Id = id,
            RhythmRef = beat.RhythmRef,
            NotesReferenceList = beat.NotesReferenceList,
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
            WhammyBar = beat.WhammyBar,
            WhammyBarExtended = beat.WhammyBarExtended,
            WhammyBarOriginValue = beat.WhammyBarOriginValue,
            WhammyBarMiddleValue = beat.WhammyBarMiddleValue,
            WhammyBarDestinationValue = beat.WhammyBarDestinationValue,
            WhammyBarOriginOffset = beat.WhammyBarOriginOffset,
            WhammyBarMiddleOffset1 = beat.WhammyBarMiddleOffset1,
            WhammyBarMiddleOffset2 = beat.WhammyBarMiddleOffset2,
            WhammyBarDestinationOffset = beat.WhammyBarDestinationOffset,
            WhammyUsesElement = beat.WhammyUsesElement,
            WhammyExtendUsesElement = beat.WhammyExtendUsesElement,
            Properties = beat.Properties,
            XProperties = beat.XProperties,
            XPropertiesXml = beat.XPropertiesXml
        };

    private static GpifNote WithNoteId(GpifNote note, int id)
        => new()
        {
            Xml = note.Xml,
            Id = id,
            Velocity = note.Velocity,
            MidiPitch = note.MidiPitch,
            TransposedMidiPitch = note.TransposedMidiPitch,
            ConcertPitch = note.ConcertPitch,
            TransposedPitch = note.TransposedPitch,
            SourceFret = note.SourceFret,
            SourceStringNumber = note.SourceStringNumber,
            ShowStringNumber = note.ShowStringNumber,
            Properties = note.Properties,
            Articulation = note.Articulation,
            XProperties = note.XProperties,
            XPropertiesXml = note.XPropertiesXml
        };

    private static GpifRhythm WithRhythmId(GpifRhythm rhythm, int id)
        => new()
        {
            Xml = rhythm.Xml,
            Id = id,
            NoteValue = rhythm.NoteValue,
            AugmentationDots = rhythm.AugmentationDots,
            AugmentationDotUsesCountAttribute = rhythm.AugmentationDotUsesCountAttribute,
            AugmentationDotCounts = rhythm.AugmentationDotCounts,
            PrimaryTuplet = rhythm.PrimaryTuplet,
            SecondaryTuplet = rhythm.SecondaryTuplet
        };

    private static bool BarsEqual(GpifBar a, GpifBar b)
        => string.Equals(a.VoicesReferenceList, b.VoicesReferenceList, StringComparison.Ordinal)
           && string.Equals(a.Clef, b.Clef, StringComparison.Ordinal)
           && string.Equals(a.SimileMark, b.SimileMark, StringComparison.Ordinal)
           && DictionariesEqual(a.XProperties, b.XProperties)
           && string.Equals(a.XPropertiesXml, b.XPropertiesXml, StringComparison.Ordinal)
           && DictionariesEqual(a.Properties, b.Properties);

    private static bool VoicesEqual(GpifVoice a, GpifVoice b)
        => string.Equals(a.BeatsReferenceList, b.BeatsReferenceList, StringComparison.Ordinal)
           && DictionariesEqual(a.Properties, b.Properties)
           && a.DirectionTags.SequenceEqual(b.DirectionTags, StringComparer.Ordinal);

    private static bool BeatsEqual(GpifBeat a, GpifBeat b)
        => a.RhythmRef == b.RhythmRef
           && string.Equals(a.NotesReferenceList, b.NotesReferenceList, StringComparison.Ordinal)
           && string.Equals(a.GraceType, b.GraceType, StringComparison.Ordinal)
           && string.Equals(a.Dynamic, b.Dynamic, StringComparison.Ordinal)
           && string.Equals(a.TransposedPitchStemOrientation, b.TransposedPitchStemOrientation, StringComparison.Ordinal)
           && string.Equals(a.UserTransposedPitchStemOrientation, b.UserTransposedPitchStemOrientation, StringComparison.Ordinal)
           && a.HasTransposedPitchStemOrientationUserDefinedElement == b.HasTransposedPitchStemOrientationUserDefinedElement
           && string.Equals(a.ConcertPitchStemOrientation, b.ConcertPitchStemOrientation, StringComparison.Ordinal)
           && string.Equals(a.Wah, b.Wah, StringComparison.Ordinal)
           && string.Equals(a.Golpe, b.Golpe, StringComparison.Ordinal)
           && string.Equals(a.Fadding, b.Fadding, StringComparison.Ordinal)
           && a.Slashed == b.Slashed
           && string.Equals(a.Hairpin, b.Hairpin, StringComparison.Ordinal)
           && string.Equals(a.Variation, b.Variation, StringComparison.Ordinal)
           && string.Equals(a.Ottavia, b.Ottavia, StringComparison.Ordinal)
           && a.LegatoOrigin == b.LegatoOrigin
           && a.LegatoDestination == b.LegatoDestination
           && string.Equals(a.LyricsXml, b.LyricsXml, StringComparison.Ordinal)
           && string.Equals(a.PickStrokeDirection, b.PickStrokeDirection, StringComparison.Ordinal)
           && string.Equals(a.VibratoWithTremBarStrength, b.VibratoWithTremBarStrength, StringComparison.Ordinal)
           && a.Slapped == b.Slapped
           && a.Popped == b.Popped
           && a.Brush == b.Brush
           && a.BrushIsUp == b.BrushIsUp
           && a.Arpeggio == b.Arpeggio
           && a.BrushDurationTicks == b.BrushDurationTicks
           && string.Equals(a.BrushDurationXPropertyId, b.BrushDurationXPropertyId, StringComparison.Ordinal)
           && a.HasExplicitBrushDurationXProperty == b.HasExplicitBrushDurationXProperty
           && a.Rasgueado == b.Rasgueado
           && string.Equals(a.RasgueadoPattern, b.RasgueadoPattern, StringComparison.Ordinal)
           && a.DeadSlapped == b.DeadSlapped
           && a.Tremolo == b.Tremolo
           && string.Equals(a.TremoloValue, b.TremoloValue, StringComparison.Ordinal)
           && string.Equals(a.ChordId, b.ChordId, StringComparison.Ordinal)
           && string.Equals(a.FreeText, b.FreeText, StringComparison.Ordinal)
           && a.WhammyBar == b.WhammyBar
           && a.WhammyBarExtended == b.WhammyBarExtended
           && a.WhammyBarOriginValue == b.WhammyBarOriginValue
           && a.WhammyBarMiddleValue == b.WhammyBarMiddleValue
           && a.WhammyBarDestinationValue == b.WhammyBarDestinationValue
           && a.WhammyBarOriginOffset == b.WhammyBarOriginOffset
           && a.WhammyBarMiddleOffset1 == b.WhammyBarMiddleOffset1
           && a.WhammyBarMiddleOffset2 == b.WhammyBarMiddleOffset2
           && a.WhammyBarDestinationOffset == b.WhammyBarDestinationOffset
           && a.WhammyUsesElement == b.WhammyUsesElement
           && a.WhammyExtendUsesElement == b.WhammyExtendUsesElement
           && DictionariesEqual(a.Properties, b.Properties)
           && DictionariesEqual(a.XProperties, b.XProperties)
           && string.Equals(a.XPropertiesXml, b.XPropertiesXml, StringComparison.Ordinal);

    private static bool RhythmsEqual(GpifRhythm a, GpifRhythm b)
        => string.Equals(a.NoteValue, b.NoteValue, StringComparison.Ordinal)
           && a.AugmentationDots == b.AugmentationDots
           && a.AugmentationDotUsesCountAttribute == b.AugmentationDotUsesCountAttribute
           && IntArraysEqual(a.AugmentationDotCounts, b.AugmentationDotCounts)
           && TupletsEqual(a.PrimaryTuplet, b.PrimaryTuplet)
           && TupletsEqual(a.SecondaryTuplet, b.SecondaryTuplet);

    private static bool NotesEqual(GpifNote a, GpifNote b)
        => a.Velocity == b.Velocity
           && a.MidiPitch == b.MidiPitch
           && a.TransposedMidiPitch == b.TransposedMidiPitch
           && PitchValuesEqual(a.ConcertPitch, b.ConcertPitch)
           && PitchValuesEqual(a.TransposedPitch, b.TransposedPitch)
           && a.SourceFret == b.SourceFret
           && a.SourceStringNumber == b.SourceStringNumber
           && a.ShowStringNumber == b.ShowStringNumber
           && PropertiesEqual(a.Properties, b.Properties)
           && ArticulationsEqual(a.Articulation, b.Articulation)
           && DictionariesEqual(a.XProperties, b.XProperties)
           && string.Equals(a.XPropertiesXml, b.XPropertiesXml, StringComparison.Ordinal);

    private static bool PitchValuesEqual(GpifPitchValue? a, GpifPitchValue? b)
        => ReferenceEquals(a, b)
           || (a is not null
               && b is not null
               && string.Equals(a.Step, b.Step, StringComparison.Ordinal)
               && string.Equals(a.Accidental, b.Accidental, StringComparison.Ordinal)
               && a.Octave == b.Octave);

    private static bool PropertiesEqual(IReadOnlyList<GpifNoteProperty> a, IReadOnlyList<GpifNoteProperty> b)
        => a.Count == b.Count && a.Zip(b).All(pair => NotePropertiesEqual(pair.First, pair.Second));

    private static bool IntArraysEqual(int[] a, int[] b)
        => a.Length == b.Length && a.SequenceEqual(b);

    private static bool NotePropertiesEqual(GpifNoteProperty a, GpifNoteProperty b)
        => string.Equals(a.Name, b.Name, StringComparison.Ordinal)
           && a.Enabled == b.Enabled
           && a.Flags == b.Flags
           && a.Number == b.Number
           && a.Fret == b.Fret
           && a.StringNumber == b.StringNumber
           && string.Equals(a.HType, b.HType, StringComparison.Ordinal)
           && a.HFret == b.HFret
           && a.Float == b.Float;

    private static bool ArticulationsEqual(GpifNoteArticulation a, GpifNoteArticulation b)
        => string.Equals(a.LeftFingering, b.LeftFingering, StringComparison.Ordinal)
           && string.Equals(a.RightFingering, b.RightFingering, StringComparison.Ordinal)
           && string.Equals(a.Ornament, b.Ornament, StringComparison.Ordinal)
           && a.LetRing == b.LetRing
           && string.Equals(a.Vibrato, b.Vibrato, StringComparison.Ordinal)
           && a.TieOrigin == b.TieOrigin
           && a.TieDestination == b.TieDestination
           && a.Trill == b.Trill
           && a.Accent == b.Accent
           && a.AntiAccent == b.AntiAccent
           && string.Equals(a.AntiAccentValue, b.AntiAccentValue, StringComparison.Ordinal)
           && a.InstrumentArticulation == b.InstrumentArticulation
           && a.PalmMuted == b.PalmMuted
           && a.Muted == b.Muted
           && a.Tapped == b.Tapped
           && a.LeftHandTapped == b.LeftHandTapped
           && a.HopoOrigin == b.HopoOrigin
           && a.HopoDestination == b.HopoDestination
           && a.SlideFlags == b.SlideFlags
           && a.BendEnabled == b.BendEnabled
           && a.BendOriginOffset == b.BendOriginOffset
           && a.BendOriginValue == b.BendOriginValue
           && a.BendMiddleOffset1 == b.BendMiddleOffset1
           && a.BendMiddleOffset2 == b.BendMiddleOffset2
           && a.BendMiddleValue == b.BendMiddleValue
           && a.BendDestinationOffset == b.BendDestinationOffset
           && a.BendDestinationValue == b.BendDestinationValue
           && a.HarmonicEnabled == b.HarmonicEnabled
           && a.HarmonicType == b.HarmonicType
           && string.Equals(a.HarmonicTypeText, b.HarmonicTypeText, StringComparison.Ordinal)
           && a.HarmonicFret == b.HarmonicFret;

    private static bool TupletsEqual(RawTupletRatio? a, RawTupletRatio? b)
        => a?.Numerator == b?.Numerator
           && a?.Denominator == b?.Denominator;

    private static bool DictionariesEqual<TValue>(IReadOnlyDictionary<string, TValue> a, IReadOnlyDictionary<string, TValue> b)
        where TValue : notnull
    {
        if (a.Count != b.Count)
        {
            return false;
        }

        foreach (var (key, value) in a)
        {
            if (!b.TryGetValue(key, out var otherValue) || !EqualityComparer<TValue>.Default.Equals(value, otherValue))
            {
                return false;
            }
        }

        return true;
    }

    private static GpifRhythm? TryPreserveSourceRhythmShape(Beat beat)
    {
        var beatMetadata = GetBeatMetadata(beat);
        if (beatMetadata.SourceRhythm is null)
        {
            return null;
        }

        var sourceRhythm = new GpifRhythm
        {
            Xml = beatMetadata.SourceRhythm.Xml,
            Id = 0,
            NoteValue = beatMetadata.SourceRhythm.NoteValue,
            AugmentationDots = beatMetadata.SourceRhythm.AugmentationDots,
            AugmentationDotUsesCountAttribute = beatMetadata.SourceRhythm.AugmentationDotUsesCountAttribute,
            AugmentationDotCounts = beatMetadata.SourceRhythm.AugmentationDotCounts,
            PrimaryTuplet = ToRawTuplet(beatMetadata.SourceRhythm.PrimaryTuplet),
            SecondaryTuplet = ToRawTuplet(beatMetadata.SourceRhythm.SecondaryTuplet)
        };

        return NearlyEqual(ResolveRhythmDuration(sourceRhythm), beat.Duration)
            ? sourceRhythm
            : null;
    }

    private static RawTupletRatio? ToRawTuplet(CoreTupletRatio? tuplet)
        => tuplet is null
            ? null
            : new RawTupletRatio
            {
                Numerator = tuplet.Numerator,
                Denominator = tuplet.Denominator
            };

    private sealed record SourceStaffShape(
        int? Id,
        string Cref,
        int[] TuningPitches,
        int? CapoFret,
        IReadOnlyDictionary<string, string> Properties);

    private sealed record SourceStringContext(
        int[] TuningPitches,
        int? CapoFret);

    private static decimal ResolveRhythmDuration(GpifRhythm rhythm)
    {
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

    private static decimal TupletFactor(RawTupletRatio? tuplet)
    {
        if (tuplet is null || tuplet.Numerator <= 0 || tuplet.Denominator <= 0)
        {
            return 1m;
        }

        return (decimal)tuplet.Denominator / tuplet.Numerator;
    }

    private readonly record struct RhythmSignature(
        string NoteValue,
        int AugmentationDots,
        bool AugmentationDotUsesCountAttribute,
        string AugmentationDotCountsKey,
        int? PrimaryNumerator,
        int? PrimaryDenominator,
        int? SecondaryNumerator,
        int? SecondaryDenominator);

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
                            PrimaryTuplet = new RawTupletRatio { Numerator = tr.Item1, Denominator = tr.Item2 }
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
