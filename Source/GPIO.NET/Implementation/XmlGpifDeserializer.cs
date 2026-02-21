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

        var tracksContainer = root.Elements("Tracks").FirstOrDefault(t => t.Elements("Track").Any());
        var tracks = tracksContainer?.Elements("Track")
            .Select(t =>
            {
                var properties = (t.Element("Properties")?.Elements("Property") ?? Enumerable.Empty<XElement>())
                    .Where(p => p.Attribute("name") is not null)
                    .ToDictionary(
                        p => p.Attribute("name")!.Value,
                        p => p.Value?.Trim() ?? string.Empty,
                        StringComparer.OrdinalIgnoreCase);

                properties.TryGetValue("Tuning", out var tuningPitchesRaw);
                properties.TryGetValue("Instrument", out var tuningInstrument);
                properties.TryGetValue("Label", out var tuningLabel);
                properties.TryGetValue("LabelVisible", out var tuningLabelVisibleRaw);

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
                    TuningPitches = SplitInts(tuningPitchesRaw),
                    TuningInstrument = tuningInstrument ?? string.Empty,
                    TuningLabel = tuningLabel ?? string.Empty,
                    TuningLabelVisible = TryParseNullableBool(tuningLabelVisibleRaw),
                    Properties = properties,
                    InstrumentSetXml = t.Element("InstrumentSet")?.ToString(SaveOptions.DisableFormatting) ?? string.Empty,
                    StavesXml = t.Element("Staves")?.ToString(SaveOptions.DisableFormatting) ?? string.Empty,
                    SoundsXml = t.Element("Sounds")?.ToString(SaveOptions.DisableFormatting) ?? string.Empty,
                    RseXml = t.Element("RSE")?.ToString(SaveOptions.DisableFormatting) ?? string.Empty,
                    PlaybackStateXml = t.Element("PlaybackState")?.ToString(SaveOptions.DisableFormatting) ?? string.Empty,
                    AudioEngineStateXml = t.Element("AudioEngineState")?.ToString(SaveOptions.DisableFormatting) ?? string.Empty,
                    MidiConnectionXml = t.Element("MidiConnection")?.ToString(SaveOptions.DisableFormatting) ?? string.Empty,
                    LyricsXml = t.Element("Lyrics")?.ToString(SaveOptions.DisableFormatting) ?? string.Empty,
                    AutomationsXml = t.Element("Automations")?.ToString(SaveOptions.DisableFormatting) ?? string.Empty,
                    TransposeXml = t.Element("Transpose")?.ToString(SaveOptions.DisableFormatting) ?? string.Empty
                };
            })
            .ToArray() ?? Array.Empty<GpifTrack>();

        var masterBars = (root.Element("MasterBars")?.Elements("MasterBar") ?? Enumerable.Empty<XElement>())
            .Select((mb, index) =>
            {
                var repeat = mb.Element("Repeat");
                var section = mb.Element("Section");
                var directions = mb.Element("Directions");

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
                    Target = directions?.Element("Target")?.Value ?? string.Empty
                };
            })
            .ToArray();

        var bars = (root.Element("Bars")?.Elements("Bar") ?? Enumerable.Empty<XElement>())
            .Select(b => new GpifBar
            {
                Id = ParseInt(b.Attribute("id")?.Value),
                VoicesReferenceList = b.Element("Voices")?.Value ?? string.Empty
            })
            .ToDictionary(b => b.Id);

        var voices = (root.Element("Voices")?.Elements("Voice") ?? Enumerable.Empty<XElement>())
            .Select(v => new GpifVoice
            {
                Id = ParseInt(v.Attribute("id")?.Value),
                BeatsReferenceList = v.Element("Beats")?.Value ?? string.Empty
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
            .Select(b => new GpifBeat
            {
                Id = ParseInt(b.Attribute("id")?.Value),
                RhythmRef = ParseInt(b.Element("Rhythm")?.Attribute("ref")?.Value),
                NotesReferenceList = b.Element("Notes")?.Value ?? string.Empty
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

        decimal? GetPropertyDecimal(string name)
        {
            var prop = properties.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
            if (prop is null)
            {
                return null;
            }

            if (prop.Float.HasValue)
            {
                return prop.Float;
            }

            return prop.Number;
        }

        return new GpifNoteArticulation
        {
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
