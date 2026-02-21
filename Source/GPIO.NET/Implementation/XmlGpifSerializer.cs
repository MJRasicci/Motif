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

    private static XElement BuildScore(ScoreInfo s)
    {
        var el = new XElement("Score",
            new XElement("Title", s.Title),
            new XElement("Artist", s.Artist),
            new XElement("Album", s.Album));

        AddTextElement(el, "SubTitle", s.SubTitle);
        AddTextElement(el, "Words", s.Words);
        AddTextElement(el, "Music", s.Music);
        AddTextElement(el, "WordsAndMusic", s.WordsAndMusic);
        AddTextElement(el, "Copyright", s.Copyright);
        AddTextElement(el, "Tabber", s.Tabber);
        AddTextElement(el, "Instructions", s.Instructions);
        AddTextElement(el, "Notices", s.Notices);
        AddTextElement(el, "FirstPageHeader", s.FirstPageHeader);
        AddTextElement(el, "FirstPageFooter", s.FirstPageFooter);
        AddTextElement(el, "PageHeader", s.PageHeader);
        AddTextElement(el, "PageFooter", s.PageFooter);
        AddTextElement(el, "ScoreSystemsDefaultLayout", s.ScoreSystemsDefaultLayout);
        AddTextElement(el, "ScoreSystemsLayout", s.ScoreSystemsLayout);
        AddTextElement(el, "ScoreZoomPolicy", s.ScoreZoomPolicy);
        AddTextElement(el, "ScoreZoom", s.ScoreZoom);
        AddTextElement(el, "MultiVoice", s.MultiVoice);

        return el;
    }

    private static XElement BuildTrack(GpifTrack t)
    {
        var el = new XElement("Track",
            new XAttribute("id", t.Id),
            new XElement("Name", t.Name));

        AddTextElement(el, "ShortName", t.ShortName);
        AddTextElement(el, "Color", t.Color);
        AddTextElement(el, "SystemsDefautLayout", t.SystemsDefaultLayout);
        AddTextElement(el, "SystemsLayout", t.SystemsLayout);
        if (t.AutoBrush) el.Add(new XElement("AutoBrush"));
        if (t.PalmMute.HasValue) el.Add(new XElement("PalmMute", t.PalmMute.Value));
        if (t.AutoAccentuation.HasValue) el.Add(new XElement("AutoAccentuation", t.AutoAccentuation.Value));
        AddTextElement(el, "PlayingStyle", t.PlayingStyle);
        if (t.UseOneChannelPerString) el.Add(new XElement("UseOneChannelPerString"));
        if (t.IconId.HasValue) el.Add(new XElement("IconId", t.IconId.Value));
        if (t.ForcedSound.HasValue) el.Add(new XElement("ForcedSound", t.ForcedSound.Value));

        if (t.TuningPitches.Length > 0)
        {
            var props = new XElement("Properties",
                new XElement("Property",
                    new XAttribute("name", "Tuning"),
                    new XElement("Pitches", string.Join(' ', t.TuningPitches)),
                    new XElement("Instrument", t.TuningInstrument ?? string.Empty),
                    new XElement("Label", t.TuningLabel ?? string.Empty),
                    t.TuningLabelVisible.HasValue ? new XElement("LabelVisible", t.TuningLabelVisible.Value.ToString().ToLowerInvariant()) : null));
            el.Add(props);
        }

        AddRawElementXml(el, t.InstrumentSetXml);
        if (!string.IsNullOrWhiteSpace(t.StavesXml))
        {
            AddRawElementXml(el, t.StavesXml);
        }
        else if (t.Staffs.Count > 0)
        {
            el.Add(BuildStaves(t.Staffs));
        }
        AddRawElementXml(el, t.SoundsXml);
        AddRawElementXml(el, t.RseXml);
        AddRawElementXml(el, t.PlaybackStateXml);
        AddRawElementXml(el, t.AudioEngineStateXml);
        AddRawElementXml(el, t.MidiConnectionXml);
        AddRawElementXml(el, t.LyricsXml);
        AddRawElementXml(el, t.AutomationsXml);
        AddRawElementXml(el, t.TransposeXml);

        return el;
    }

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

    private static XElement BuildStaves(IReadOnlyList<GpifStaff> staffs)
    {
        var root = new XElement("Staves");
        foreach (var s in staffs)
        {
            if (!string.IsNullOrWhiteSpace(s.Xml))
            {
                try
                {
                    root.Add(XElement.Parse(s.Xml));
                    continue;
                }
                catch
                {
                    // fallback to generated staff element
                }
            }

            var staff = new XElement("Staff");
            if (s.Id.HasValue) staff.SetAttributeValue("id", s.Id.Value);
            if (!string.IsNullOrWhiteSpace(s.Cref)) staff.SetAttributeValue("cref", s.Cref);

            var props = new XElement("Properties");
            foreach (var kv in s.Properties)
            {
                props.Add(new XElement("Property", new XAttribute("name", kv.Key), new XElement("Value", kv.Value)));
            }

            if (s.TuningPitches.Length > 0)
            {
                props.Add(new XElement("Property", new XAttribute("name", "Tuning"), new XElement("Value", string.Join(' ', s.TuningPitches))));
            }

            if (s.CapoFret.HasValue)
            {
                props.Add(new XElement("Property", new XAttribute("name", "CapoFret"), new XElement("Value", s.CapoFret.Value)));
            }

            if (props.HasElements)
            {
                staff.Add(props);
            }

            root.Add(staff);
        }

        return root;
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

    private static void AddTextElement(XElement parent, string name, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        parent.Add(new XElement(name, value));
    }

    private static void AddRawElementXml(XElement parent, string xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
        {
            return;
        }

        try
        {
            parent.Add(XElement.Parse(xml));
        }
        catch
        {
            // ignore malformed passthrough chunks
        }
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
