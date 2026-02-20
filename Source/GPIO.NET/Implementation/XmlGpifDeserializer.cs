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
            .Select((mb, index) => new GpifMasterBar
            {
                Index = index,
                Time = mb.Element("Time")?.Value ?? string.Empty,
                BarsReferenceList = mb.Element("Bars")?.Value ?? string.Empty
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
            .Select(r => new GpifRhythm
            {
                Id = ParseInt(r.Attribute("id")?.Value),
                NoteValue = r.Element("NoteValue")?.Value ?? string.Empty
            })
            .ToDictionary(r => r.Id);

        var notes = (root.Element("Notes")?.Elements("Note") ?? Enumerable.Empty<XElement>())
            .Select(n => new GpifNote
            {
                Id = ParseInt(n.Attribute("id")?.Value),
                MidiPitch = ParseMidiPitch(n)
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
}
