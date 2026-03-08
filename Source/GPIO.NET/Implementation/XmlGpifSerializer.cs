namespace GPIO.NET.Implementation;

using GPIO.NET.Abstractions;
using GPIO.NET.Models.Raw;
using System.Globalization;
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

        var builder = new StringBuilder();
        builder.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?>\n");
        builder.Append("<GPIF>\n");
        AppendElementXml(builder, new XElement("GPVersion", ResolveOrDefault(document.GpVersion, DefaultGpVersion)));
        AppendElementXml(builder, BuildGpRevision(document.GpRevision));
        AppendElementXml(builder, new XElement("Encoding", new XElement("EncodingDescription", ResolveOrDefault(document.EncodingDescription, DefaultEncodingDescription))));
        AppendElementXml(builder, BuildScore(document.Score));
        AppendElementXml(builder, BuildMasterTrack(document.MasterTrack));
        AppendRawElementXml(builder, document.BackingTrackXml);
        AppendRawElementXml(builder, document.AudioTracksXml);
        AppendCollectionXml(builder, "Tracks", document.Tracks.OrderBy(t => t.Id).Select(BuildTrack));
        AppendCollectionXml(builder, "MasterBars", document.MasterBars.OrderBy(m => m.Index).Select(BuildMasterBar));
        AppendCollectionXml(builder, "Bars", document.BarsById.OrderBy(kv => kv.Key).Select(kv => BuildBar(kv.Value)));
        AppendCollectionXml(builder, "Voices", document.VoicesById.OrderBy(kv => kv.Key).Select(kv => BuildVoice(kv.Value)));
        AppendCollectionXml(builder, "Beats", document.BeatsById.OrderBy(kv => kv.Key).Select(kv => BuildBeat(kv.Value)));
        AppendCollectionXml(builder, "Notes", document.NotesById.OrderBy(kv => kv.Key).Select(kv => BuildNote(kv.Value)));
        AppendCollectionXml(builder, "Rhythms", document.RhythmsById.OrderBy(kv => kv.Key).Select(kv => BuildRhythm(kv.Value)));
        AppendRawElementXml(builder, document.AssetsXml);
        AppendRawElementXml(builder, document.ScoreViewsXml);
        builder.Append("</GPIF>");

        var bytes = Encoding.UTF8.GetBytes(builder.ToString());
        await output.WriteAsync(bytes, cancellationToken);
    }

    private static XElement BuildGpRevision(GpifRevisionInfo revision)
    {
        if (CanPreserveSourceGpRevisionXml(revision, out var rawRevision))
        {
            return rawRevision;
        }

        return new XElement(
            "GPRevision",
            new XAttribute("required", ResolveOrDefault(revision.Required, DefaultGpRevisionRequired)),
            new XAttribute("recommended", ResolveOrDefault(revision.Recommended, DefaultGpRevisionRecommended)),
            ResolveOrDefault(revision.Value, DefaultGpRevisionValue));
    }

    private static string ResolveOrDefault(string value, string fallback)
        => string.IsNullOrWhiteSpace(value) ? fallback : value;

    private static XElement BuildScore(ScoreInfo s)
        => PreserveSourceElementXmlIfEquivalent(s.Xml, BuildScoreCore(s));

    private static XElement BuildScoreCore(ScoreInfo s)
    {
        var el = new XElement("Score",
            CreateCDataElement("Title", s.Title),
            CreateCDataElement("Artist", s.Artist),
            CreateCDataElement("Album", s.Album));

        AddOptionalScoreCDataElement(el, s, "SubTitle", s.SubTitle);
        AddOptionalScoreCDataElement(el, s, "Words", s.Words);
        AddOptionalScoreCDataElement(el, s, "Music", s.Music);
        AddOptionalScoreCDataElement(el, s, "WordsAndMusic", s.WordsAndMusic);
        AddOptionalScoreCDataElement(el, s, "Copyright", s.Copyright);
        AddOptionalScoreCDataElement(el, s, "Tabber", s.Tabber);
        AddOptionalScoreCDataElement(el, s, "Instructions", s.Instructions);
        AddOptionalScoreCDataElement(el, s, "Notices", s.Notices);
        AddOptionalScoreCDataElement(el, s, "FirstPageHeader", s.FirstPageHeader);
        AddOptionalScoreCDataElement(el, s, "FirstPageFooter", s.FirstPageFooter);
        AddOptionalScoreCDataElement(el, s, "PageHeader", s.PageHeader);
        AddOptionalScoreCDataElement(el, s, "PageFooter", s.PageFooter);
        AddOptionalPlainTextScoreElement(el, s, "ScoreSystemsDefaultLayout", s.ScoreSystemsDefaultLayout);
        AddOptionalPlainTextScoreElement(el, s, "ScoreSystemsLayout", s.ScoreSystemsLayout);
        AddOptionalPlainTextScoreElement(el, s, "ScoreZoomPolicy", s.ScoreZoomPolicy);
        AddOptionalPlainTextScoreElement(el, s, "ScoreZoom", s.ScoreZoom);
        AddRawElementXml(el, s.PageSetupXml);
        AddOptionalPlainTextScoreElement(el, s, "MultiVoice", s.MultiVoice);

        return el;
    }

    private static XElement BuildMasterTrack(GpifMasterTrack master)
        => PreserveSourceElementXmlIfEquivalent(master.Xml, BuildMasterTrackCore(master));

    private static XElement BuildMasterTrackCore(GpifMasterTrack master)
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

        if (!string.IsNullOrWhiteSpace(master.AutomationsXml))
        {
            AddRawElementXml(el, master.AutomationsXml);
        }
        else if (master.Automations.Count > 0)
        {
            el.Add(BuildAutomations(master.Automations));
        }

        return el;
    }

    private static XElement BuildTrack(GpifTrack t)
        => PreserveSourceElementXmlIfEquivalent(t.Xml, BuildTrackCore(t));

    private static XElement BuildTrackCore(GpifTrack t)
    {
        var el = new XElement("Track",
            new XAttribute("id", t.Id),
            new XElement("Name", t.Name));

        AddOptionalEmptyTextElement(el, "ShortName", t.ShortName, t.HasExplicitEmptyShortName);
        AddTextElement(el, "Color", t.Color);
        AddTextElement(el, "SystemsDefautLayout", t.SystemsDefaultLayout);
        AddOptionalEmptyTextElement(el, "SystemsLayout", t.SystemsLayout, t.HasExplicitEmptySystemsLayout);
        if (t.AutoBrush) el.Add(new XElement("AutoBrush"));
        if (t.LetRingThroughout) el.Add(new XElement("LetRingThroughout"));
        if (t.PalmMute.HasValue) el.Add(new XElement("PalmMute", t.PalmMute.Value));
        if (t.AutoAccentuation.HasValue) el.Add(new XElement("AutoAccentuation", t.AutoAccentuation.Value));
        AddTextElement(el, "PlayingStyle", t.PlayingStyle);
        if (t.UseOneChannelPerString) el.Add(new XElement("UseOneChannelPerString"));
        if (t.IconId.HasValue) el.Add(new XElement("IconId", t.IconId.Value));
        if (t.ForcedSound.HasValue) el.Add(new XElement("ForcedSound", t.ForcedSound.Value));

        var trackProperties = BuildTrackProperties(t);
        if (trackProperties is not null)
        {
            el.Add(trackProperties);
        }

        if (!string.IsNullOrWhiteSpace(t.InstrumentSetXml))
        {
            AddRawElementXml(el, t.InstrumentSetXml);
        }
        else if (!string.IsNullOrWhiteSpace(t.InstrumentSet.Name) || !string.IsNullOrWhiteSpace(t.InstrumentSet.Type) || t.InstrumentSet.LineCount.HasValue)
        {
            el.Add(BuildInstrumentSet(t.InstrumentSet));
        }

        if (!string.IsNullOrWhiteSpace(t.NotationPatchXml))
        {
            AddRawElementXml(el, t.NotationPatchXml);
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

    private static XElement? BuildTrackProperties(GpifTrack track)
    {
        var properties = new XElement("Properties");
        var propertyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (name, value) in track.Properties.OrderBy(kv => kv.Key, StringComparer.Ordinal))
        {
            if (string.Equals(name, "Tuning", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            properties.Add(new XElement(
                "Property",
                new XAttribute("name", name),
                new XElement("Value", value)));
            propertyNames.Add(name);
        }

        if (ShouldEmitTrackTuningProperty(track) && !propertyNames.Contains("Tuning"))
        {
            properties.Add(new XElement(
                "Property",
                new XAttribute("name", "Tuning"),
                new XElement("Pitches", string.Join(' ', track.TuningPitches)),
                new XElement("Instrument", track.TuningInstrument ?? string.Empty),
                new XElement("Label", track.TuningLabel ?? string.Empty),
                track.TuningLabelVisible.HasValue
                    ? new XElement("LabelVisible", track.TuningLabelVisible.Value.ToString().ToLowerInvariant())
                    : null));
        }

        return properties.HasElements ? properties : null;
    }

    private static bool ShouldEmitTrackTuningProperty(GpifTrack track)
        => track.TuningPitches.Length > 0
           && (track.HasTrackTuningProperty || string.IsNullOrWhiteSpace(track.StavesXml));

    private static XElement BuildMasterBar(GpifMasterBar m)
        => PreserveSourceElementXmlIfEquivalent(m.Xml, BuildMasterBarCore(m));

    private static XElement BuildMasterBarCore(GpifMasterBar m)
    {
        var el = new XElement("MasterBar",
            new XElement("Time", m.Time),
            new XElement("Bars", m.BarsReferenceList ?? string.Empty));

        if (m.DoubleBar)
        {
            el.Add(new XElement("DoubleBar"));
        }

        if (m.FreeTime)
        {
            el.Add(new XElement("FreeTime"));
        }

        AddTextElement(el, "TripletFeel", m.TripletFeel);
        if (!string.IsNullOrWhiteSpace(m.AlternateEndings)) el.Add(new XElement("AlternateEndings", m.AlternateEndings));
        if (m.RepeatStart || m.RepeatEnd || m.RepeatCount > 0 || m.RepeatStartAttributePresent || m.RepeatEndAttributePresent || m.RepeatCountAttributePresent)
        {
            var repeat = new XElement("Repeat");
            if (m.RepeatStart || m.RepeatStartAttributePresent) repeat.SetAttributeValue("start", m.RepeatStart.ToString().ToLowerInvariant());
            if (m.RepeatEnd || m.RepeatEndAttributePresent) repeat.SetAttributeValue("end", m.RepeatEnd.ToString().ToLowerInvariant());
            if (m.RepeatCount > 0 || m.RepeatCountAttributePresent) repeat.SetAttributeValue("count", m.RepeatCount);
            el.Add(repeat);
        }

        if (!string.IsNullOrWhiteSpace(m.SectionLetter) || !string.IsNullOrWhiteSpace(m.SectionText) || m.HasExplicitEmptySection)
        {
            el.Add(new XElement(
                "Section",
                new XElement("Letter", new XCData(m.SectionLetter)),
                new XElement("Text", new XCData(m.SectionText))));
        }

        if (CanPreserveSourceDirectionsXml(m))
        {
            AddRawElementXml(el, m.DirectionsXml);
        }
        else if (!string.IsNullOrWhiteSpace(m.Jump) || !string.IsNullOrWhiteSpace(m.Target) || m.DirectionProperties.Count > 0)
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

        if (CanPreserveSourceXPropertiesXml(m.XPropertiesXml, m.XProperties))
        {
            AddRawElementXml(el, m.XPropertiesXml);
        }
        else if (m.XProperties.Count > 0)
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
        => PreserveSourceElementXmlIfEquivalent(b.Xml, BuildBarCore(b));

    private static XElement BuildBarCore(GpifBar b)
    {
        var bar = new XElement("Bar", new XAttribute("id", b.Id), new XElement("Voices", b.VoicesReferenceList));
        AddTextElement(bar, "Clef", b.Clef);
        AddTextElement(bar, "SimileMark", b.SimileMark);

        if (b.Properties.Count > 0)
        {
            bar.Add(new XElement("Properties", b.Properties.Select(kv => new XElement("Property", new XAttribute("name", kv.Key), new XElement("Value", kv.Value)))));
        }

        if (CanPreserveSourceXPropertiesXml(b.XPropertiesXml, b.XProperties))
        {
            AddRawElementXml(bar, b.XPropertiesXml);
        }
        else if (b.XProperties.Count > 0)
        {
            bar.Add(new XElement("XProperties", b.XProperties.Select(kv => new XElement("XProperty", new XAttribute("id", kv.Key), new XElement("Int", kv.Value)))));
        }

        return bar;
    }
    private static XElement BuildVoice(GpifVoice v)
        => PreserveSourceElementXmlIfEquivalent(v.Xml, BuildVoiceCore(v));

    private static XElement BuildVoiceCore(GpifVoice v)
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
        => PreserveSourceElementXmlIfEquivalent(r.Xml, BuildRhythmCore(r));

    private static XElement BuildRhythmCore(GpifRhythm r)
    {
        var el = new XElement("Rhythm", new XAttribute("id", r.Id), new XElement("NoteValue", r.NoteValue));
        if (r.AugmentationDotCounts.Length > 0)
        {
            foreach (var count in r.AugmentationDotCounts)
            {
                el.Add(r.AugmentationDotUsesCountAttribute || count != 1
                    ? new XElement("AugmentationDot", new XAttribute("count", count))
                    : new XElement("AugmentationDot"));
            }
        }
        else if (r.AugmentationDots > 0)
        {
            if (r.AugmentationDotUsesCountAttribute)
            {
                el.Add(new XElement("AugmentationDot", new XAttribute("count", r.AugmentationDots)));
            }
            else
            {
                for (var i = 0; i < r.AugmentationDots; i++)
                {
                    el.Add(new XElement("AugmentationDot"));
                }
            }
        }
        if (r.PrimaryTuplet is not null) el.Add(new XElement("PrimaryTuplet", new XAttribute("num", r.PrimaryTuplet.Numerator), new XAttribute("den", r.PrimaryTuplet.Denominator)));
        if (r.SecondaryTuplet is not null) el.Add(new XElement("SecondaryTuplet", new XAttribute("num", r.SecondaryTuplet.Numerator), new XAttribute("den", r.SecondaryTuplet.Denominator)));
        return el;
    }
    private static XElement BuildBeat(GpifBeat b)
        => PreserveSourceElementXmlIfEquivalent(b.Xml, BuildBeatCore(b));

    private static XElement BuildBeatCore(GpifBeat b)
    {
        var el = new XElement("Beat", new XAttribute("id", b.Id), new XElement("Rhythm", new XAttribute("ref", b.RhythmRef)));
        AddTextElement(el, "GraceNotes", b.GraceType);
        AddTextElement(el, "Dynamic", b.Dynamic);
        AddTextElement(el, "TransposedPitchStemOrientation", b.TransposedPitchStemOrientation);
        if (b.HasTransposedPitchStemOrientationUserDefinedElement)
        {
            AddOptionalEmptyTextElement(
                el,
                "TransposedPitchStemOrientationUserDefined",
                b.UserTransposedPitchStemOrientation,
                preserveExplicitEmpty: true);
        }
        else
        {
            AddTextElement(el, "UserTransposedPitchStemOrientation", b.UserTransposedPitchStemOrientation);
        }
        AddTextElement(el, "ConcertPitchStemOrientation", b.ConcertPitchStemOrientation);
        AddTextElement(el, "Wah", b.Wah);
        AddTextElement(el, "Golpe", b.Golpe);
        AddTextElement(el, "Fadding", b.Fadding);
        if (b.Slashed)
        {
            el.Add(new XElement("Slashed"));
        }
        AddTextElement(el, "Hairpin", b.Hairpin);
        AddTextElement(el, "Ottavia", b.Ottavia);
        if (b.WhammyUsesElement)
        {
            var whammy = BuildWhammyElement(b);
            if (whammy is not null)
            {
                el.Add(whammy);
            }
        }
        if (b.WhammyExtendUsesElement && b.WhammyBarExtended)
        {
            el.Add(new XElement("WhammyExtend"));
        }
        AddTextElement(el, "Variation", b.Variation);
        if (b.DeadSlapped) el.Add(new XElement("DeadSlapped"));
        if (b.Tremolo) el.Add(new XElement("Tremolo", b.TremoloValue));
        AddTextElement(el, "Chord", b.ChordId);
        AddTextElement(el, "FreeText", b.FreeText);
        var legato = BuildLegatoElement(b);
        if (legato is not null)
        {
            el.Add(legato);
        }
        if (b.Arpeggio) el.Add(new XElement("Arpeggio", b.BrushIsUp ? "Up" : "Down"));
        if (!string.IsNullOrWhiteSpace(b.NotesReferenceList)) el.Add(new XElement("Notes", b.NotesReferenceList));

        var beatPropertyValues = new Dictionary<string, string>(b.Properties, StringComparer.OrdinalIgnoreCase);
        UpsertBeatProperty(beatPropertyValues, "PickStroke", b.PickStrokeDirection);
        UpsertBeatProperty(beatPropertyValues, "VibratoWTremBar", b.VibratoWithTremBarStrength);
        UpsertBeatProperty(beatPropertyValues, "Slapped", b.Slapped);
        UpsertBeatProperty(beatPropertyValues, "Popped", b.Popped);
        if (b.Brush && !b.Arpeggio)
        {
            UpsertBeatProperty(beatPropertyValues, "Brush", b.BrushIsUp ? "Up" : "Down");
        }
        if (!string.IsNullOrWhiteSpace(b.RasgueadoPattern))
        {
            UpsertBeatProperty(beatPropertyValues, "Rasgueado", b.RasgueadoPattern);
        }
        else
        {
            UpsertBeatProperty(beatPropertyValues, "Rasgueado", b.Rasgueado);
        }
        if (!b.WhammyUsesElement)
        {
            if (ShouldEmitWhammyBarProperty(b))
            {
                UpsertBeatProperty(beatPropertyValues, "WhammyBar", b.WhammyBar);
            }
            UpsertBeatProperty(beatPropertyValues, "WhammyBarOriginValue", b.WhammyBarOriginValue);
            UpsertBeatProperty(beatPropertyValues, "WhammyBarMiddleValue", b.WhammyBarMiddleValue);
            UpsertBeatProperty(beatPropertyValues, "WhammyBarDestinationValue", b.WhammyBarDestinationValue);
            UpsertBeatProperty(beatPropertyValues, "WhammyBarOriginOffset", b.WhammyBarOriginOffset);
            UpsertBeatProperty(beatPropertyValues, "WhammyBarMiddleOffset1", b.WhammyBarMiddleOffset1);
            UpsertBeatProperty(beatPropertyValues, "WhammyBarMiddleOffset2", b.WhammyBarMiddleOffset2);
            UpsertBeatProperty(beatPropertyValues, "WhammyBarDestinationOffset", b.WhammyBarDestinationOffset);
        }
        if (!b.WhammyExtendUsesElement)
        {
            UpsertBeatProperty(beatPropertyValues, "WhammyBarExtend", b.WhammyBarExtended);
        }

        var beatProperties = new XElement("Properties");
        foreach (var (name, value) in beatPropertyValues.OrderBy(kv => kv.Key, StringComparer.Ordinal))
        {
            var property = BuildBeatProperty(name, value);
            if (property is not null)
            {
                beatProperties.Add(property);
            }
        }

        if (beatProperties.HasElements)
        {
            el.Add(beatProperties);
        }

        AddRawElementXml(el, b.LyricsXml);

        if (CanPreserveSourceXPropertiesXml(b.XPropertiesXml, b.XProperties))
        {
            AddRawElementXml(el, b.XPropertiesXml);
        }
        else if (b.XProperties.Count > 0)
        {
            el.Add(new XElement("XProperties", b.XProperties.Select(kv =>
                new XElement("XProperty", new XAttribute("id", kv.Key), new XElement("Int", kv.Value)))));
        }

        return el;
    }

    private static void UpsertBeatProperty(IDictionary<string, string> properties, string name, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        properties[name] = value;
    }

    private static void UpsertBeatProperty(IDictionary<string, string> properties, string name, bool value)
    {
        if (!value)
        {
            return;
        }

        properties[name] = "true";
    }

    private static void UpsertBeatProperty(IDictionary<string, string> properties, string name, decimal? value)
    {
        if (!value.HasValue)
        {
            return;
        }

        properties[name] = value.Value.ToString(CultureInfo.InvariantCulture);
    }

    private static XElement? BuildBeatProperty(string name, string value)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        if (string.Equals(name, "Rasgueado", StringComparison.OrdinalIgnoreCase)
            && !TryParseBeatBooleanValue(value))
        {
            return new XElement("Property", new XAttribute("name", name), new XElement("Rasgueado", value));
        }

        if (IsBeatEnableProperty(name))
        {
            return TryParseBeatBooleanValue(value)
                ? new XElement("Property", new XAttribute("name", name), new XElement("Enable"))
                : null;
        }

        var payloadName = GetBeatPropertyPayloadName(name);
        if (payloadName is not null)
        {
            return new XElement("Property", new XAttribute("name", name), new XElement(payloadName, value));
        }

        if (TryParseBeatBooleanValue(value))
        {
            return new XElement("Property", new XAttribute("name", name), new XElement("Enable"));
        }

        return new XElement("Property", new XAttribute("name", name), new XElement("Value", value));
    }

    private static bool IsBeatEnableProperty(string name)
        => name is "Slapped"
            or "Popped"
            or "Rasgueado"
            or "WhammyBar"
            or "WhammyBarExtend";

    private static string? GetBeatPropertyPayloadName(string name)
        => name switch
        {
            "BarreFret" => "Fret",
            "BarreString" => "String",
            "PickStroke" or "Brush" => "Direction",
            "VibratoWTremBar" => "Strength",
            "PrimaryPickupVolume" or "PrimaryPickupTone"
                or "WhammyBarOriginValue"
                or "WhammyBarMiddleValue"
                or "WhammyBarDestinationValue"
                or "WhammyBarOriginOffset"
                or "WhammyBarMiddleOffset1"
                or "WhammyBarMiddleOffset2"
                or "WhammyBarDestinationOffset" => "Float",
            _ => null
        };

    private static bool TryParseBeatBooleanValue(string value)
        => bool.TryParse(value, out var parsed) && parsed;

    private static XElement BuildNote(GpifNote n)
        => PreserveSourceElementXmlIfEquivalent(n.Xml, BuildNoteCore(n));

    private static XElement BuildNoteCore(GpifNote n)
    {
        var el = new XElement("Note", new XAttribute("id", n.Id));
        AddTextElement(el, "LeftFingering", n.Articulation.LeftFingering);
        AddTextElement(el, "RightFingering", n.Articulation.RightFingering);
        AddTextElement(el, "Ornament", n.Articulation.Ornament);
        if (n.Articulation.Accent.HasValue) el.Add(new XElement("Accent", n.Articulation.Accent.Value));
        if (n.Articulation.AntiAccent)
        {
            el.Add(string.IsNullOrWhiteSpace(n.Articulation.AntiAccentValue)
                ? new XElement("AntiAccent")
                : new XElement("AntiAccent", n.Articulation.AntiAccentValue));
        }
        if (n.Velocity.HasValue) el.Add(new XElement("Velocity", n.Velocity.Value));
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
            AddPitchProperty(props, propertyNames, "ConcertPitch", n.ConcertPitch, n.MidiPitch.Value);
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
            AddPitchProperty(props, propertyNames, "TransposedPitch", n.TransposedPitch, n.TransposedMidiPitch.Value);
        }

        if (n.ShowStringNumber && !propertyNames.Contains("ShowStringNumber"))
        {
            props.Add(new XElement("Property", new XAttribute("name", "ShowStringNumber"), new XElement("Enable")));
            propertyNames.Add("ShowStringNumber");
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

        if (CanPreserveSourceXPropertiesXml(n.XPropertiesXml, n.XProperties))
        {
            AddRawElementXml(el, n.XPropertiesXml);
        }
        else if (n.XProperties.Count > 0)
        {
            el.Add(new XElement("XProperties", n.XProperties.Select(kv =>
                new XElement("XProperty", new XAttribute("id", kv.Key), new XElement("Int", kv.Value)))));
        }

        return el;
    }

    private static void AddPitchProperty(
        XElement parent,
        HashSet<string> propertyNames,
        string propertyName,
        GpifPitchValue? pitch,
        int midi)
    {
        if (propertyNames.Contains(propertyName))
        {
            return;
        }

        var (step, accidental, octave) = pitch is null
            ? FromMidi(midi)
            : (pitch.Step, pitch.Accidental, pitch.Octave ?? FromMidi(midi).octave);
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

    private static void AddOptionalEmptyTextElement(XElement parent, string name, string value, bool preserveExplicitEmpty)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            parent.Add(new XElement(name, value));
            return;
        }

        if (preserveExplicitEmpty)
        {
            parent.Add(new XElement(name, string.Empty));
        }
    }

    private static void AddOptionalScoreCDataElement(XElement parent, ScoreInfo score, string name, string value)
    {
        if (value.Length == 0)
        {
            if (!score.ExplicitEmptyOptionalElements.Contains(name, StringComparer.Ordinal))
            {
                return;
            }

            parent.Add(CreateCDataElement(name, value));
            return;
        }

        parent.Add(CreateCDataElement(name, value));
    }

    private static void AddOptionalPlainTextScoreElement(XElement parent, ScoreInfo score, string name, string value)
    {
        if (value.Length == 0)
        {
            if (!score.ExplicitEmptyOptionalElements.Contains(name, StringComparer.Ordinal))
            {
                return;
            }

            parent.Add(new XElement(name, string.Empty));
            return;
        }

        parent.Add(new XElement(name, value));
    }

    private static XElement CreateCDataElement(string name, string value)
        => new(name, new XCData(value));

    private static bool CanPreserveSourceGpRevisionXml(GpifRevisionInfo revision, out XElement rawRevision)
    {
        if (string.IsNullOrWhiteSpace(revision.Xml))
        {
            rawRevision = null!;
            return false;
        }

        try
        {
            rawRevision = XElement.Parse(revision.Xml);
            return string.Equals(rawRevision.Attribute("required")?.Value ?? string.Empty, revision.Required, StringComparison.Ordinal)
                && string.Equals(rawRevision.Attribute("recommended")?.Value ?? string.Empty, revision.Recommended, StringComparison.Ordinal)
                && string.Equals(rawRevision.Value ?? string.Empty, revision.Value, StringComparison.Ordinal);
        }
        catch
        {
            rawRevision = null!;
            return false;
        }
    }

    private static bool CanPreserveSourceDirectionsXml(GpifMasterBar masterBar)
    {
        if (string.IsNullOrWhiteSpace(masterBar.DirectionsXml))
        {
            return false;
        }

        try
        {
            var directions = XElement.Parse(masterBar.DirectionsXml);
            var sourceJump = directions.Elements("Jump").FirstOrDefault()?.Value ?? string.Empty;
            var sourceTarget = directions.Elements("Target").FirstOrDefault()?.Value ?? string.Empty;
            var sourceProperties = directions.Elements()
                .GroupBy(element => element.Name.LocalName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.Last().Value ?? string.Empty, StringComparer.OrdinalIgnoreCase);

            return string.Equals(sourceJump, masterBar.Jump, StringComparison.Ordinal)
                && string.Equals(sourceTarget, masterBar.Target, StringComparison.Ordinal)
                && DictionariesEqual(sourceProperties, masterBar.DirectionProperties);
        }
        catch
        {
            return false;
        }
    }

    private static void AddRawElementXml(XElement parent, string xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
        {
            return;
        }

        try
        {
            parent.Add(XElement.Parse(xml, LoadOptions.PreserveWhitespace));
        }
        catch
        {
            // ignore malformed passthrough chunks
        }
    }

    private static void AppendCollectionXml(StringBuilder builder, string name, IEnumerable<XElement> elements)
    {
        var materialized = elements.ToArray();
        if (materialized.Length == 0)
        {
            builder.Append('<').Append(name).Append(" />\n");
            return;
        }

        builder.Append('<').Append(name).Append(">\n");
        foreach (var element in materialized)
        {
            AppendElementXml(builder, element);
        }

        builder.Append("</").Append(name).Append(">\n");
    }

    private static void AppendElementXml(StringBuilder builder, XElement element)
    {
        builder.Append(NormalizeXml(element.ToString(SaveOptions.DisableFormatting)));
        builder.Append('\n');
    }

    private static void AppendRawElementXml(StringBuilder builder, string xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
        {
            return;
        }

        builder.Append(NormalizeXml(xml));
        builder.Append('\n');
    }

    private static string NormalizeXml(string xml)
        => xml.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');

    private static XElement PreserveSourceElementXmlIfEquivalent(string xml, XElement generatedElement)
    {
        if (CanPreserveSourceElementXml(xml, generatedElement, out var rawElement))
        {
            return rawElement;
        }

        return generatedElement;
    }

    private static bool CanPreserveSourceElementXml(string xml, XElement generatedElement, out XElement rawElement)
    {
        rawElement = null!;
        if (string.IsNullOrWhiteSpace(xml))
        {
            return false;
        }

        try
        {
            rawElement = XElement.Parse(xml, LoadOptions.PreserveWhitespace);
            var sourceComparable = XElement.Parse(xml);
            var generatedComparable = XElement.Parse(generatedElement.ToString(SaveOptions.DisableFormatting));
            var differences = GpifXmlDifferenceComparer.Compare(sourceComparable, generatedComparable);
            return differences.All(diff => string.Equals(diff.Code, "RAW_XML_CHILD_ORDER_DRIFT", StringComparison.Ordinal));
        }
        catch
        {
            rawElement = null!;
            return false;
        }
    }

    private static bool CanPreserveSourceXPropertiesXml(string xml, IReadOnlyDictionary<string, int> currentValues)
    {
        if (string.IsNullOrWhiteSpace(xml))
        {
            return false;
        }

        try
        {
            var sourceValues = XElement.Parse(xml)
                .Elements("XProperty")
                .Where(x => x.Attribute("id") is not null)
                .Select(x => new { Id = x.Attribute("id")!.Value, Value = TryParseInt(x.Element("Int")?.Value) })
                .Where(x => x.Value.HasValue)
                .ToDictionary(x => x.Id, x => x.Value!.Value);

            return DictionariesEqual(sourceValues, currentValues);
        }
        catch
        {
            return false;
        }
    }

    private static XElement? BuildLegatoElement(GpifBeat beat)
    {
        if (!beat.LegatoOrigin.HasValue && !beat.LegatoDestination.HasValue)
        {
            return null;
        }

        var legato = new XElement("Legato");
        if (beat.LegatoOrigin.HasValue)
        {
            legato.SetAttributeValue("origin", beat.LegatoOrigin.Value.ToString().ToLowerInvariant());
        }

        if (beat.LegatoDestination.HasValue)
        {
            legato.SetAttributeValue("destination", beat.LegatoDestination.Value.ToString().ToLowerInvariant());
        }

        return legato;
    }

    private static XElement? BuildWhammyElement(GpifBeat beat)
    {
        if (!beat.WhammyBar && !beat.WhammyBarExtended
            && !beat.WhammyBarOriginValue.HasValue
            && !beat.WhammyBarMiddleValue.HasValue
            && !beat.WhammyBarDestinationValue.HasValue
            && !beat.WhammyBarOriginOffset.HasValue
            && !beat.WhammyBarMiddleOffset1.HasValue
            && !beat.WhammyBarMiddleOffset2.HasValue
            && !beat.WhammyBarDestinationOffset.HasValue)
        {
            return null;
        }

        var element = new XElement("Whammy");
        SetDecimalAttribute(element, "originValue", beat.WhammyBarOriginValue);
        SetDecimalAttribute(element, "middleValue", beat.WhammyBarMiddleValue);
        SetDecimalAttribute(element, "destinationValue", beat.WhammyBarDestinationValue);
        SetDecimalAttribute(element, "originOffset", beat.WhammyBarOriginOffset);
        SetDecimalAttribute(element, "middleOffset1", beat.WhammyBarMiddleOffset1);
        SetDecimalAttribute(element, "middleOffset2", beat.WhammyBarMiddleOffset2);
        SetDecimalAttribute(element, "destinationOffset", beat.WhammyBarDestinationOffset);
        return element;
    }

    private static bool ShouldEmitWhammyBarProperty(GpifBeat beat)
    {
        if (!beat.WhammyBar)
        {
            return false;
        }

        if (beat.Properties.ContainsKey("WhammyBar"))
        {
            return true;
        }

        if (beat.WhammyExtendUsesElement
            && !beat.WhammyUsesElement
            && !HasWhammyCurveData(beat))
        {
            return false;
        }

        return true;
    }

    private static bool HasWhammyCurveData(GpifBeat beat)
        => beat.WhammyBarOriginValue.HasValue
           || beat.WhammyBarMiddleValue.HasValue
           || beat.WhammyBarDestinationValue.HasValue
           || beat.WhammyBarOriginOffset.HasValue
           || beat.WhammyBarMiddleOffset1.HasValue
           || beat.WhammyBarMiddleOffset2.HasValue
           || beat.WhammyBarDestinationOffset.HasValue;

    private static void SetDecimalAttribute(XElement element, string name, decimal? value)
    {
        if (!value.HasValue)
        {
            return;
        }

        element.SetAttributeValue(name, value.Value.ToString("0.000000", CultureInfo.InvariantCulture));
    }

    private static int? TryParseInt(string? value)
        => int.TryParse(value, out var parsed) ? parsed : null;

    private static bool DictionariesEqual<TKey, TValue>(
        IReadOnlyDictionary<TKey, TValue> left,
        IReadOnlyDictionary<TKey, TValue> right)
        where TKey : notnull
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        foreach (var (key, value) in left)
        {
            if (!right.TryGetValue(key, out var otherValue)
                || !EqualityComparer<TValue>.Default.Equals(value, otherValue))
            {
                return false;
            }
        }

        return true;
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
        parent.Add(new XElement(
            "Property",
            new XAttribute("name", name),
            new XElement("Float", value.Value.ToString("0.000000", CultureInfo.InvariantCulture))));
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
