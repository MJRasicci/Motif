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

        var tracks = root.Element("Tracks")?.Elements("Track")
            .Select(t => new GpifTrack
            {
                Id = ParseInt(t.Attribute("id")?.Value),
                Name = t.Element("Name")?.Value ?? string.Empty
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
            .Select(n => new GpifNote
            {
                Id = ParseInt(n.Attribute("id")?.Value),
                MidiPitch = ParseMidiPitch(n),
                Articulation = ParseArticulation(n)
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
                Artist = score?.Element("Artist")?.Value ?? string.Empty,
                Album = score?.Element("Album")?.Value ?? string.Empty
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

    private static GpifNoteArticulation ParseArticulation(XElement note)
    {
        var tie = note.Element("Tie");
        return new GpifNoteArticulation
        {
            LetRing = note.Element("LetRing") is not null,
            Vibrato = note.Element("Vibrato")?.Value ?? string.Empty,
            TieOrigin = ParseBool(tie?.Attribute("origin")?.Value),
            TieDestination = ParseBool(tie?.Attribute("destination")?.Value),
            Trill = TryParseNullableInt(note.Element("Trill")?.Value),
            Accent = TryParseNullableInt(note.Element("Accent")?.Value),
            AntiAccent = note.Element("AntiAccent") is not null,
            InstrumentArticulation = TryParseNullableInt(note.Element("InstrumentArticulation")?.Value)
        };
    }

    private static int? TryParseNullableInt(string? value)
        => int.TryParse(value, out var parsed) ? parsed : null;

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
