namespace GPIO.NET.Implementation;

using GPIO.NET.Abstractions;
using GPIO.NET.Models.Raw;
using System.Xml;
using System.Xml.Linq;

public sealed class XmlGpifSerializer : IGpifSerializer
{
    public async ValueTask SerializeAsync(GpifDocument document, Stream output, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        cancellationToken.ThrowIfCancellationRequested();

        var x = new XDocument(
            new XElement("GPIF",
                BuildScore(document.Score),
                new XElement("Tracks", document.Tracks.OrderBy(t => t.Id).Select(BuildTrack)),
                new XElement("MasterBars", document.MasterBars.OrderBy(m => m.Index).Select(BuildMasterBar)),
                new XElement("Bars", document.BarsById.OrderBy(kv => kv.Key).Select(kv => BuildBar(kv.Value))),
                new XElement("Voices", document.VoicesById.OrderBy(kv => kv.Key).Select(kv => BuildVoice(kv.Value))),
                new XElement("Rhythms", document.RhythmsById.OrderBy(kv => kv.Key).Select(kv => BuildRhythm(kv.Value))),
                new XElement("Beats", document.BeatsById.OrderBy(kv => kv.Key).Select(kv => BuildBeat(kv.Value))),
                new XElement("Notes", document.NotesById.OrderBy(kv => kv.Key).Select(kv => BuildNote(kv.Value)))
            ));

        var settings = new XmlWriterSettings { Async = true, Indent = true, OmitXmlDeclaration = true };
        using var writer = XmlWriter.Create(output, settings);
        x.WriteTo(writer);
        await writer.FlushAsync();
    }

    private static XElement BuildScore(ScoreInfo s) => new("Score",
        new XElement("Title", s.Title),
        new XElement("Artist", s.Artist),
        new XElement("Album", s.Album));

    private static XElement BuildTrack(GpifTrack t) => new("Track",
        new XAttribute("id", t.Id),
        new XElement("Name", t.Name));

    private static XElement BuildMasterBar(GpifMasterBar m)
    {
        var el = new XElement("MasterBar",
            new XElement("Time", m.Time),
            new XElement("Bars", m.BarsReferenceList ?? string.Empty));

        if (!string.IsNullOrWhiteSpace(m.AlternateEndings)) el.Add(new XElement("AlternateEndings", m.AlternateEndings));
        if (m.RepeatStart || m.RepeatEnd || m.RepeatCount > 0)
        {
            var repeat = new XElement("Repeat");
            if (m.RepeatStart) repeat.SetAttributeValue("start", "true");
            if (m.RepeatEnd) repeat.SetAttributeValue("end", "true");
            if (m.RepeatCount > 0) repeat.SetAttributeValue("count", m.RepeatCount);
            el.Add(repeat);
        }

        if (!string.IsNullOrWhiteSpace(m.SectionLetter) || !string.IsNullOrWhiteSpace(m.SectionText))
        {
            el.Add(new XElement("Section", new XElement("Letter", m.SectionLetter), new XElement("Text", m.SectionText)));
        }

        if (!string.IsNullOrWhiteSpace(m.Jump) || !string.IsNullOrWhiteSpace(m.Target))
        {
            var dirs = new XElement("Directions");
            if (!string.IsNullOrWhiteSpace(m.Jump)) dirs.Add(new XElement("Jump", m.Jump));
            if (!string.IsNullOrWhiteSpace(m.Target)) dirs.Add(new XElement("Target", m.Target));
            el.Add(dirs);
        }

        return el;
    }

    private static XElement BuildBar(GpifBar b) => new("Bar", new XAttribute("id", b.Id), new XElement("Voices", b.VoicesReferenceList));
    private static XElement BuildVoice(GpifVoice v) => new("Voice", new XAttribute("id", v.Id), new XElement("Beats", v.BeatsReferenceList));
    private static XElement BuildRhythm(GpifRhythm r)
    {
        var el = new XElement("Rhythm", new XAttribute("id", r.Id), new XElement("NoteValue", r.NoteValue));
        for (var i = 0; i < r.AugmentationDots; i++) el.Add(new XElement("AugmentationDot"));
        if (r.PrimaryTuplet is not null) el.Add(new XElement("PrimaryTuplet", new XElement("Num", r.PrimaryTuplet.Numerator), new XElement("Den", r.PrimaryTuplet.Denominator)));
        if (r.SecondaryTuplet is not null) el.Add(new XElement("SecondaryTuplet", new XElement("Num", r.SecondaryTuplet.Numerator), new XElement("Den", r.SecondaryTuplet.Denominator)));
        return el;
    }
    private static XElement BuildBeat(GpifBeat b)
    {
        var el = new XElement("Beat", new XAttribute("id", b.Id), new XElement("Rhythm", new XAttribute("ref", b.RhythmRef)));
        if (!string.IsNullOrWhiteSpace(b.NotesReferenceList)) el.Add(new XElement("Notes", b.NotesReferenceList));
        return el;
    }

    private static XElement BuildNote(GpifNote n)
    {
        var el = new XElement("Note", new XAttribute("id", n.Id));
        if (n.Articulation.Accent.HasValue) el.Add(new XElement("Accent", n.Articulation.Accent.Value));
        if (n.Articulation.AntiAccent) el.Add(new XElement("AntiAccent"));
        if (n.Articulation.InstrumentArticulation.HasValue) el.Add(new XElement("InstrumentArticulation", n.Articulation.InstrumentArticulation.Value));
        if (n.Articulation.LetRing) el.Add(new XElement("LetRing"));
        if (n.Articulation.TieOrigin || n.Articulation.TieDestination)
            el.Add(new XElement("Tie", new XAttribute("origin", n.Articulation.TieOrigin.ToString().ToLowerInvariant()), new XAttribute("destination", n.Articulation.TieDestination.ToString().ToLowerInvariant())));
        if (n.Articulation.Trill.HasValue) el.Add(new XElement("Trill", n.Articulation.Trill.Value));
        if (!string.IsNullOrWhiteSpace(n.Articulation.Vibrato)) el.Add(new XElement("Vibrato", n.Articulation.Vibrato));

        var props = new XElement("Properties");
        if (n.MidiPitch.HasValue)
        {
            var (step, accidental, octave) = FromMidi(n.MidiPitch.Value);
            props.Add(new XElement("Property", new XAttribute("name", "Pitch"), new XElement("Pitch", new XElement("Step", step), new XElement("Accidental", accidental), new XElement("Octave", octave))));
        }

        AddBoolProperty(props, "PalmMuted", n.Articulation.PalmMuted);
        AddBoolProperty(props, "Muted", n.Articulation.Muted);
        AddBoolProperty(props, "Tapped", n.Articulation.Tapped);
        AddBoolProperty(props, "LeftHandTapped", n.Articulation.LeftHandTapped);
        AddBoolProperty(props, "HopoOrigin", n.Articulation.HopoOrigin);
        AddBoolProperty(props, "HopoDestination", n.Articulation.HopoDestination);

        if (n.Articulation.SlideFlags.HasValue)
            props.Add(new XElement("Property", new XAttribute("name", "Slide"), new XElement("Flags", n.Articulation.SlideFlags.Value)));

        if (n.Articulation.HarmonicEnabled) AddBoolProperty(props, "Harmonic", true);
        AddNumberProperty(props, "HarmonicType", n.Articulation.HarmonicType);
        AddDecimalProperty(props, "HarmonicFret", n.Articulation.HarmonicFret);

        if (n.Articulation.BendEnabled) AddBoolProperty(props, "Bended", true);
        AddDecimalProperty(props, "BendOriginOffset", n.Articulation.BendOriginOffset);
        AddDecimalProperty(props, "BendOriginValue", n.Articulation.BendOriginValue);
        AddDecimalProperty(props, "BendMiddleOffset1", n.Articulation.BendMiddleOffset1);
        AddDecimalProperty(props, "BendMiddleOffset2", n.Articulation.BendMiddleOffset2);
        AddDecimalProperty(props, "BendMiddleValue", n.Articulation.BendMiddleValue);
        AddDecimalProperty(props, "BendDestinationOffset", n.Articulation.BendDestinationOffset);
        AddDecimalProperty(props, "BendDestinationValue", n.Articulation.BendDestinationValue);

        if (props.HasElements) el.Add(props);
        return el;
    }

    private static void AddBoolProperty(XElement parent, string name, bool value)
    {
        if (!value) return;
        parent.Add(new XElement("Property", new XAttribute("name", name), new XElement("Enable")));
    }

    private static void AddNumberProperty(XElement parent, string name, int? value)
    {
        if (!value.HasValue) return;
        parent.Add(new XElement("Property", new XAttribute("name", name), new XElement("Number", value.Value)));
    }

    private static void AddDecimalProperty(XElement parent, string name, decimal? value)
    {
        if (!value.HasValue) return;
        parent.Add(new XElement("Property", new XAttribute("name", name), new XElement("Float", value.Value)));
    }

    private static (string step, string accidental, int octave) FromMidi(int midi)
    {
        var octave = midi / 12;
        var cls = midi % 12;
        return cls switch
        {
            0 => ("C", "", octave),
            1 => ("C", "#", octave),
            2 => ("D", "", octave),
            3 => ("D", "#", octave),
            4 => ("E", "", octave),
            5 => ("F", "", octave),
            6 => ("F", "#", octave),
            7 => ("G", "", octave),
            8 => ("G", "#", octave),
            9 => ("A", "", octave),
            10 => ("A", "#", octave),
            11 => ("B", "", octave),
            _ => ("C", "", octave)
        };
    }
}
