namespace GPIO.NET.Implementation;

using GPIO.NET.Abstractions;
using GPIO.NET.Models.Raw;
using System.Text;
using System.Xml;
using System.Xml.Linq;

public sealed class XmlGpifSerializer : IGpifSerializer
{
    private const string DefaultGpVersion = "8.1.0";
    private const string DefaultGpRevisionRequired = "12024";
    private const string DefaultGpRevisionRecommended = "13000";
    private const string DefaultGpRevisionValue = "13006";
    private const string DefaultEncodingDescription = "GP8";

    public async ValueTask SerializeAsync(GpifDocument document, Stream output, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        cancellationToken.ThrowIfCancellationRequested();

        var root = new XElement("GPIF",
            new XElement("GPVersion", ResolveOrDefault(document.GpVersion, DefaultGpVersion)),
            BuildGpRevision(document.GpRevision),
            new XElement("Encoding", new XElement("EncodingDescription", ResolveOrDefault(document.EncodingDescription, DefaultEncodingDescription))),
            BuildScore(document.Score),
            BuildMasterTrack(document.MasterTrack),
            new XElement("Tracks", document.Tracks.OrderBy(t => t.Id).Select(BuildTrack)),
            new XElement("MasterBars", document.MasterBars.OrderBy(m => m.Index).Select(BuildMasterBar)),
            new XElement("Bars", document.BarsById.OrderBy(kv => kv.Key).Select(kv => BuildBar(kv.Value))),
            new XElement("Voices", document.VoicesById.OrderBy(kv => kv.Key).Select(kv => BuildVoice(kv.Value))),
            new XElement("Beats", document.BeatsById.OrderBy(kv => kv.Key).Select(kv => BuildBeat(kv.Value))),
            new XElement("Notes", document.NotesById.OrderBy(kv => kv.Key).Select(kv => BuildNote(kv.Value))),
            new XElement("Rhythms", document.RhythmsById.OrderBy(kv => kv.Key).Select(kv => BuildRhythm(kv.Value))));

        AddRawElementXml(root, document.ScoreViewsXml);

        var x = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            root);

        var settings = new XmlWriterSettings
        {
            Async = true,
            Indent = true,
            OmitXmlDeclaration = false,
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
        };
        using var writer = XmlWriter.Create(output, settings);
        x.WriteTo(writer);
        await writer.FlushAsync();
    }

    private static XElement BuildGpRevision(GpifRevisionInfo revision)
        => new(
            "GPRevision",
            new XAttribute("required", ResolveOrDefault(revision.Required, DefaultGpRevisionRequired)),
            new XAttribute("recommended", ResolveOrDefault(revision.Recommended, DefaultGpRevisionRecommended)),
            ResolveOrDefault(revision.Value, DefaultGpRevisionValue));

    private static string ResolveOrDefault(string value, string fallback)
        => string.IsNullOrWhiteSpace(value) ? fallback : value;

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

    private static XElement BuildMasterTrack(GpifMasterTrack master)
    {
        var el = new XElement("MasterTrack",
            new XElement("Tracks", string.Join(' ', master.TrackIds)));

        if (!string.IsNullOrWhiteSpace(master.RseXml))
        {
            AddRawElementXml(el, master.RseXml);
        }
        else if (master.Rse.MasterEffects.Count > 0)
        {
            el.Add(BuildMasterRse(master.Rse));
        }

        if (master.Anacrusis)
        {
            el.Add(new XElement("Anacrusis"));
        }

        if (master.Automations.Count > 0)
        {
            el.Add(BuildAutomations(master.Automations));
        }

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

        if (!string.IsNullOrWhiteSpace(t.InstrumentSetXml))
        {
            AddRawElementXml(el, t.InstrumentSetXml);
        }
        else if (!string.IsNullOrWhiteSpace(t.InstrumentSet.Name) || !string.IsNullOrWhiteSpace(t.InstrumentSet.Type) || t.InstrumentSet.LineCount.HasValue)
        {
            el.Add(BuildInstrumentSet(t.InstrumentSet));
        }

        if (!string.IsNullOrWhiteSpace(t.StavesXml))
        {
            AddRawElementXml(el, t.StavesXml);
        }
        else if (t.Staffs.Count > 0)
        {
            el.Add(BuildStaves(t.Staffs));
        }

        if (!string.IsNullOrWhiteSpace(t.SoundsXml))
        {
            AddRawElementXml(el, t.SoundsXml);
        }
        else if (t.Sounds.Count > 0)
        {
            el.Add(BuildSounds(t.Sounds));
        }

        if (!string.IsNullOrWhiteSpace(t.RseXml))
        {
            AddRawElementXml(el, t.RseXml);
        }
        else if (!string.IsNullOrWhiteSpace(t.ChannelRse.Bank)
            || !string.IsNullOrWhiteSpace(t.ChannelRse.ChannelStripVersion)
            || !string.IsNullOrWhiteSpace(t.ChannelRse.ChannelStripParameters)
            || t.ChannelRse.Automations.Count > 0)
        {
            el.Add(BuildRse(t.ChannelRse));
        }
        if (!string.IsNullOrWhiteSpace(t.PlaybackStateXml))
        {
            AddRawElementXml(el, t.PlaybackStateXml);
        }
        else if (!string.IsNullOrWhiteSpace(t.PlaybackState.Value))
        {
            el.Add(new XElement("PlaybackState", t.PlaybackState.Value));
        }

        if (!string.IsNullOrWhiteSpace(t.AudioEngineStateXml))
        {
            AddRawElementXml(el, t.AudioEngineStateXml);
        }
        else if (!string.IsNullOrWhiteSpace(t.AudioEngineState.Value))
        {
            el.Add(new XElement("AudioEngineState", t.AudioEngineState.Value));
        }

        if (!string.IsNullOrWhiteSpace(t.MidiConnectionXml))
        {
            AddRawElementXml(el, t.MidiConnectionXml);
        }
        else if (t.MidiConnection.Port.HasValue
            || t.MidiConnection.PrimaryChannel.HasValue
            || t.MidiConnection.SecondaryChannel.HasValue
            || t.MidiConnection.ForceOneChannelPerString.HasValue)
        {
            el.Add(BuildMidiConnection(t.MidiConnection));
        }

        if (!string.IsNullOrWhiteSpace(t.LyricsXml))
        {
            AddRawElementXml(el, t.LyricsXml);
        }
        else if (t.Lyrics.Dispatched.HasValue || t.Lyrics.Lines.Count > 0)
        {
            el.Add(BuildLyrics(t.Lyrics));
        }

        if (!string.IsNullOrWhiteSpace(t.AutomationsXml))
        {
            AddRawElementXml(el, t.AutomationsXml);
        }
        else if (t.Automations.Count > 0)
        {
            el.Add(BuildAutomations(t.Automations));
        }

        if (!string.IsNullOrWhiteSpace(t.TransposeXml))
        {
            AddRawElementXml(el, t.TransposeXml);
        }
        else if (t.Transpose.Chromatic.HasValue || t.Transpose.Octave.HasValue)
        {
            el.Add(BuildTranspose(t.Transpose));
        }

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
            el.Add(new XElement(
                "Section",
                new XElement("Letter", new XCData(m.SectionLetter)),
                new XElement("Text", new XCData(m.SectionText))));
        }

        if (!string.IsNullOrWhiteSpace(m.Jump) || !string.IsNullOrWhiteSpace(m.Target) || m.DirectionProperties.Count > 0)
        {
            var dirs = new XElement("Directions");
            if (!string.IsNullOrWhiteSpace(m.Jump)) dirs.Add(new XElement("Jump", m.Jump));
            if (!string.IsNullOrWhiteSpace(m.Target)) dirs.Add(new XElement("Target", m.Target));
            foreach (var kv in m.DirectionProperties)
            {
                if (string.Equals(kv.Key, "Jump", StringComparison.OrdinalIgnoreCase) || string.Equals(kv.Key, "Target", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                dirs.Add(new XElement(kv.Key, kv.Value));
            }
            el.Add(dirs);
        }

        if (m.KeyAccidentalCount.HasValue || !string.IsNullOrWhiteSpace(m.KeyMode) || !string.IsNullOrWhiteSpace(m.KeyTransposeAs))
        {
            el.Add(new XElement("Key",
                m.KeyAccidentalCount.HasValue ? new XElement("AccidentalCount", m.KeyAccidentalCount.Value) : null,
                !string.IsNullOrWhiteSpace(m.KeyMode) ? new XElement("Mode", m.KeyMode) : null,
                !string.IsNullOrWhiteSpace(m.KeyTransposeAs) ? new XElement("TransposeAs", m.KeyTransposeAs) : null));
        }

        if (m.Fermatas.Count > 0)
        {
            el.Add(new XElement("Fermatas", m.Fermatas.Select(f => new XElement("Fermata",
                !string.IsNullOrWhiteSpace(f.Type) ? new XElement("Type", f.Type) : null,
                !string.IsNullOrWhiteSpace(f.Offset) ? new XElement("Offset", f.Offset) : null,
                f.Length.HasValue ? new XElement("Length", f.Length.Value) : null))));
        }

        if (m.XProperties.Count > 0)
        {
            el.Add(new XElement("XProperties", m.XProperties.Select(kv =>
                new XElement("XProperty", new XAttribute("id", kv.Key), new XElement("Int", kv.Value)))));
        }

        return el;
    }

    private static XElement BuildInstrumentSet(GpifInstrumentSet set)
    {
        var el = new XElement("InstrumentSet");
        AddTextElement(el, "Name", set.Name);
        AddTextElement(el, "Type", set.Type);
        if (set.LineCount.HasValue)
        {
            el.Add(new XElement("LineCount", set.LineCount.Value));
        }

        if (set.Elements.Count > 0)
        {
            var elements = new XElement("Elements");
            foreach (var element in set.Elements)
            {
                var elementNode = new XElement("Element");
                AddTextElement(elementNode, "Name", element.Name);
                AddTextElement(elementNode, "Type", element.Type);
                AddTextElement(elementNode, "SoundbankName", element.SoundbankName);

                if (element.Articulations.Count > 0)
                {
                    var articulations = new XElement("Articulations");
                    foreach (var articulation in element.Articulations)
                    {
                        var articulationNode = new XElement("Articulation");
                        AddTextElement(articulationNode, "Name", articulation.Name);
                        if (articulation.StaffLine.HasValue)
                        {
                            articulationNode.Add(new XElement("StaffLine", articulation.StaffLine.Value));
                        }

                        AddTextElement(articulationNode, "Noteheads", articulation.Noteheads);
                        AddTextElement(articulationNode, "TechniquePlacement", articulation.TechniquePlacement);
                        AddTextElement(articulationNode, "TechniqueSymbol", articulation.TechniqueSymbol);
                        AddTextElement(articulationNode, "InputMidiNumbers", articulation.InputMidiNumbers);
                        AddTextElement(articulationNode, "OutputRSESound", articulation.OutputRseSound);
                        if (articulation.OutputMidiNumber.HasValue)
                        {
                            articulationNode.Add(new XElement("OutputMidiNumber", articulation.OutputMidiNumber.Value));
                        }

                        articulations.Add(articulationNode);
                    }

                    elementNode.Add(articulations);
                }

                elements.Add(elementNode);
            }

            el.Add(elements);
        }

        return el;
    }

    private static XElement BuildSounds(IReadOnlyList<GpifSound> sounds)
    {
        var root = new XElement("Sounds");
        foreach (var s in sounds)
        {
            var sound = new XElement("Sound");
            AddTextElement(sound, "Name", s.Name);
            AddTextElement(sound, "Label", s.Label);
            AddTextElement(sound, "Path", s.Path);
            AddTextElement(sound, "Role", s.Role);
            if (s.MidiLsb.HasValue || s.MidiMsb.HasValue || s.MidiProgram.HasValue)
            {
                sound.Add(new XElement("MIDI",
                    s.MidiLsb.HasValue ? new XElement("LSB", s.MidiLsb.Value) : null,
                    s.MidiMsb.HasValue ? new XElement("MSB", s.MidiMsb.Value) : null,
                    s.MidiProgram.HasValue ? new XElement("Program", s.MidiProgram.Value) : null));
            }

            if (!string.IsNullOrWhiteSpace(s.Rse.SoundbankPatch)
                || !string.IsNullOrWhiteSpace(s.Rse.SoundbankSet)
                || !string.IsNullOrWhiteSpace(s.Rse.ElementsSettingsXml)
                || !string.IsNullOrWhiteSpace(s.Rse.Pickups.OverloudPosition)
                || !string.IsNullOrWhiteSpace(s.Rse.Pickups.Volumes)
                || !string.IsNullOrWhiteSpace(s.Rse.Pickups.Tones)
                || s.Rse.EffectChain.Count > 0)
            {
                sound.Add(BuildSoundRse(s.Rse));
            }

            root.Add(sound);
        }

        return root;
    }

    private static XElement BuildRse(GpifRse rse)
    {
        var root = new XElement("RSE");
        AddTextElement(root, "Bank", rse.Bank);

        if (!string.IsNullOrWhiteSpace(rse.ChannelStripVersion)
            || !string.IsNullOrWhiteSpace(rse.ChannelStripParameters)
            || rse.Automations.Count > 0)
        {
            var strip = new XElement("ChannelStrip",
                !string.IsNullOrWhiteSpace(rse.ChannelStripVersion) ? new XAttribute("version", rse.ChannelStripVersion) : null,
                !string.IsNullOrWhiteSpace(rse.ChannelStripParameters) ? new XElement("Parameters", rse.ChannelStripParameters) : null,
                rse.Automations.Count > 0 ? BuildAutomations(rse.Automations) : null);
            root.Add(strip);
        }

        return root;
    }

    private static XElement BuildMasterRse(GpifMasterRse rse)
        => new("RSE",
            new XElement("Master", BuildRseEffects(rse.MasterEffects)));

    private static XElement BuildSoundRse(GpifSoundRse rse)
    {
        var root = new XElement("RSE");
        AddTextElement(root, "SoundbankPatch", rse.SoundbankPatch);
        AddTextElement(root, "SoundbankSet", rse.SoundbankSet);
        AddRawElementXml(root, rse.ElementsSettingsXml);

        if (!string.IsNullOrWhiteSpace(rse.Pickups.OverloudPosition)
            || !string.IsNullOrWhiteSpace(rse.Pickups.Volumes)
            || !string.IsNullOrWhiteSpace(rse.Pickups.Tones))
        {
            root.Add(new XElement("Pickups",
                !string.IsNullOrWhiteSpace(rse.Pickups.OverloudPosition) ? new XElement("OverloudPosition", rse.Pickups.OverloudPosition) : null,
                !string.IsNullOrWhiteSpace(rse.Pickups.Volumes) ? new XElement("Volumes", rse.Pickups.Volumes) : null,
                !string.IsNullOrWhiteSpace(rse.Pickups.Tones) ? new XElement("Tones", rse.Pickups.Tones) : null));
        }

        if (rse.EffectChain.Count > 0)
        {
            root.Add(new XElement("EffectChain", BuildRseEffects(rse.EffectChain)));
        }

        return root;
    }

    private static IEnumerable<XElement> BuildRseEffects(IReadOnlyList<GpifRseEffect> effects)
        => effects.Select(effect => new XElement("Effect",
            !string.IsNullOrWhiteSpace(effect.Id) ? new XAttribute("id", effect.Id) : null,
            effect.Bypass ? new XElement("ByPass") : null,
            !string.IsNullOrWhiteSpace(effect.Parameters) ? new XElement("Parameters", effect.Parameters) : null));

    private static XElement BuildAutomations(IReadOnlyList<GpifAutomation> automations)
    {
        var root = new XElement("Automations");
        foreach (var a in automations)
        {
            var el = new XElement("Automation");
            AddTextElement(el, "Type", a.Type);
            if (a.Linear.HasValue) el.Add(new XElement("Linear", a.Linear.Value.ToString().ToLowerInvariant()));
            if (a.Bar.HasValue) el.Add(new XElement("Bar", a.Bar.Value));
            if (a.Position.HasValue) el.Add(new XElement("Position", a.Position.Value));
            if (a.Visible.HasValue) el.Add(new XElement("Visible", a.Visible.Value.ToString().ToLowerInvariant()));
            AddTextElement(el, "Value", a.Value);
            root.Add(el);
        }

        return root;
    }

    private static XElement BuildMidiConnection(GpifMidiConnection midiConnection)
        => new("MidiConnection",
            midiConnection.Port.HasValue ? new XElement("Port", midiConnection.Port.Value) : null,
            midiConnection.PrimaryChannel.HasValue ? new XElement("PrimaryChannel", midiConnection.PrimaryChannel.Value) : null,
            midiConnection.SecondaryChannel.HasValue ? new XElement("SecondaryChannel", midiConnection.SecondaryChannel.Value) : null,
            midiConnection.ForceOneChannelPerString.HasValue
                ? new XElement("ForeOneChannelPerString", midiConnection.ForceOneChannelPerString.Value.ToString().ToLowerInvariant())
                : null);

    private static XElement BuildLyrics(GpifLyrics lyrics)
        => new("Lyrics",
            lyrics.Dispatched.HasValue ? new XAttribute("dispatched", lyrics.Dispatched.Value.ToString().ToLowerInvariant()) : null,
            lyrics.Lines.Select(line => new XElement("Line",
                new XElement("Text", line.Text ?? string.Empty),
                line.Offset.HasValue ? new XElement("Offset", line.Offset.Value) : null)));

    private static XElement BuildTranspose(GpifTranspose transpose)
        => new("Transpose",
            transpose.Chromatic.HasValue ? new XElement("Chromatic", transpose.Chromatic.Value) : null,
            transpose.Octave.HasValue ? new XElement("Octave", transpose.Octave.Value) : null);

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

    private static XElement BuildBar(GpifBar b)
    {
        var bar = new XElement("Bar", new XAttribute("id", b.Id), new XElement("Voices", b.VoicesReferenceList));
        AddTextElement(bar, "Clef", b.Clef);

        if (b.Properties.Count > 0)
        {
            bar.Add(new XElement("Properties", b.Properties.Select(kv => new XElement("Property", new XAttribute("name", kv.Key), new XElement("Value", kv.Value)))));
        }

        if (b.XProperties.Count > 0)
        {
            bar.Add(new XElement("XProperties", b.XProperties.Select(kv => new XElement("XProperty", new XAttribute("id", kv.Key), new XElement("Int", kv.Value)))));
        }

        return bar;
    }
    private static XElement BuildVoice(GpifVoice v)
    {
        var voice = new XElement("Voice", new XAttribute("id", v.Id), new XElement("Beats", v.BeatsReferenceList));
        if (v.Properties.Count > 0)
        {
            voice.Add(new XElement("Properties", v.Properties.Select(kv => new XElement("Property", new XAttribute("name", kv.Key), new XElement("Value", kv.Value)))));
        }

        if (v.DirectionTags.Length > 0)
        {
            voice.Add(new XElement("Directions", v.DirectionTags.Select(tag => new XElement(tag))));
        }

        return voice;
    }
    private static XElement BuildRhythm(GpifRhythm r)
    {
        var el = new XElement("Rhythm", new XAttribute("id", r.Id), new XElement("NoteValue", r.NoteValue));
        for (var i = 0; i < r.AugmentationDots; i++) el.Add(new XElement("AugmentationDot"));
        if (r.PrimaryTuplet is not null) el.Add(new XElement("PrimaryTuplet", new XAttribute("num", r.PrimaryTuplet.Numerator), new XAttribute("den", r.PrimaryTuplet.Denominator)));
        if (r.SecondaryTuplet is not null) el.Add(new XElement("SecondaryTuplet", new XAttribute("num", r.SecondaryTuplet.Numerator), new XAttribute("den", r.SecondaryTuplet.Denominator)));
        return el;
    }
    private static XElement BuildBeat(GpifBeat b)
    {
        var el = new XElement("Beat", new XAttribute("id", b.Id), new XElement("Rhythm", new XAttribute("ref", b.RhythmRef)));
        AddTextElement(el, "GraceNotes", b.GraceType);
        AddTextElement(el, "Dynamic", b.Dynamic);
        if (b.DeadSlapped) el.Add(new XElement("DeadSlapped"));
        if (b.Tremolo) el.Add(new XElement("Tremolo", b.TremoloValue));
        AddTextElement(el, "Chord", b.ChordId);
        AddTextElement(el, "FreeText", b.FreeText);
        if (b.Arpeggio) el.Add(new XElement("Arpeggio", b.BrushIsUp ? "Up" : "Down"));
        if (!string.IsNullOrWhiteSpace(b.NotesReferenceList)) el.Add(new XElement("Notes", b.NotesReferenceList));

        var beatProperties = new XElement("Properties");
        if (!string.IsNullOrWhiteSpace(b.PickStrokeDirection))
        {
            beatProperties.Add(new XElement("Property",
                new XAttribute("name", "PickStroke"),
                new XElement("Direction", b.PickStrokeDirection)));
        }

        if (!string.IsNullOrWhiteSpace(b.VibratoWithTremBarStrength))
        {
            beatProperties.Add(new XElement("Property",
                new XAttribute("name", "VibratoWTremBar"),
                new XElement("Strength", b.VibratoWithTremBarStrength)));
        }

        if (b.Slapped)
        {
            beatProperties.Add(new XElement("Property",
                new XAttribute("name", "Slapped"),
                new XElement("Enable")));
        }

        if (b.Popped)
        {
            beatProperties.Add(new XElement("Property",
                new XAttribute("name", "Popped"),
                new XElement("Enable")));
        }

        if (b.Brush && !b.Arpeggio)
        {
            beatProperties.Add(new XElement("Property",
                new XAttribute("name", "Brush"),
                new XElement("Direction", b.BrushIsUp ? "Up" : "Down")));
        }

        if (b.Rasgueado)
        {
            beatProperties.Add(new XElement("Property",
                new XAttribute("name", "Rasgueado"),
                new XElement("Enable")));
        }

        if (b.WhammyBar)
        {
            beatProperties.Add(new XElement("Property",
                new XAttribute("name", "WhammyBar"),
                new XElement("Enable")));
        }

        if (b.WhammyBarExtended)
        {
            beatProperties.Add(new XElement("Property",
                new XAttribute("name", "WhammyBarExtend"),
                new XElement("Enable")));
        }

        AddBeatFloatProperty(beatProperties, "WhammyBarOriginValue", b.WhammyBarOriginValue);
        AddBeatFloatProperty(beatProperties, "WhammyBarMiddleValue", b.WhammyBarMiddleValue);
        AddBeatFloatProperty(beatProperties, "WhammyBarDestinationValue", b.WhammyBarDestinationValue);
        AddBeatFloatProperty(beatProperties, "WhammyBarOriginOffset", b.WhammyBarOriginOffset);
        AddBeatFloatProperty(beatProperties, "WhammyBarMiddleOffset1", b.WhammyBarMiddleOffset1);
        AddBeatFloatProperty(beatProperties, "WhammyBarMiddleOffset2", b.WhammyBarMiddleOffset2);
        AddBeatFloatProperty(beatProperties, "WhammyBarDestinationOffset", b.WhammyBarDestinationOffset);

        if (beatProperties.HasElements)
        {
            el.Add(beatProperties);
        }

        if (b.XProperties.Count > 0)
        {
            el.Add(new XElement("XProperties", b.XProperties.Select(kv =>
                new XElement("XProperty", new XAttribute("id", kv.Key), new XElement("Int", kv.Value)))));
        }

        return el;
    }

    private static void AddBeatFloatProperty(XElement parent, string name, decimal? value)
    {
        if (!value.HasValue) return;
        parent.Add(new XElement("Property", new XAttribute("name", name), new XElement("Float", value.Value)));
    }

    private static XElement BuildNote(GpifNote n)
    {
        var el = new XElement("Note", new XAttribute("id", n.Id));
        AddTextElement(el, "LeftFingering", n.Articulation.LeftFingering);
        AddTextElement(el, "RightFingering", n.Articulation.RightFingering);
        AddTextElement(el, "Ornament", n.Articulation.Ornament);
        if (n.Articulation.Accent.HasValue) el.Add(new XElement("Accent", n.Articulation.Accent.Value));
        if (n.Articulation.AntiAccent) el.Add(new XElement("AntiAccent"));
        if (n.Articulation.InstrumentArticulation.HasValue) el.Add(new XElement("InstrumentArticulation", n.Articulation.InstrumentArticulation.Value));
        if (n.Articulation.LetRing) el.Add(new XElement("LetRing"));
        if (n.Articulation.TieOrigin || n.Articulation.TieDestination)
            el.Add(new XElement("Tie", new XAttribute("origin", n.Articulation.TieOrigin.ToString().ToLowerInvariant()), new XAttribute("destination", n.Articulation.TieDestination.ToString().ToLowerInvariant())));
        if (n.Articulation.Trill.HasValue) el.Add(new XElement("Trill", n.Articulation.Trill.Value));
        if (!string.IsNullOrWhiteSpace(n.Articulation.Vibrato)) el.Add(new XElement("Vibrato", n.Articulation.Vibrato));

        var props = new XElement("Properties");
        var propertyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (n.MidiPitch.HasValue)
        {
            AddPitchProperty(props, propertyNames, "ConcertPitch", n.MidiPitch.Value);
        }

        foreach (var property in n.Properties)
        {
            AddNoteProperty(props, propertyNames, property);
        }

        if (n.MidiPitch.HasValue && !propertyNames.Contains("Midi"))
        {
            props.Add(new XElement("Property",
                new XAttribute("name", "Midi"),
                new XElement("Number", n.MidiPitch.Value)));
            propertyNames.Add("Midi");
        }

        if (n.TransposedMidiPitch.HasValue)
        {
            AddPitchProperty(props, propertyNames, "TransposedPitch", n.TransposedMidiPitch.Value);
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
        if (!string.IsNullOrWhiteSpace(n.Articulation.HarmonicTypeText))
        {
            props.Add(new XElement(
                "Property",
                new XAttribute("name", "HarmonicType"),
                new XElement("HType", n.Articulation.HarmonicTypeText)));
        }
        else
        {
            AddNumberProperty(props, "HarmonicType", n.Articulation.HarmonicType);
        }

        if (n.Articulation.HarmonicFret.HasValue)
        {
            props.Add(new XElement(
                "Property",
                new XAttribute("name", "HarmonicFret"),
                new XElement("HFret", n.Articulation.HarmonicFret.Value)));
        }

        if (n.Articulation.BendEnabled) AddBoolProperty(props, "Bended", true);
        AddDecimalProperty(props, "BendOriginOffset", n.Articulation.BendOriginOffset);
        AddDecimalProperty(props, "BendOriginValue", n.Articulation.BendOriginValue);
        AddDecimalProperty(props, "BendMiddleOffset1", n.Articulation.BendMiddleOffset1);
        AddDecimalProperty(props, "BendMiddleOffset2", n.Articulation.BendMiddleOffset2);
        AddDecimalProperty(props, "BendMiddleValue", n.Articulation.BendMiddleValue);
        AddDecimalProperty(props, "BendDestinationOffset", n.Articulation.BendDestinationOffset);
        AddDecimalProperty(props, "BendDestinationValue", n.Articulation.BendDestinationValue);

        if (props.HasElements) el.Add(props);

        if (n.XProperties.Count > 0)
        {
            el.Add(new XElement("XProperties", n.XProperties.Select(kv =>
                new XElement("XProperty", new XAttribute("id", kv.Key), new XElement("Int", kv.Value)))));
        }

        return el;
    }

    private static void AddPitchProperty(XElement parent, HashSet<string> propertyNames, string propertyName, int midi)
    {
        if (propertyNames.Contains(propertyName))
        {
            return;
        }

        var (step, accidental, octave) = FromMidi(midi);
        parent.Add(new XElement(
            "Property",
            new XAttribute("name", propertyName),
            new XElement(
                "Pitch",
                new XElement("Step", step),
                new XElement("Accidental", accidental),
                new XElement("Octave", octave))));

        propertyNames.Add(propertyName);
    }

    private static void AddNoteProperty(XElement parent, HashSet<string> propertyNames, GpifNoteProperty property)
    {
        if (string.IsNullOrWhiteSpace(property.Name) || propertyNames.Contains(property.Name))
        {
            return;
        }

        XElement? payload = null;
        if (property.Flags.HasValue)
        {
            payload = new XElement("Flags", property.Flags.Value);
        }
        else if (property.Number.HasValue)
        {
            payload = new XElement("Number", property.Number.Value);
        }
        else if (property.Fret.HasValue)
        {
            payload = new XElement("Fret", property.Fret.Value);
        }
        else if (property.StringNumber.HasValue)
        {
            payload = new XElement("String", property.StringNumber.Value);
        }
        else if (!string.IsNullOrWhiteSpace(property.HType))
        {
            payload = new XElement("HType", property.HType);
        }
        else if (property.HFret.HasValue)
        {
            payload = new XElement("HFret", property.HFret.Value);
        }
        else if (property.Float.HasValue)
        {
            payload = new XElement("Float", property.Float.Value);
        }
        else if (property.Enabled)
        {
            payload = new XElement("Enable");
        }

        if (payload is null)
        {
            return;
        }

        parent.Add(new XElement(
            "Property",
            new XAttribute("name", property.Name),
            payload));
        propertyNames.Add(property.Name);
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
