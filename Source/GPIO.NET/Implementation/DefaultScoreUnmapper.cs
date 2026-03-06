namespace GPIO.NET.Implementation;

using GPIO.NET.Abstractions;
using GPIO.NET.Models;
using GPIO.NET.Models.Raw;
using GPIO.NET.Models.Write;
using GPIO.NET.Utilities;

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
                ForcedSound = t.Metadata.ForcedSound,
                TuningPitches = t.Metadata.TuningPitches,
                TuningInstrument = t.Metadata.TuningInstrument,
                TuningLabel = t.Metadata.TuningLabel,
                TuningLabelVisible = t.Metadata.TuningLabelVisible,
                Properties = t.Metadata.Properties,
                InstrumentSetXml = t.Metadata.InstrumentSetXml,
                StavesXml = t.Metadata.StavesXml,
                SoundsXml = t.Metadata.SoundsXml,
                RseXml = t.Metadata.RseXml,
                InstrumentSet = new GpifInstrumentSet
                {
                    Name = t.Metadata.InstrumentSet.Name,
                    Type = t.Metadata.InstrumentSet.Type,
                    LineCount = t.Metadata.InstrumentSet.LineCount,
                    Elements = t.Metadata.InstrumentSet.Elements.Select(element => new GpifInstrumentElement
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
                Sounds = t.Metadata.Sounds.Select(s => new GpifSound
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
                    Bank = t.Metadata.Rse.Bank,
                    ChannelStripVersion = t.Metadata.Rse.ChannelStripVersion,
                    ChannelStripParameters = t.Metadata.Rse.ChannelStripParameters,
                    Automations = t.Metadata.Rse.Automations.Select(a => new GpifAutomation
                    {
                        Type = a.Type,
                        Linear = a.Linear,
                        Bar = a.Bar,
                        Position = a.Position,
                        Visible = a.Visible,
                        Value = a.Value
                    }).ToArray()
                },
                PlaybackStateXml = t.Metadata.PlaybackStateXml,
                AudioEngineStateXml = t.Metadata.AudioEngineStateXml,
                PlaybackState = new GpifPlaybackState { Value = t.Metadata.PlaybackState.Value },
                AudioEngineState = new GpifAudioEngineState { Value = t.Metadata.AudioEngineState.Value },
                MidiConnectionXml = t.Metadata.MidiConnectionXml,
                LyricsXml = t.Metadata.LyricsXml,
                AutomationsXml = t.Metadata.AutomationsXml,
                Automations = t.Metadata.Automations.Select(a => new GpifAutomation
                {
                    Type = a.Type,
                    Linear = a.Linear,
                    Bar = a.Bar,
                    Position = a.Position,
                    Visible = a.Visible,
                    Value = a.Value
                }).ToArray(),
                TransposeXml = t.Metadata.TransposeXml,
                MidiConnection = new GpifMidiConnection
                {
                    Port = t.Metadata.MidiConnection.Port,
                    PrimaryChannel = t.Metadata.MidiConnection.PrimaryChannel,
                    SecondaryChannel = t.Metadata.MidiConnection.SecondaryChannel,
                    ForceOneChannelPerString = t.Metadata.MidiConnection.ForceOneChannelPerString
                },
                Lyrics = new GpifLyrics
                {
                    Dispatched = t.Metadata.Lyrics.Dispatched,
                    Lines = t.Metadata.Lyrics.Lines.Select(line => new GpifLyricsLine
                    {
                        Text = line.Text,
                        Offset = line.Offset
                    }).ToArray()
                },
                Transpose = new GpifTranspose
                {
                    Chromatic = t.Metadata.Transpose.Chromatic,
                    Octave = t.Metadata.Transpose.Octave
                },
                Staffs = t.Metadata.Staffs.Select(s => new GpifStaff
                {
                    Id = s.Id,
                    Cref = s.Cref,
                    TuningPitches = s.TuningPitches,
                    CapoFret = s.CapoFret,
                    Properties = s.Properties,
                    Xml = s.Xml
                }).ToArray()
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
                var jump = ResolveDirectionValue(measure.Jump, measure.DirectionProperties, "Jump");
                var target = ResolveDirectionValue(measure.Target, measure.DirectionProperties, "Target");
                var currentBarId = barId++;
                var measureVoiceIds = new List<int>();
                var measureVoices = ResolveMeasureVoices(measure);

                foreach (var measureVoice in measureVoices)
                {
                    var currentVoiceId = voiceId++;
                    var beatIds = new List<int>();

                    foreach (var beat in measureVoice.Beats)
                    {
                        var currentBeatId = beatId++;
                        var currentRhythmId = rhythmId++;
                        var noteRefs = new List<int>();

                        rhythms[currentRhythmId] = ToRhythm(beat.Duration, currentRhythmId, diagnostics);

                        if (beat.Notes.Count > 0)
                        {
                            foreach (var note in beat.Notes)
                            {
                                var bend = ArticulationDecoders.EncodeBend(note.Articulation.Bend);
                                var harmonic = ArticulationDecoders.EncodeHarmonic(note.Articulation.Harmonic);
                                var noteXProperties = new Dictionary<string, int>();
                                var encodedTrillSpeed = ArticulationDecoders.EncodeTrillSpeed(note.Articulation.TrillSpeed);
                                if (encodedTrillSpeed.HasValue)
                                {
                                    noteXProperties["688062467"] = encodedTrillSpeed.Value;
                                }

                                var currentNoteId = noteId++;
                                noteRefs.Add(currentNoteId);
                                notes[currentNoteId] = new GpifNote
                                {
                                    Id = currentNoteId,
                                    MidiPitch = note.MidiPitch,
                                    XProperties = noteXProperties,
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
                                        InstrumentArticulation = note.Articulation.InstrumentArticulation,
                                        PalmMuted = beat.PalmMuted || note.Articulation.PalmMuted,
                                        Muted = note.Articulation.Muted,
                                        Tapped = note.Articulation.Tapped,
                                        LeftHandTapped = note.Articulation.LeftHandTapped,
                                        HopoOrigin = note.Articulation.HopoOrigin,
                                        HopoDestination = note.Articulation.HopoDestination,
                                        SlideFlags = note.Articulation.SlideFlags ?? ArticulationDecoders.EncodeSlides(note.Articulation.Slides),
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
                            }
                        }

                        var encodedWhammy = ArticulationDecoders.EncodeWhammyBar(beat.WhammyBar);
                        var beatXProperties = new Dictionary<string, int>();
                        if (beat.BrushDurationTicks.HasValue)
                        {
                            beatXProperties[beat.Arpeggio ? "687931393" : "687935489"] = beat.BrushDurationTicks.Value;
                        }

                        beats[currentBeatId] = new GpifBeat
                        {
                            Id = currentBeatId,
                            RhythmRef = currentRhythmId,
                            NotesReferenceList = ReferenceListFormatter.JoinRefs(noteRefs),
                            GraceType = beat.GraceType,
                            Dynamic = beat.Dynamic,
                            PickStrokeDirection = beat.PickStrokeDirection,
                            VibratoWithTremBarStrength = beat.VibratoWithTremBarStrength,
                            Slapped = beat.Slapped,
                            Popped = beat.Popped,
                            Brush = beat.Brush,
                            BrushIsUp = beat.BrushIsUp,
                            Arpeggio = beat.Arpeggio,
                            BrushDurationTicks = beat.BrushDurationTicks,
                            Rasgueado = beat.Rasgueado,
                            DeadSlapped = beat.DeadSlapped,
                            Tremolo = beat.Tremolo,
                            TremoloValue = beat.TremoloValue,
                            ChordId = beat.ChordId,
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
                            XProperties = beatXProperties
                        };

                        beatIds.Add(currentBeatId);
                    }

                    voices[currentVoiceId] = new GpifVoice
                    {
                        Id = currentVoiceId,
                        BeatsReferenceList = ReferenceListFormatter.JoinRefs(beatIds),
                        Properties = measureVoice.Properties.ToDictionary(kv => kv.Key, kv => kv.Value),
                        DirectionTags = measureVoice.DirectionTags.ToArray()
                    };

                    measureVoiceIds.Add(currentVoiceId);
                }

                bars[currentBarId] = new GpifBar
                {
                    Id = currentBarId,
                    VoicesReferenceList = ReferenceListFormatter.JoinRefs(measureVoiceIds),
                    Clef = measure.Clef,
                    Properties = measure.BarProperties,
                    XProperties = measure.XProperties
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
                        Jump = jump,
                        Target = target,
                        DirectionProperties = measure.DirectionProperties,
                        KeyAccidentalCount = measure.KeyAccidentalCount,
                        KeyMode = measure.KeyMode,
                        KeyTransposeAs = measure.KeyTransposeAs,
                        Fermatas = measure.Fermatas.Select(f => new GpifFermata
                        {
                            Type = f.Type,
                            Offset = f.Offset,
                            Length = f.Length
                        }).ToArray(),
                        XProperties = measure.XProperties
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
                    Target = existing.Target,
                    DirectionProperties = existing.DirectionProperties,
                    KeyAccidentalCount = existing.KeyAccidentalCount,
                    KeyMode = existing.KeyMode,
                    KeyTransposeAs = existing.KeyTransposeAs,
                    Fermatas = existing.Fermatas,
                    XProperties = existing.XProperties
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
            MasterTrack = new GpifMasterTrack
            {
                TrackIds = score.MasterTrack.TrackIds,
                Automations = score.MasterTrack.Automations.Select(a => new GpifAutomation
                {
                    Type = a.Type,
                    Linear = a.Linear,
                    Bar = a.Bar,
                    Position = a.Position,
                    Visible = a.Visible,
                    Value = a.Value
                }).ToArray(),
                Anacrusis = score.MasterTrack.Anacrusis,
                RseXml = score.MasterTrack.RseXml,
                Rse = new GpifMasterRse
                {
                    MasterEffects = score.MasterTrack.Rse.MasterEffects.Select(effect => new GpifRseEffect
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
            RhythmsById = rhythms
        };

        return ValueTask.FromResult(new WriteResult
        {
            RawDocument = doc,
            Diagnostics = diagnostics
        });
    }

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

    private static IReadOnlyList<MeasureVoiceModel> ResolveMeasureVoices(MeasureModel measure)
    {
        if (measure.Voices.Count > 0)
        {
            return measure.Voices
                .OrderBy(v => v.VoiceIndex)
                .ToArray();
        }

        var fallbackProperties = measure.Beats.Count > 0
            ? measure.Beats[0].VoiceProperties
            : new Dictionary<string, string>();
        var fallbackDirectionTags = measure.Beats.Count > 0
            ? measure.Beats[0].VoiceDirectionTags
            : Array.Empty<string>();

        return
        [
            new MeasureVoiceModel
            {
                VoiceIndex = 0,
                SourceVoiceId = 0,
                Properties = fallbackProperties.ToDictionary(kv => kv.Key, kv => kv.Value),
                DirectionTags = fallbackDirectionTags.ToArray(),
                Beats = measure.Beats
            }
        ];
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
