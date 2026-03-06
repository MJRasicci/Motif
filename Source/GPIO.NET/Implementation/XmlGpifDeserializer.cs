namespace GPIO.NET.Implementation;

using GPIO.NET.Abstractions;
using GPIO.NET.Models.Raw;
using System.Xml.Linq;

public sealed class XmlGpifDeserializer : IGpifDeserializer
{
    public ValueTask<GpifDocument> DeserializeAsync(Stream scoreStream, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var doc = XDocument.Load(scoreStream);
        var root = doc.Root ?? throw new InvalidDataException("GPIF root element missing.");

        var score = root.Element("Score");
        var masterTrack = root.Element("MasterTrack");

        var tracksContainer = root.Elements("Tracks").FirstOrDefault(t => t.Elements("Track").Any());
        var tracks = tracksContainer?.Elements("Track")
            .Select(t =>
            {
                var trackProperties = ParsePropertyDictionary(t.Element("Properties"));
                var stavesElement = t.Element("Staves");
                var staves = ParseStaffs(stavesElement);

                var tuningProperty = FindLastProperty(t.Element("Properties"), "Tuning");
                trackProperties.TryGetValue("Tuning", out var trackTuningRaw);

                var tuningPitches = SplitInts(tuningProperty?.Element("Pitches")?.Value);
                if (tuningPitches.Length == 0)
                {
                    tuningPitches = SplitInts(trackTuningRaw);
                }

                var tuningInstrument = tuningProperty?.Element("Instrument")?.Value?.Trim() ?? string.Empty;
                var tuningLabel = tuningProperty?.Element("Label")?.Value?.Trim() ?? string.Empty;
                var tuningLabelVisible = TryParseNullableBool(tuningProperty?.Element("LabelVisible")?.Value);

                if (tuningPitches.Length == 0)
                {
                    tuningPitches = staves.FirstOrDefault(s => s.TuningPitches.Length > 0)?.TuningPitches ?? Array.Empty<int>();
                }

                var staffTuningProperty = FindLastProperty(
                    stavesElement?.Elements("Staff").FirstOrDefault()?.Element("Properties"),
                    "Tuning");

                if (string.IsNullOrWhiteSpace(tuningInstrument))
                {
                    tuningInstrument = staffTuningProperty?.Element("Instrument")?.Value?.Trim() ?? string.Empty;
                }

                if (string.IsNullOrWhiteSpace(tuningLabel))
                {
                    tuningLabel = staffTuningProperty?.Element("Label")?.Value?.Trim() ?? string.Empty;
                }

                if (!tuningLabelVisible.HasValue)
                {
                    tuningLabelVisible = TryParseNullableBool(staffTuningProperty?.Element("LabelVisible")?.Value);
                }

                return new GpifTrack
                {
                    Id = ParseInt(t.Attribute("id")?.Value),
                    Name = t.Element("Name")?.Value ?? string.Empty,
                    ShortName = t.Element("ShortName")?.Value ?? string.Empty,
                    Color = t.Element("Color")?.Value ?? string.Empty,
                    SystemsDefaultLayout = t.Element("SystemsDefautLayout")?.Value ?? string.Empty,
                    SystemsLayout = t.Element("SystemsLayout")?.Value ?? string.Empty,
                    PalmMute = TryParseNullableDecimal(t.Element("PalmMute")?.Value),
                    AutoAccentuation = TryParseNullableDecimal(t.Element("AutoAccentuation")?.Value),
                    AutoBrush = t.Element("AutoBrush") is not null,
                    PlayingStyle = t.Element("PlayingStyle")?.Value ?? string.Empty,
                    UseOneChannelPerString = t.Element("UseOneChannelPerString") is not null,
                    IconId = TryParseNullableInt(t.Element("IconId")?.Value),
                    ForcedSound = TryParseNullableInt(t.Element("ForcedSound")?.Value),
                    TuningPitches = tuningPitches,
                    TuningInstrument = tuningInstrument,
                    TuningLabel = tuningLabel,
                    TuningLabelVisible = tuningLabelVisible,
                    Properties = trackProperties,
                    InstrumentSetXml = t.Element("InstrumentSet")?.ToString(SaveOptions.DisableFormatting) ?? string.Empty,
                    StavesXml = stavesElement?.ToString(SaveOptions.DisableFormatting) ?? string.Empty,
                    SoundsXml = t.Element("Sounds")?.ToString(SaveOptions.DisableFormatting) ?? string.Empty,
                    RseXml = t.Element("RSE")?.ToString(SaveOptions.DisableFormatting) ?? string.Empty,
                    InstrumentSet = ParseInstrumentSet(t.Element("InstrumentSet")),
                    Sounds = ParseSounds(t.Element("Sounds")),
                    ChannelRse = ParseRse(t.Element("RSE")),
                    PlaybackStateXml = t.Element("PlaybackState")?.ToString(SaveOptions.DisableFormatting) ?? string.Empty,
                    AudioEngineStateXml = t.Element("AudioEngineState")?.ToString(SaveOptions.DisableFormatting) ?? string.Empty,
                    PlaybackState = ParsePlaybackState(t.Element("PlaybackState")),
                    MidiConnectionXml = t.Element("MidiConnection")?.ToString(SaveOptions.DisableFormatting) ?? string.Empty,
                    LyricsXml = t.Element("Lyrics")?.ToString(SaveOptions.DisableFormatting) ?? string.Empty,
                    AutomationsXml = t.Element("Automations")?.ToString(SaveOptions.DisableFormatting) ?? string.Empty,
                    Automations = ParseAutomations(t.Element("Automations")),
                    TransposeXml = t.Element("Transpose")?.ToString(SaveOptions.DisableFormatting) ?? string.Empty,
                    Staffs = staves
                };
            })
            .ToArray() ?? Array.Empty<GpifTrack>();

        var masterBars = (root.Element("MasterBars")?.Elements("MasterBar") ?? Enumerable.Empty<XElement>())
            .Select((mb, index) =>
            {
                var repeat = mb.Element("Repeat");
                var section = mb.Element("Section");
                var directions = mb.Element("Directions");

                var key = mb.Element("Key");
                var fermatas = (mb.Element("Fermatas")?.Elements("Fermata") ?? Enumerable.Empty<XElement>())
                    .Select(f => new GpifFermata
                    {
                        Type = f.Element("Type")?.Value ?? string.Empty,
                        Offset = f.Element("Offset")?.Value ?? string.Empty,
                        Length = TryParseNullableDecimal(f.Element("Length")?.Value)
                    })
                    .ToArray();
                var xprops = (mb.Element("XProperties")?.Elements("XProperty") ?? Enumerable.Empty<XElement>())
                    .Where(x => x.Attribute("id") is not null)
                    .Select(x => new { Id = x.Attribute("id")!.Value, Int = ParseInt(x.Element("Int")?.Value) })
                    .Where(x => x.Int >= 0)
                    .ToDictionary(x => x.Id, x => x.Int);

                var directionProps = (directions?.Elements() ?? Enumerable.Empty<XElement>())
                    .GroupBy(e => e.Name.LocalName, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.Last().Value ?? string.Empty, StringComparer.OrdinalIgnoreCase);

                return new GpifMasterBar
                {
                    Index = index,
                    Time = mb.Element("Time")?.Value ?? string.Empty,
                    BarsReferenceList = mb.Element("Bars")?.Value ?? string.Empty,
                    AlternateEndings = mb.Element("AlternateEndings")?.Value ?? string.Empty,
                    RepeatStart = ParseBool(repeat?.Attribute("start")?.Value),
                    RepeatEnd = ParseBool(repeat?.Attribute("end")?.Value),
                    RepeatCount = ParseInt(repeat?.Attribute("count")?.Value),
                    SectionLetter = section?.Element("Letter")?.Value ?? string.Empty,
                    SectionText = section?.Element("Text")?.Value ?? string.Empty,
                    Jump = directions?.Element("Jump")?.Value ?? string.Empty,
                    Target = directions?.Element("Target")?.Value ?? string.Empty,
                    DirectionProperties = directionProps,
                    KeyAccidentalCount = TryParseNullableInt(key?.Element("AccidentalCount")?.Value),
                    KeyMode = key?.Element("Mode")?.Value ?? string.Empty,
                    KeyTransposeAs = key?.Element("TransposeAs")?.Value ?? string.Empty,
                    Fermatas = fermatas,
                    XProperties = xprops
                };
            })
            .ToArray();

        var bars = (root.Element("Bars")?.Elements("Bar") ?? Enumerable.Empty<XElement>())
            .Select(b =>
            {
                var props = ParsePropertyDictionary(b.Element("Properties"));
                var xprops = (b.Element("XProperties")?.Elements("XProperty") ?? Enumerable.Empty<XElement>())
                    .Where(x => x.Attribute("id") is not null)
                    .Select(x => new { Id = x.Attribute("id")!.Value, Int = ParseInt(x.Element("Int")?.Value) })
                    .Where(x => x.Int >= 0)
                    .ToDictionary(x => x.Id, x => x.Int);

                return new GpifBar
                {
                    Id = ParseInt(b.Attribute("id")?.Value),
                    VoicesReferenceList = b.Element("Voices")?.Value ?? string.Empty,
                    Clef = b.Element("Clef")?.Value ?? string.Empty,
                    XProperties = xprops,
                    Properties = props
                };
            })
            .ToDictionary(b => b.Id);

        var voices = (root.Element("Voices")?.Elements("Voice") ?? Enumerable.Empty<XElement>())
            .Select(v =>
            {
                var props = ParsePropertyDictionary(v.Element("Properties"));
                var dirTags = (v.Element("Directions")?.Elements() ?? Enumerable.Empty<XElement>())
                    .Select(e => e.Name.LocalName)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                return new GpifVoice
                {
                    Id = ParseInt(v.Attribute("id")?.Value),
                    BeatsReferenceList = v.Element("Beats")?.Value ?? string.Empty,
                    Properties = props,
                    DirectionTags = dirTags
                };
            })
            .ToDictionary(v => v.Id);

        var rhythms = (root.Element("Rhythms")?.Elements("Rhythm") ?? Enumerable.Empty<XElement>())
            .Select(r =>
            {
                var primaryTuplet = r.Element("PrimaryTuplet");
                var secondaryTuplet = r.Element("SecondaryTuplet");

                return new GpifRhythm
                {
                    Id = ParseInt(r.Attribute("id")?.Value),
                    NoteValue = r.Element("NoteValue")?.Value ?? string.Empty,
                    AugmentationDots = r.Elements("AugmentationDot").Count(),
                    PrimaryTuplet = ParseTuplet(primaryTuplet),
                    SecondaryTuplet = ParseTuplet(secondaryTuplet)
                };
            })
            .ToDictionary(r => r.Id);

        var notes = (root.Element("Notes")?.Elements("Note") ?? Enumerable.Empty<XElement>())
            .Select(n =>
            {
                var properties = ParseNoteProperties(n);
                return new GpifNote
                {
                    Id = ParseInt(n.Attribute("id")?.Value),
                    MidiPitch = ParseMidiPitch(n),
                    Properties = properties,
                    Articulation = ParseArticulation(n, properties)
                };
            })
            .ToDictionary(n => n.Id);

        var beats = (root.Element("Beats")?.Elements("Beat") ?? Enumerable.Empty<XElement>())
            .Select(b =>
            {
                var properties = ParsePropertyDictionary(b.Element("Properties"));
                properties.TryGetValue("PickStroke", out var pickStrokeDirection);
                properties.TryGetValue("VibratoWTremBar", out var vibratoWithTremBarStrength);
                properties.TryGetValue("Slapped", out var slappedRaw);
                properties.TryGetValue("Popped", out var poppedRaw);
                properties.TryGetValue("Brush", out var brushRaw);

                return new GpifBeat
                {
                    Id = ParseInt(b.Attribute("id")?.Value),
                    RhythmRef = ParseInt(b.Element("Rhythm")?.Attribute("ref")?.Value),
                    NotesReferenceList = b.Element("Notes")?.Value ?? string.Empty,
                    GraceType = b.Element("GraceNotes")?.Value ?? string.Empty,
                    PickStrokeDirection = pickStrokeDirection ?? string.Empty,
                    VibratoWithTremBarStrength = vibratoWithTremBarStrength ?? string.Empty,
                    Slapped = string.Equals(slappedRaw, "true", StringComparison.OrdinalIgnoreCase),
                    Popped = string.Equals(poppedRaw, "true", StringComparison.OrdinalIgnoreCase),
                    Brush = !string.IsNullOrWhiteSpace(brushRaw),
                    BrushIsUp = string.Equals(brushRaw, "Up", StringComparison.OrdinalIgnoreCase)
                };
            })
            .ToDictionary(b => b.Id);

        return ValueTask.FromResult(new GpifDocument
        {
            Score = new ScoreInfo
            {
                Title = score?.Element("Title")?.Value ?? string.Empty,
                SubTitle = score?.Element("SubTitle")?.Value ?? string.Empty,
                Artist = score?.Element("Artist")?.Value ?? string.Empty,
                Album = score?.Element("Album")?.Value ?? string.Empty,
                Words = score?.Element("Words")?.Value ?? string.Empty,
                Music = score?.Element("Music")?.Value ?? string.Empty,
                WordsAndMusic = score?.Element("WordsAndMusic")?.Value ?? string.Empty,
                Copyright = score?.Element("Copyright")?.Value ?? string.Empty,
                Tabber = score?.Element("Tabber")?.Value ?? string.Empty,
                Instructions = score?.Element("Instructions")?.Value ?? string.Empty,
                Notices = score?.Element("Notices")?.Value ?? string.Empty,
                FirstPageHeader = score?.Element("FirstPageHeader")?.Value ?? string.Empty,
                FirstPageFooter = score?.Element("FirstPageFooter")?.Value ?? string.Empty,
                PageHeader = score?.Element("PageHeader")?.Value ?? string.Empty,
                PageFooter = score?.Element("PageFooter")?.Value ?? string.Empty,
                ScoreSystemsDefaultLayout = score?.Element("ScoreSystemsDefaultLayout")?.Value ?? string.Empty,
                ScoreSystemsLayout = score?.Element("ScoreSystemsLayout")?.Value ?? string.Empty,
                ScoreZoomPolicy = score?.Element("ScoreZoomPolicy")?.Value ?? string.Empty,
                ScoreZoom = score?.Element("ScoreZoom")?.Value ?? string.Empty,
                MultiVoice = score?.Element("MultiVoice")?.Value ?? string.Empty
            },
            MasterTrack = new GpifMasterTrack
            {
                TrackIds = SplitInts(masterTrack?.Element("Tracks")?.Value),
                Automations = ParseAutomations(masterTrack?.Element("Automations")),
                Anacrusis = masterTrack?.Element("Anacrusis") is not null,
                RseXml = masterTrack?.Element("RSE")?.ToString(SaveOptions.DisableFormatting) ?? string.Empty
            },
            Tracks = tracks,
            MasterBars = masterBars,
            BarsById = bars,
            VoicesById = voices,
            BeatsById = beats,
            NotesById = notes,
            RhythmsById = rhythms
        });
    }

    private static int ParseInt(string? value)
        => int.TryParse(value, out var result) ? result : -1;

    private static bool ParseBool(string? value)
        => bool.TryParse(value, out var result) && result;

    private static int? ParseMidiPitch(XElement note)
    {
        var pitch = note.Descendants("Pitch").FirstOrDefault();
        if (pitch is null)
        {
            return null;
        }

        var step = pitch.Element("Step")?.Value;
        var accidental = pitch.Element("Accidental")?.Value;
        var octaveRaw = pitch.Element("Octave")?.Value;

        if (string.IsNullOrWhiteSpace(step) || !int.TryParse(octaveRaw, out var octave))
        {
            return null;
        }

        var baseValue = step switch
        {
            "C" => 0,
            "D" => 2,
            "E" => 4,
            "F" => 5,
            "G" => 7,
            "A" => 9,
            "B" => 11,
            _ => 0
        };

        var accidentalValue = accidental switch
        {
            "#" or "Sharp" => 1,
            "##" or "DoubleSharp" => 2,
            "b" or "Flat" => -1,
            "bb" or "DoubleFlat" => -2,
            _ => 0
        };

        return (octave * 12) + baseValue + accidentalValue;
    }

    private static IReadOnlyList<GpifNoteProperty> ParseNoteProperties(XElement note)
        => (note.Element("Properties")?.Elements("Property") ?? Enumerable.Empty<XElement>())
            .Select(p => new GpifNoteProperty
            {
                Name = p.Attribute("name")?.Value ?? string.Empty,
                Enabled = p.Element("Enable") is not null,
                Flags = TryParseNullableInt(p.Element("Flags")?.Value),
                Number = TryParseNullableInt(p.Element("Number")?.Value),
                Fret = TryParseNullableInt(p.Element("Fret")?.Value),
                StringNumber = TryParseNullableInt(p.Element("String")?.Value),
                HType = p.Element("HType")?.Value ?? string.Empty,
                HFret = TryParseNullableDecimal(p.Element("HFret")?.Value),
                Float = TryParseNullableDecimal(p.Element("Float")?.Value)
            })
            .ToArray();

    private static GpifNoteArticulation ParseArticulation(XElement note, IReadOnlyList<GpifNoteProperty> properties)
    {
        var tie = note.Element("Tie");
        bool HasEnabledProperty(string name)
            => properties.Any(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase) && p.Enabled);

        int? GetPropertyFlags(string name)
            => properties.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))?.Flags;

        int? GetPropertyInt(string name)
            => properties.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))?.Number;

        string GetPropertyText(string name)
            => properties.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))?.HType ?? string.Empty;

        decimal? GetPropertyDecimal(string name)
        {
            var prop = properties.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
            if (prop is null)
            {
                return null;
            }

            if (prop.HFret.HasValue)
            {
                return prop.HFret;
            }

            if (prop.Float.HasValue)
            {
                return prop.Float;
            }

            return prop.Number;
        }

        return new GpifNoteArticulation
        {
            LeftFingering = note.Element("LeftFingering")?.Value ?? string.Empty,
            RightFingering = note.Element("RightFingering")?.Value ?? string.Empty,
            Ornament = note.Element("Ornament")?.Value ?? string.Empty,
            LetRing = note.Element("LetRing") is not null,
            Vibrato = note.Element("Vibrato")?.Value ?? string.Empty,
            TieOrigin = ParseBool(tie?.Attribute("origin")?.Value),
            TieDestination = ParseBool(tie?.Attribute("destination")?.Value),
            Trill = TryParseNullableInt(note.Element("Trill")?.Value),
            Accent = TryParseNullableInt(note.Element("Accent")?.Value),
            AntiAccent = note.Element("AntiAccent") is not null,
            InstrumentArticulation = TryParseNullableInt(note.Element("InstrumentArticulation")?.Value),
            PalmMuted = HasEnabledProperty("PalmMuted"),
            Muted = HasEnabledProperty("Muted"),
            Tapped = HasEnabledProperty("Tapped"),
            LeftHandTapped = HasEnabledProperty("LeftHandTapped"),
            HopoOrigin = HasEnabledProperty("HopoOrigin"),
            HopoDestination = HasEnabledProperty("HopoDestination"),
            SlideFlags = GetPropertyFlags("Slide"),
            BendEnabled = HasEnabledProperty("Bended"),
            BendOriginOffset = GetPropertyDecimal("BendOriginOffset"),
            BendOriginValue = GetPropertyDecimal("BendOriginValue"),
            BendMiddleOffset1 = GetPropertyDecimal("BendMiddleOffset1"),
            BendMiddleOffset2 = GetPropertyDecimal("BendMiddleOffset2"),
            BendMiddleValue = GetPropertyDecimal("BendMiddleValue"),
            BendDestinationOffset = GetPropertyDecimal("BendDestinationOffset"),
            BendDestinationValue = GetPropertyDecimal("BendDestinationValue"),
            HarmonicEnabled = HasEnabledProperty("Harmonic"),
            HarmonicType = GetPropertyInt("HarmonicType"),
            HarmonicTypeText = GetPropertyText("HarmonicType"),
            HarmonicFret = GetPropertyDecimal("HarmonicFret")
        };
    }

    private static int? TryParseNullableInt(string? value)
        => int.TryParse(value, out var parsed) ? parsed : null;

    private static decimal? TryParseNullableDecimal(string? value)
        => decimal.TryParse(value, out var parsed) ? parsed : null;

    private static bool? TryParseNullableBool(string? value)
        => bool.TryParse(value, out var parsed) ? parsed : null;

    private static int[] SplitInts(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? Array.Empty<int>()
            : value.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(v => int.TryParse(v, out var i) ? i : int.MinValue)
                .Where(i => i != int.MinValue)
                .ToArray();

    private static GpifInstrumentSet ParseInstrumentSet(XElement? instrumentSet)
        => new()
        {
            Name = instrumentSet?.Element("Name")?.Value ?? string.Empty,
            Type = instrumentSet?.Element("Type")?.Value ?? string.Empty,
            LineCount = TryParseNullableInt(instrumentSet?.Element("LineCount")?.Value)
        };

    private static GpifSound[] ParseSounds(XElement? sounds)
        => (sounds?.Elements("Sound") ?? Enumerable.Empty<XElement>())
            .Select(s => new GpifSound
            {
                Name = s.Element("Name")?.Value ?? string.Empty,
                Label = s.Element("Label")?.Value ?? string.Empty,
                Path = s.Element("Path")?.Value ?? string.Empty,
                Role = s.Element("Role")?.Value ?? string.Empty,
                MidiLsb = TryParseNullableInt(s.Element("MIDI")?.Element("LSB")?.Value),
                MidiMsb = TryParseNullableInt(s.Element("MIDI")?.Element("MSB")?.Value),
                MidiProgram = TryParseNullableInt(s.Element("MIDI")?.Element("Program")?.Value)
            })
            .ToArray();

    private static GpifRse ParseRse(XElement? rse)
        => new()
        {
            ChannelStripVersion = rse?.Element("ChannelStrip")?.Attribute("version")?.Value ?? string.Empty,
            ChannelStripParameters = rse?.Element("ChannelStrip")?.Element("Parameters")?.Value ?? string.Empty
        };

    private static GpifPlaybackState ParsePlaybackState(XElement? playbackState)
        => new()
        {
            Value = playbackState?.Value?.Trim() ?? string.Empty
        };

    private static GpifAutomation[] ParseAutomations(XElement? automations)
        => (automations?.Elements("Automation") ?? Enumerable.Empty<XElement>())
            .Select(a => new GpifAutomation
            {
                Type = a.Element("Type")?.Value ?? string.Empty,
                Linear = TryParseNullableBool(a.Element("Linear")?.Value),
                Bar = TryParseNullableInt(a.Element("Bar")?.Value),
                Position = TryParseNullableInt(a.Element("Position")?.Value),
                Visible = TryParseNullableBool(a.Element("Visible")?.Value),
                Value = a.Element("Value")?.Value ?? string.Empty
            })
            .ToArray();

    private static GpifStaff[] ParseStaffs(XElement? staves)
    {
        if (staves is null)
        {
            return Array.Empty<GpifStaff>();
        }

        return staves.Elements("Staff")
            .Select(staff =>
            {
                var props = ParsePropertyDictionary(staff.Element("Properties"));

                props.TryGetValue("Tuning", out var tuningRaw);
                var tuningPitches = SplitInts(tuningRaw);
                if (tuningPitches.Length == 0)
                {
                    tuningPitches = (staff.Descendants("Pitch") ?? Enumerable.Empty<XElement>())
                        .Select(p => int.TryParse(p.Value, out var i) ? i : int.MinValue)
                        .Where(i => i != int.MinValue)
                        .ToArray();
                }

                props.TryGetValue("CapoFret", out var capoRaw);

                return new GpifStaff
                {
                    Id = TryParseNullableInt(staff.Attribute("id")?.Value),
                    Cref = staff.Attribute("cref")?.Value ?? string.Empty,
                    TuningPitches = tuningPitches,
                    CapoFret = TryParseNullableInt(capoRaw),
                    Properties = props,
                    Xml = staff.ToString(SaveOptions.DisableFormatting)
                };
            })
            .ToArray();
    }

    private static Dictionary<string, string> ParsePropertyDictionary(XElement? propertiesElement)
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

            values[name] = ParsePropertyValue(property);
        }

        return values;
    }

    private static XElement? FindLastProperty(XElement? propertiesElement, string propertyName)
    {
        if (propertiesElement is null)
        {
            return null;
        }

        return propertiesElement.Elements("Property")
            .LastOrDefault(p => string.Equals(p.Attribute("name")?.Value, propertyName, StringComparison.OrdinalIgnoreCase));
    }

    private static string ParsePropertyValue(XElement property)
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

    private static TupletRatio? ParseTuplet(XElement? tuplet)
    {
        if (tuplet is null)
        {
            return null;
        }

        var numerator = ParseInt(tuplet.Element("Num")?.Value);
        var denominator = ParseInt(tuplet.Element("Den")?.Value);
        if (numerator <= 0 || denominator <= 0)
        {
            return null;
        }

        return new TupletRatio
        {
            Numerator = numerator,
            Denominator = denominator
        };
    }
}
