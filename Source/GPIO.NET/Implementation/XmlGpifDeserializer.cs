namespace GPIO.NET.Implementation;

using GPIO.NET.Abstractions;
using GPIO.NET.Models.Raw;
using System.Xml.Linq;

public sealed class XmlGpifDeserializer : IGpifDeserializer
{
    public ValueTask<GpifDocument> DeserializeAsync(Stream scoreStream, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var doc = XDocument.Load(scoreStream, LoadOptions.PreserveWhitespace);
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
                    Xml = t.ToString(SaveOptions.DisableFormatting),
                    Id = ParseInt(t.Attribute("id")?.Value),
                    Name = t.Element("Name")?.Value ?? string.Empty,
                    ShortName = t.Element("ShortName")?.Value ?? string.Empty,
                    HasExplicitEmptyShortName = t.Element("ShortName") is not null
                        && string.IsNullOrWhiteSpace(t.Element("ShortName")?.Value),
                    Color = t.Element("Color")?.Value ?? string.Empty,
                    SystemsDefaultLayout = t.Element("SystemsDefautLayout")?.Value ?? string.Empty,
                    SystemsLayout = t.Element("SystemsLayout")?.Value ?? string.Empty,
                    HasExplicitEmptySystemsLayout = t.Element("SystemsLayout") is not null
                        && string.IsNullOrWhiteSpace(t.Element("SystemsLayout")?.Value),
                    PalmMute = TryParseNullableDecimal(t.Element("PalmMute")?.Value),
                    AutoAccentuation = TryParseNullableDecimal(t.Element("AutoAccentuation")?.Value),
                    AutoBrush = t.Element("AutoBrush") is not null,
                    LetRingThroughout = t.Element("LetRingThroughout") is not null,
                    PlayingStyle = t.Element("PlayingStyle")?.Value ?? string.Empty,
                    UseOneChannelPerString = t.Element("UseOneChannelPerString") is not null,
                    IconId = TryParseNullableInt(t.Element("IconId")?.Value),
                    ForcedSound = TryParseNullableInt(t.Element("ForcedSound")?.Value),
                    TuningPitches = tuningPitches,
                    TuningInstrument = tuningInstrument,
                    TuningLabel = tuningLabel,
                    TuningLabelVisible = tuningLabelVisible,
                    HasTrackTuningProperty = tuningProperty is not null,
                    Properties = trackProperties,
                    InstrumentSetXml = t.Element("InstrumentSet")?.ToString(SaveOptions.DisableFormatting) ?? string.Empty,
                    StavesXml = stavesElement?.ToString(SaveOptions.DisableFormatting) ?? string.Empty,
                    SoundsXml = t.Element("Sounds")?.ToString(SaveOptions.DisableFormatting) ?? string.Empty,
                    RseXml = t.Element("RSE")?.ToString(SaveOptions.DisableFormatting) ?? string.Empty,
                    NotationPatchXml = t.Element("NotationPatch")?.ToString(SaveOptions.DisableFormatting) ?? string.Empty,
                    InstrumentSet = ParseInstrumentSet(t.Element("InstrumentSet")),
                    Sounds = ParseSounds(t.Element("Sounds")),
                    ChannelRse = ParseRse(t.Element("RSE")),
                    PlaybackStateXml = t.Element("PlaybackState")?.ToString(SaveOptions.DisableFormatting) ?? string.Empty,
                    AudioEngineStateXml = t.Element("AudioEngineState")?.ToString(SaveOptions.DisableFormatting) ?? string.Empty,
                    PlaybackState = ParsePlaybackState(t.Element("PlaybackState")),
                    AudioEngineState = ParseAudioEngineState(t.Element("AudioEngineState")),
                    MidiConnectionXml = t.Element("MidiConnection")?.ToString(SaveOptions.DisableFormatting) ?? string.Empty,
                    LyricsXml = t.Element("Lyrics")?.ToString(SaveOptions.DisableFormatting) ?? string.Empty,
                    AutomationsXml = t.Element("Automations")?.ToString(SaveOptions.DisableFormatting) ?? string.Empty,
                    Automations = ParseAutomations(t.Element("Automations")),
                    TransposeXml = t.Element("Transpose")?.ToString(SaveOptions.DisableFormatting) ?? string.Empty,
                    MidiConnection = ParseMidiConnection(t.Element("MidiConnection")),
                    Lyrics = ParseLyrics(t.Element("Lyrics")),
                    Transpose = ParseTranspose(t.Element("Transpose")),
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
                var masterBarXProperties = mb.Element("XProperties");
                var xprops = ParseXPropertyInts(masterBarXProperties);

                var directionProps = (directions?.Elements() ?? Enumerable.Empty<XElement>())
                    .GroupBy(e => e.Name.LocalName, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.Last().Value ?? string.Empty, StringComparer.OrdinalIgnoreCase);

                return new GpifMasterBar
                {
                    Xml = mb.ToString(SaveOptions.DisableFormatting),
                    Index = index,
                    Time = mb.Element("Time")?.Value ?? string.Empty,
                    DoubleBar = mb.Element("DoubleBar") is not null,
                    FreeTime = mb.Element("FreeTime") is not null,
                    TripletFeel = mb.Element("TripletFeel")?.Value ?? string.Empty,
                    BarsReferenceList = mb.Element("Bars")?.Value ?? string.Empty,
                    AlternateEndings = mb.Element("AlternateEndings")?.Value ?? string.Empty,
                    RepeatStart = ParseBool(repeat?.Attribute("start")?.Value),
                    RepeatStartAttributePresent = repeat?.Attribute("start") is not null,
                    RepeatEnd = ParseBool(repeat?.Attribute("end")?.Value),
                    RepeatEndAttributePresent = repeat?.Attribute("end") is not null,
                    RepeatCount = ParseInt(repeat?.Attribute("count")?.Value),
                    RepeatCountAttributePresent = repeat?.Attribute("count") is not null,
                    SectionLetter = ReadDirectTextValue(section?.Element("Letter")),
                    SectionText = ReadDirectTextValue(section?.Element("Text")),
                    HasExplicitEmptySection = section is not null
                        && section.Elements().Any()
                        && section.Elements().All(child => ReadDirectTextValue(child).Length == 0),
                    Jump = directions?.Element("Jump")?.Value ?? string.Empty,
                    Target = directions?.Element("Target")?.Value ?? string.Empty,
                    DirectionProperties = directionProps,
                    DirectionsXml = directions?.ToString(SaveOptions.DisableFormatting) ?? string.Empty,
                    KeyAccidentalCount = TryParseNullableInt(key?.Element("AccidentalCount")?.Value),
                    KeyMode = key?.Element("Mode")?.Value ?? string.Empty,
                    KeyTransposeAs = key?.Element("TransposeAs")?.Value ?? string.Empty,
                    Fermatas = fermatas,
                    XProperties = xprops,
                    XPropertiesXml = masterBarXProperties?.ToString(SaveOptions.DisableFormatting) ?? string.Empty
                };
            })
            .ToArray();

        var bars = (root.Element("Bars")?.Elements("Bar") ?? Enumerable.Empty<XElement>())
            .Select(b =>
            {
                var props = ParsePropertyDictionary(b.Element("Properties"));
                var barXProperties = b.Element("XProperties");
                var xprops = ParseXPropertyInts(barXProperties);

                return new GpifBar
                {
                    Xml = b.ToString(SaveOptions.DisableFormatting),
                    Id = ParseInt(b.Attribute("id")?.Value),
                    VoicesReferenceList = b.Element("Voices")?.Value ?? string.Empty,
                    Clef = b.Element("Clef")?.Value ?? string.Empty,
                    SimileMark = b.Element("SimileMark")?.Value ?? string.Empty,
                    XProperties = xprops,
                    XPropertiesXml = barXProperties?.ToString(SaveOptions.DisableFormatting) ?? string.Empty,
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
                    Xml = v.ToString(SaveOptions.DisableFormatting),
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
                    Xml = r.ToString(SaveOptions.DisableFormatting),
                    Id = ParseInt(r.Attribute("id")?.Value),
                    NoteValue = r.Element("NoteValue")?.Value ?? string.Empty,
                    AugmentationDots = r.Elements("AugmentationDot")
                        .Select(dot => TryParseNullableInt(dot.Attribute("count")?.Value) ?? 1)
                        .Sum(),
                    AugmentationDotUsesCountAttribute = r.Elements("AugmentationDot").Any(dot => dot.Attribute("count") is not null),
                    AugmentationDotCounts = r.Elements("AugmentationDot")
                        .Select(dot => TryParseNullableInt(dot.Attribute("count")?.Value) ?? 1)
                        .ToArray(),
                    PrimaryTuplet = ParseTuplet(primaryTuplet),
                    SecondaryTuplet = ParseTuplet(secondaryTuplet)
                };
            })
            .ToDictionary(r => r.Id);

        var notes = (root.Element("Notes")?.Elements("Note") ?? Enumerable.Empty<XElement>())
            .Select(n =>
            {
                var properties = ParseNoteProperties(n);
                var noteXProperties = n.Element("XProperties");
                var noteXprops = ParseXPropertyInts(noteXProperties);

                return new GpifNote
                {
                    Xml = n.ToString(SaveOptions.DisableFormatting),
                    Id = ParseInt(n.Attribute("id")?.Value),
                    Velocity = TryParseNullableInt(n.Element("Velocity")?.Value),
                    MidiPitch = ParseNamedNumberProperty(n, "Midi") ?? ParseMidiPitch(n),
                    TransposedMidiPitch = ParseNamedMidiPitch(n, "TransposedPitch"),
                    ConcertPitch = ParseNamedPitchValue(n, "ConcertPitch"),
                    TransposedPitch = ParseNamedPitchValue(n, "TransposedPitch"),
                    SourceFret = ParseNamedFretProperty(n),
                    SourceStringNumber = ParseNamedStringProperty(n),
                    ShowStringNumber = HasEnabledNoteProperty(properties, "ShowStringNumber"),
                    Properties = properties,
                    Articulation = ParseArticulation(n, properties),
                    XProperties = noteXprops,
                    XPropertiesXml = noteXProperties?.ToString(SaveOptions.DisableFormatting) ?? string.Empty
                };
            })
            .ToDictionary(n => n.Id);

        var beats = (root.Element("Beats")?.Elements("Beat") ?? Enumerable.Empty<XElement>())
            .Select(b =>
            {
                var properties = ParsePropertyDictionary(b.Element("Properties"));
                var whammyElement = b.Element("Whammy");
                properties.TryGetValue("PickStroke", out var pickStrokeDirection);
                properties.TryGetValue("VibratoWTremBar", out var vibratoWithTremBarStrength);
                properties.TryGetValue("Slapped", out var slappedRaw);
                properties.TryGetValue("Popped", out var poppedRaw);
                properties.TryGetValue("Brush", out var brushRaw);
                properties.TryGetValue("Rasgueado", out var rasgueadoRaw);
                properties.TryGetValue("WhammyBar", out var whammyBarRaw);
                properties.TryGetValue("WhammyBarExtend", out var whammyBarExtendRaw);

                var hasArpeggio = b.Element("Arpeggio") is not null;
                var arpeggioDirection = b.Element("Arpeggio")?.Value ?? string.Empty;

                var beatXProperties = b.Element("XProperties");
                var xprops = ParseXPropertyInts(beatXProperties);

                int? brushDurationTicks = null;
                var brushDurationXPropertyId = string.Empty;
                if (xprops.TryGetValue("687935489", out var bd1))
                {
                    brushDurationTicks = bd1;
                    brushDurationXPropertyId = "687935489";
                }
                else if (xprops.TryGetValue("687931393", out var bd2))
                {
                    brushDurationTicks = bd2;
                    brushDurationXPropertyId = "687931393";
                }
                else if (!string.IsNullOrWhiteSpace(brushRaw))
                {
                    // Android parser defaults brush direction-only beats to 60 ticks.
                    brushDurationTicks = 60;
                }

                decimal? GetBeatPropertyFloat(string name)
                {
                    if (!properties.TryGetValue(name, out var raw) || string.IsNullOrWhiteSpace(raw))
                    {
                        return null;
                    }

                    return decimal.TryParse(raw, out var v) ? v : null;
                }

                decimal? GetWhammyAttribute(string attributeName)
                {
                    var raw = whammyElement?.Attribute(attributeName)?.Value;
                    return decimal.TryParse(raw, out var value) ? value : null;
                }

                var legato = b.Element("Legato");
                var transposedUserElement = b.Element("TransposedPitchStemOrientationUserDefined");
                var legacyUserElement = b.Element("UserTransposedPitchStemOrientation");
                var rasgueadoPattern = IsBeatBooleanTrue(rasgueadoRaw)
                    ? string.Empty
                    : rasgueadoRaw ?? string.Empty;

                return new GpifBeat
                {
                    Xml = b.ToString(SaveOptions.DisableFormatting),
                    Id = ParseInt(b.Attribute("id")?.Value),
                    RhythmRef = ParseInt(b.Element("Rhythm")?.Attribute("ref")?.Value),
                    NotesReferenceList = b.Element("Notes")?.Value ?? string.Empty,
                    GraceType = b.Element("GraceNotes")?.Value ?? string.Empty,
                    Dynamic = b.Element("Dynamic")?.Value ?? string.Empty,
                    TransposedPitchStemOrientation = b.Element("TransposedPitchStemOrientation")?.Value ?? string.Empty,
                    UserTransposedPitchStemOrientation = transposedUserElement?.Value ?? legacyUserElement?.Value ?? string.Empty,
                    HasTransposedPitchStemOrientationUserDefinedElement = transposedUserElement is not null,
                    ConcertPitchStemOrientation = b.Element("ConcertPitchStemOrientation")?.Value ?? string.Empty,
                    Wah = b.Element("Wah")?.Value ?? string.Empty,
                    Golpe = b.Element("Golpe")?.Value ?? string.Empty,
                    Fadding = b.Element("Fadding")?.Value ?? string.Empty,
                    Slashed = b.Element("Slashed") is not null,
                    Hairpin = b.Element("Hairpin")?.Value ?? string.Empty,
                    Variation = b.Element("Variation")?.Value ?? string.Empty,
                    Ottavia = b.Element("Ottavia")?.Value ?? string.Empty,
                    LegatoOrigin = TryParseNullableBool(legato?.Attribute("origin")?.Value),
                    LegatoDestination = TryParseNullableBool(legato?.Attribute("destination")?.Value),
                    LyricsXml = b.Element("Lyrics")?.ToString(SaveOptions.DisableFormatting) ?? string.Empty,
                    PickStrokeDirection = pickStrokeDirection ?? string.Empty,
                    VibratoWithTremBarStrength = vibratoWithTremBarStrength ?? string.Empty,
                    Slapped = string.Equals(slappedRaw, "true", StringComparison.OrdinalIgnoreCase),
                    Popped = string.Equals(poppedRaw, "true", StringComparison.OrdinalIgnoreCase),
                    Brush = !string.IsNullOrWhiteSpace(brushRaw) || hasArpeggio,
                    BrushIsUp = string.Equals(brushRaw, "Up", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(arpeggioDirection, "Up", StringComparison.OrdinalIgnoreCase),
                    Arpeggio = hasArpeggio,
                    BrushDurationTicks = brushDurationTicks,
                    BrushDurationXPropertyId = brushDurationXPropertyId,
                    HasExplicitBrushDurationXProperty = !string.IsNullOrWhiteSpace(brushDurationXPropertyId),
                    Rasgueado = IsBeatBooleanTrue(rasgueadoRaw) || !string.IsNullOrWhiteSpace(rasgueadoPattern),
                    RasgueadoPattern = rasgueadoPattern,
                    DeadSlapped = b.Element("DeadSlapped") is not null,
                    Tremolo = b.Element("Tremolo") is not null,
                    TremoloValue = b.Element("Tremolo")?.Value ?? string.Empty,
                    ChordId = b.Element("Chord")?.Value ?? string.Empty,
                    FreeText = b.Element("FreeText")?.Value ?? string.Empty,
                    WhammyBar = string.Equals(whammyBarRaw, "true", StringComparison.OrdinalIgnoreCase)
                        || whammyElement is not null,
                    WhammyBarExtended = string.Equals(whammyBarExtendRaw, "true", StringComparison.OrdinalIgnoreCase)
                        || b.Element("WhammyExtend") is not null,
                    WhammyExtendUsesElement = b.Element("WhammyExtend") is not null,
                    WhammyBarOriginValue = GetBeatPropertyFloat("WhammyBarOriginValue") ?? GetWhammyAttribute("originValue"),
                    WhammyBarMiddleValue = GetBeatPropertyFloat("WhammyBarMiddleValue") ?? GetWhammyAttribute("middleValue"),
                    WhammyBarDestinationValue = GetBeatPropertyFloat("WhammyBarDestinationValue") ?? GetWhammyAttribute("destinationValue"),
                    WhammyBarOriginOffset = GetBeatPropertyFloat("WhammyBarOriginOffset") ?? GetWhammyAttribute("originOffset"),
                    WhammyBarMiddleOffset1 = GetBeatPropertyFloat("WhammyBarMiddleOffset1") ?? GetWhammyAttribute("middleOffset1"),
                    WhammyBarMiddleOffset2 = GetBeatPropertyFloat("WhammyBarMiddleOffset2") ?? GetWhammyAttribute("middleOffset2"),
                    WhammyBarDestinationOffset = GetBeatPropertyFloat("WhammyBarDestinationOffset") ?? GetWhammyAttribute("destinationOffset"),
                    WhammyUsesElement = whammyElement is not null,
                    Properties = properties,
                    XProperties = xprops,
                    XPropertiesXml = beatXProperties?.ToString(SaveOptions.DisableFormatting) ?? string.Empty
                };
            })
            .ToDictionary(b => b.Id);

        return ValueTask.FromResult(new GpifDocument
        {
            GpVersion = root.Element("GPVersion")?.Value ?? string.Empty,
            GpRevision = new GpifRevisionInfo
            {
                Xml = root.Element("GPRevision")?.ToString(SaveOptions.DisableFormatting) ?? string.Empty,
                Required = root.Element("GPRevision")?.Attribute("required")?.Value ?? string.Empty,
                Recommended = root.Element("GPRevision")?.Attribute("recommended")?.Value ?? string.Empty,
                Value = root.Element("GPRevision")?.Value ?? string.Empty
            },
            EncodingDescription = root.Element("Encoding")?.Element("EncodingDescription")?.Value ?? string.Empty,
            Score = new ScoreInfo
            {
                Xml = score?.ToString(SaveOptions.DisableFormatting) ?? string.Empty,
                ExplicitEmptyOptionalElements = ParseExplicitEmptyOptionalScoreElements(score),
                Title = ReadDirectTextValue(score?.Element("Title")),
                SubTitle = ReadDirectTextValue(score?.Element("SubTitle")),
                Artist = ReadDirectTextValue(score?.Element("Artist")),
                Album = ReadDirectTextValue(score?.Element("Album")),
                Words = ReadDirectTextValue(score?.Element("Words")),
                Music = ReadDirectTextValue(score?.Element("Music")),
                WordsAndMusic = ReadDirectTextValue(score?.Element("WordsAndMusic")),
                Copyright = ReadDirectTextValue(score?.Element("Copyright")),
                Tabber = ReadDirectTextValue(score?.Element("Tabber")),
                Instructions = ReadDirectTextValue(score?.Element("Instructions")),
                Notices = ReadDirectTextValue(score?.Element("Notices")),
                FirstPageHeader = ReadDirectTextValue(score?.Element("FirstPageHeader")),
                FirstPageFooter = ReadDirectTextValue(score?.Element("FirstPageFooter")),
                PageHeader = ReadDirectTextValue(score?.Element("PageHeader")),
                PageFooter = ReadDirectTextValue(score?.Element("PageFooter")),
                ScoreSystemsDefaultLayout = score?.Element("ScoreSystemsDefaultLayout")?.Value ?? string.Empty,
                ScoreSystemsLayout = score?.Element("ScoreSystemsLayout")?.Value ?? string.Empty,
                ScoreZoomPolicy = score?.Element("ScoreZoomPolicy")?.Value ?? string.Empty,
                ScoreZoom = score?.Element("ScoreZoom")?.Value ?? string.Empty,
                PageSetupXml = score?.Element("PageSetup")?.ToString(SaveOptions.DisableFormatting) ?? string.Empty,
                MultiVoice = score?.Element("MultiVoice")?.Value ?? string.Empty
            },
            MasterTrack = new GpifMasterTrack
            {
                Xml = masterTrack?.ToString(SaveOptions.DisableFormatting) ?? string.Empty,
                TrackIds = SplitInts(masterTrack?.Element("Tracks")?.Value),
                AutomationsXml = masterTrack?.Element("Automations")?.ToString(SaveOptions.DisableFormatting) ?? string.Empty,
                Automations = ParseAutomations(masterTrack?.Element("Automations")),
                Anacrusis = masterTrack?.Element("Anacrusis") is not null,
                RseXml = masterTrack?.Element("RSE")?.ToString(SaveOptions.DisableFormatting) ?? string.Empty,
                Rse = ParseMasterRse(masterTrack?.Element("RSE"))
            },
            Tracks = tracks,
            MasterBars = masterBars,
            BarsById = bars,
            VoicesById = voices,
            BeatsById = beats,
            NotesById = notes,
            RhythmsById = rhythms,
            BackingTrackXml = root.Element("BackingTrack")?.ToString(SaveOptions.DisableFormatting) ?? string.Empty,
            AudioTracksXml = root.Element("AudioTracks")?.ToString(SaveOptions.DisableFormatting) ?? string.Empty,
            AssetsXml = root.Element("Assets")?.ToString(SaveOptions.DisableFormatting) ?? string.Empty,
            ScoreViewsXml = root.Element("ScoreViews")?.ToString(SaveOptions.DisableFormatting) ?? string.Empty
        });
    }

    private static int ParseInt(string? value)
        => int.TryParse(value, out var result) ? result : -1;

    private static Dictionary<string, int> ParseXPropertyInts(XElement? xPropertiesElement)
        => (xPropertiesElement?.Elements("XProperty") ?? Enumerable.Empty<XElement>())
            .Where(x => x.Attribute("id") is not null)
            .Select(x => new { Id = x.Attribute("id")!.Value, Int = TryParseXPropertyIntValue(x) })
            .Where(x => x.Int.HasValue)
            .ToDictionary(x => x.Id, x => x.Int!.Value);

    private static int? TryParseXPropertyIntValue(XElement xProperty)
    {
        var raw = xProperty.Element("Int")?.Value;
        return int.TryParse(raw, out var value) ? value : null;
    }

    private static string[] ParseExplicitEmptyOptionalScoreElements(XElement? score)
    {
        if (score is null)
        {
            return Array.Empty<string>();
        }

        return score.Elements()
            .Where(element => IsOptionalScoreElement(element.Name.LocalName) && !element.HasElements && element.Value.Length == 0)
            .Select(element => element.Name.LocalName)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static bool IsOptionalScoreElement(string elementName)
        => elementName is "SubTitle"
            or "Words"
            or "Music"
            or "WordsAndMusic"
            or "Copyright"
            or "Tabber"
            or "Instructions"
            or "Notices"
            or "FirstPageHeader"
            or "FirstPageFooter"
            or "PageHeader"
            or "PageFooter"
            or "ScoreSystemsDefaultLayout"
            or "ScoreSystemsLayout"
            or "ScoreZoomPolicy"
            or "ScoreZoom"
            or "MultiVoice";

    private static bool ParseBool(string? value)
        => bool.TryParse(value, out var result) && result;

    private static string ReadDirectTextValue(XElement? element)
    {
        if (element is null)
        {
            return string.Empty;
        }

        var cdataNodes = element.Nodes().OfType<XCData>().ToArray();
        if (cdataNodes.Length > 0)
        {
            return string.Concat(cdataNodes.Select(node => node.Value));
        }

        var textNodes = element.Nodes().OfType<XText>().Select(node => node.Value).ToArray();
        if (textNodes.Length <= 1)
        {
            return element.Value;
        }

        var nonWhitespaceText = textNodes
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToArray();

        return nonWhitespaceText.Length > 0 && nonWhitespaceText.Length < textNodes.Length
            ? string.Concat(nonWhitespaceText)
            : string.Concat(textNodes);
    }

    private static int? ParseMidiPitch(XElement note)
    {
        var pitch = note.Descendants("Pitch").FirstOrDefault();
        if (pitch is null)
        {
            return null;
        }

        return ParsePitchElementToMidi(pitch);
    }

    private static int? ParseNamedMidiPitch(XElement note, string propertyName)
    {
        var pitch = GetNamedPitchElement(note, propertyName);

        if (pitch is null)
        {
            return null;
        }

        return ParsePitchElementToMidi(pitch);
    }

    private static GpifPitchValue? ParseNamedPitchValue(XElement note, string propertyName)
    {
        var pitch = GetNamedPitchElement(note, propertyName);
        if (pitch is null)
        {
            return null;
        }

        return new GpifPitchValue
        {
            Step = pitch.Element("Step")?.Value ?? string.Empty,
            Accidental = pitch.Element("Accidental")?.Value ?? string.Empty,
            Octave = TryParseNullableInt(pitch.Element("Octave")?.Value)
        };
    }

    private static XElement? GetNamedPitchElement(XElement note, string propertyName)
        => note.Element("Properties")?
            .Elements("Property")
            .FirstOrDefault(p => string.Equals(p.Attribute("name")?.Value, propertyName, StringComparison.OrdinalIgnoreCase))
            ?.Element("Pitch");

    private static int? ParseNamedNumberProperty(XElement note, string propertyName)
        => TryParseNullableInt(
            note.Element("Properties")?
                .Elements("Property")
                .FirstOrDefault(p => string.Equals(p.Attribute("name")?.Value, propertyName, StringComparison.OrdinalIgnoreCase))
                ?.Element("Number")
                ?.Value);

    private static int? ParseNamedFretProperty(XElement note)
        => TryParseNullableInt(
            note.Element("Properties")?
                .Elements("Property")
                .FirstOrDefault(p => string.Equals(p.Attribute("name")?.Value, "Fret", StringComparison.OrdinalIgnoreCase))
                ?.Element("Fret")
                ?.Value);

    private static int? ParseNamedStringProperty(XElement note)
    {
        var property = note.Element("Properties")?
            .Elements("Property")
            .FirstOrDefault(p => string.Equals(p.Attribute("name")?.Value, "String", StringComparison.OrdinalIgnoreCase));

        return TryParseNullableInt(property?.Element("String")?.Value)
            ?? TryParseNullableInt(property?.Element("Number")?.Value);
    }

    private static int? ParsePitchElementToMidi(XElement pitch)
    {
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

    private static bool HasEnabledNoteProperty(IReadOnlyList<GpifNoteProperty> properties, string propertyName)
        => properties.Any(p => string.Equals(p.Name, propertyName, StringComparison.OrdinalIgnoreCase) && p.Enabled);

    private static GpifNoteArticulation ParseArticulation(XElement note, IReadOnlyList<GpifNoteProperty> properties)
    {
        var tie = note.Element("Tie");
        bool HasEnabledProperty(string name)
            => HasEnabledNoteProperty(properties, name);

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
            AntiAccentValue = note.Element("AntiAccent")?.Value ?? string.Empty,
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

    private static bool IsBeatBooleanTrue(string? value)
        => bool.TryParse(value, out var parsed) && parsed;

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
            LineCount = TryParseNullableInt(instrumentSet?.Element("LineCount")?.Value),
            Elements = (instrumentSet?.Element("Elements")?.Elements("Element") ?? Enumerable.Empty<XElement>())
                .Select(element => new GpifInstrumentElement
                {
                    Name = element.Element("Name")?.Value ?? string.Empty,
                    Type = element.Element("Type")?.Value ?? string.Empty,
                    SoundbankName = element.Element("SoundbankName")?.Value ?? string.Empty,
                    Articulations = (element.Element("Articulations")?.Elements("Articulation") ?? Enumerable.Empty<XElement>())
                        .Select(articulation => new GpifInstrumentArticulation
                        {
                            Name = articulation.Element("Name")?.Value ?? string.Empty,
                            StaffLine = TryParseNullableInt(articulation.Element("StaffLine")?.Value),
                            Noteheads = articulation.Element("Noteheads")?.Value ?? string.Empty,
                            TechniquePlacement = articulation.Element("TechniquePlacement")?.Value ?? string.Empty,
                            TechniqueSymbol = articulation.Element("TechniqueSymbol")?.Value ?? string.Empty,
                            InputMidiNumbers = articulation.Element("InputMidiNumbers")?.Value ?? string.Empty,
                            OutputRseSound = articulation.Element("OutputRSESound")?.Value ?? string.Empty,
                            OutputMidiNumber = TryParseNullableInt(articulation.Element("OutputMidiNumber")?.Value)
                        })
                        .ToArray()
                })
                .ToArray()
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
                MidiProgram = TryParseNullableInt(s.Element("MIDI")?.Element("Program")?.Value),
                Rse = ParseSoundRse(s.Element("RSE"))
            })
            .ToArray();

    private static GpifSoundRse ParseSoundRse(XElement? rse)
        => new()
        {
            SoundbankPatch = rse?.Element("SoundbankPatch")?.Value ?? string.Empty,
            SoundbankSet = rse?.Element("SoundbankSet")?.Value ?? string.Empty,
            ElementsSettingsXml = rse?.Element("ElementsSettings")?.ToString(SaveOptions.DisableFormatting) ?? string.Empty,
            Pickups = new GpifSoundRsePickups
            {
                OverloudPosition = rse?.Element("Pickups")?.Element("OverloudPosition")?.Value ?? string.Empty,
                Volumes = rse?.Element("Pickups")?.Element("Volumes")?.Value ?? string.Empty,
                Tones = rse?.Element("Pickups")?.Element("Tones")?.Value ?? string.Empty
            },
            EffectChain = ParseRseEffects(rse?.Element("EffectChain")?.Elements("Effect"))
        };

    private static GpifRse ParseRse(XElement? rse)
        => new()
        {
            Bank = rse?.Element("Bank")?.Value ?? string.Empty,
            ChannelStripVersion = rse?.Element("ChannelStrip")?.Attribute("version")?.Value ?? string.Empty,
            ChannelStripParameters = rse?.Element("ChannelStrip")?.Element("Parameters")?.Value ?? string.Empty,
            Automations = ParseAutomations(rse?.Element("ChannelStrip")?.Element("Automations"))
        };

    private static GpifMasterRse ParseMasterRse(XElement? rse)
        => new()
        {
            MasterEffects = ParseRseEffects(rse?.Element("Master")?.Elements("Effect"))
        };

    private static GpifRseEffect[] ParseRseEffects(IEnumerable<XElement>? effects)
        => (effects ?? Enumerable.Empty<XElement>())
            .Select(effect => new GpifRseEffect
            {
                Id = effect.Attribute("id")?.Value ?? string.Empty,
                Bypass = effect.Element("ByPass") is not null || effect.Element("Bypass") is not null,
                Parameters = effect.Element("Parameters")?.Value ?? string.Empty
            })
            .ToArray();

    private static GpifAudioEngineState ParseAudioEngineState(XElement? audioEngineState)
        => new()
        {
            Value = audioEngineState?.Value?.Trim() ?? string.Empty
        };

    private static GpifPlaybackState ParsePlaybackState(XElement? playbackState)
        => new()
        {
            Value = playbackState?.Value?.Trim() ?? string.Empty
        };

    private static GpifMidiConnection ParseMidiConnection(XElement? midiConnection)
        => new()
        {
            Port = TryParseNullableInt(midiConnection?.Element("Port")?.Value),
            PrimaryChannel = TryParseNullableInt(midiConnection?.Element("PrimaryChannel")?.Value),
            SecondaryChannel = TryParseNullableInt(midiConnection?.Element("SecondaryChannel")?.Value),
            ForceOneChannelPerString = TryParseNullableBool(
                midiConnection?.Element("ForeOneChannelPerString")?.Value
                ?? midiConnection?.Element("ForceOneChannelPerString")?.Value)
        };

    private static GpifLyrics ParseLyrics(XElement? lyrics)
        => new()
        {
            Dispatched = TryParseNullableBool(lyrics?.Attribute("dispatched")?.Value),
            Lines = (lyrics?.Elements("Line") ?? Enumerable.Empty<XElement>())
                .Select(line => new GpifLyricsLine
                {
                    Text = line.Element("Text")?.Value ?? string.Empty,
                    Offset = TryParseNullableInt(line.Element("Offset")?.Value)
                })
                .ToArray()
        };

    private static GpifTranspose ParseTranspose(XElement? transpose)
        => new()
        {
            Chromatic = TryParseNullableInt(transpose?.Element("Chromatic")?.Value),
            Octave = TryParseNullableInt(transpose?.Element("Octave")?.Value)
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
            ?? property.Element("String")?.Value
            ?? property.Element("Rasgueado")?.Value;

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

        var numerator = ParseInt(tuplet.Attribute("num")?.Value ?? tuplet.Element("Num")?.Value);
        var denominator = ParseInt(tuplet.Attribute("den")?.Value ?? tuplet.Element("Den")?.Value);
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
