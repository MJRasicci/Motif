namespace GPIO.NET.Implementation;

using GPIO.NET.Abstractions;
using GPIO.NET.Models.Patching;
using System.IO.Compression;
using System.Xml.Linq;

public sealed class GuitarProPatcher : IGuitarProPatcher
{
    public async ValueTask<PatchResult> PatchAsync(string sourceGpPath, string outputGpPath, GpPatchDocument patch, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(patch);

        var diagnostics = new PatchDiagnostics();

        if (!File.Exists(sourceGpPath))
        {
            throw new FileNotFoundException($"Source gp file not found: {sourceGpPath}", sourceGpPath);
        }

        var outDir = Path.GetDirectoryName(outputGpPath);
        if (!string.IsNullOrWhiteSpace(outDir))
        {
            Directory.CreateDirectory(outDir);
        }

        await using var source = await ZipFile.OpenReadAsync(sourceGpPath, cancellationToken).ConfigureAwait(false);
        var scoreEntry = source.GetEntry("Content/score.gpif") ?? throw new InvalidDataException("Archive missing Content/score.gpif");

        XDocument gpif;
        await using (var scoreStream = await scoreEntry.OpenAsync(cancellationToken).ConfigureAwait(false))
        {
            gpif = await XDocument.LoadAsync(scoreStream, LoadOptions.None, cancellationToken).ConfigureAwait(false);
        }

        ApplyPatch(gpif, patch, diagnostics);

        if (File.Exists(outputGpPath))
        {
            File.Delete(outputGpPath);
        }

        using var target = ZipFile.Open(outputGpPath, ZipArchiveMode.Create);
        foreach (var entry in source.Entries)
        {
            var targetEntry = target.CreateEntry(entry.FullName, CompressionLevel.Optimal);
            await using var inStream = await entry.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var outStream = await targetEntry.OpenAsync(cancellationToken).ConfigureAwait(false);

            if (string.Equals(entry.FullName, "Content/score.gpif", StringComparison.OrdinalIgnoreCase))
            {
                await gpif.SaveAsync(outStream, SaveOptions.None, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await inStream.CopyToAsync(outStream, cancellationToken).ConfigureAwait(false);
            }
        }

        return new PatchResult { Diagnostics = diagnostics };
    }

    private static void ApplyPatch(XDocument gpif, GpPatchDocument patch, PatchDiagnostics diagnostics)
    {
        var root = gpif.Root ?? throw new InvalidDataException("Invalid GPIF document.");

        var tracksEl = root.Element("Tracks") ?? throw new InvalidDataException("GPIF missing Tracks.");
        var masterBarsEl = root.Element("MasterBars") ?? throw new InvalidDataException("GPIF missing MasterBars.");
        var barsEl = root.Element("Bars") ?? throw new InvalidDataException("GPIF missing Bars.");
        var voicesEl = root.Element("Voices") ?? throw new InvalidDataException("GPIF missing Voices.");
        var beatsEl = root.Element("Beats") ?? throw new InvalidDataException("GPIF missing Beats.");
        var notesEl = root.Element("Notes") ?? throw new InvalidDataException("GPIF missing Notes.");
        var rhythmsEl = root.Element("Rhythms") ?? throw new InvalidDataException("GPIF missing Rhythms.");

        var trackList = tracksEl.Elements("Track").ToList();
        var masterBarList = masterBarsEl.Elements("MasterBar").ToList();

        foreach (var op in patch.AppendBars)
        {
            if (op.MasterBarIndex < 0 || op.MasterBarIndex >= masterBarList.Count)
            {
                throw new InvalidOperationException($"Master bar index {op.MasterBarIndex} out of range.");
            }

            var trackOrderIndex = trackList.FindIndex(t => ParseInt(t.Attribute("id")?.Value) == op.TrackId);
            if (trackOrderIndex < 0)
            {
                throw new InvalidOperationException($"Track id {op.TrackId} not found.");
            }

            var masterBar = masterBarList[op.MasterBarIndex];
            var barRefs = SplitRefs(masterBar.Element("Bars")?.Value);

            var newBarId = NextId(barsEl, "Bar");
            var newVoiceIds = new List<int>();
            var voiceCount = Math.Max(1, op.NewBarVoiceCount);
            for (var i = 0; i < voiceCount; i++)
            {
                var voiceId = NextId(voicesEl, "Voice");
                voicesEl.Add(new XElement("Voice", new XAttribute("id", voiceId), new XElement("Beats", string.Empty)));
                newVoiceIds.Add(voiceId);
            }

            barsEl.Add(new XElement("Bar",
                new XAttribute("id", newBarId),
                new XElement("Voices", JoinRefs(newVoiceIds))));

            if (trackOrderIndex <= barRefs.Count)
            {
                if (trackOrderIndex == barRefs.Count)
                {
                    barRefs.Add(newBarId);
                }
                else
                {
                    barRefs[trackOrderIndex] = newBarId;
                }
            }

            masterBar.SetElementValue("Bars", JoinRefs(barRefs));
            diagnostics.Add("append-bar", $"MasterBar {op.MasterBarIndex}, Track {op.TrackId}: created Bar {newBarId} with Voices [{JoinRefs(newVoiceIds)}]");
        }

        foreach (var op in patch.AppendVoices)
        {
            var barEl = ResolveBarElement(op.TrackId, op.MasterBarIndex, trackList, masterBarList, barsEl);
            var voiceRefs = SplitRefs(barEl.Element("Voices")?.Value);

            var newVoiceId = NextId(voicesEl, "Voice");
            voicesEl.Add(new XElement("Voice",
                new XAttribute("id", newVoiceId),
                new XElement("Beats", JoinRefs(op.InitialBeatIds))));

            voiceRefs.Add(newVoiceId);
            barEl.SetElementValue("Voices", JoinRefs(voiceRefs));
            diagnostics.Add("append-voice", $"Track {op.TrackId}, MasterBar {op.MasterBarIndex}: appended Voice {newVoiceId}");
        }

        foreach (var op in patch.AppendNotes)
        {
            var voiceEl = ResolveVoiceElement(op.TrackId, op.MasterBarIndex, op.VoiceIndex, trackList, masterBarList, barsEl, voicesEl);
            var built = BuildBeatWithDependencies(op.RhythmNoteValue, op.AugmentationDots, op.TupletNumerator, op.TupletDenominator, op.MidiPitches, rhythmsEl, beatsEl, notesEl);

            var beatRefs = SplitRefs(voiceEl.Element("Beats")?.Value);
            beatRefs.Add(built.BeatId);
            voiceEl.SetElementValue("Beats", JoinRefs(beatRefs));
            diagnostics.Add("append-notes", $"Track {op.TrackId}, MasterBar {op.MasterBarIndex}, VoiceIndex {op.VoiceIndex}: appended Beat {built.BeatId} / Rhythm {built.RhythmId}");
        }

        foreach (var op in patch.InsertBeats)
        {
            var voiceEl = ResolveVoiceElement(op.TrackId, op.MasterBarIndex, op.VoiceIndex, trackList, masterBarList, barsEl, voicesEl);
            var built = BuildBeatWithDependencies(op.RhythmNoteValue, op.AugmentationDots, op.TupletNumerator, op.TupletDenominator, op.MidiPitches, rhythmsEl, beatsEl, notesEl);

            var beatRefs = SplitRefs(voiceEl.Element("Beats")?.Value);
            var index = Math.Clamp(op.BeatInsertIndex, 0, beatRefs.Count);
            beatRefs.Insert(index, built.BeatId);
            voiceEl.SetElementValue("Beats", JoinRefs(beatRefs));
            diagnostics.Add("insert-beat", $"Track {op.TrackId}, MasterBar {op.MasterBarIndex}, VoiceIndex {op.VoiceIndex}: inserted Beat {built.BeatId} at {index}");
        }

        foreach (var op in patch.UpdateNoteArticulations)
        {
            var noteEl = notesEl.Elements("Note").FirstOrDefault(n => ParseInt(n.Attribute("id")?.Value) == op.NoteId)
                         ?? throw new InvalidOperationException($"Note id {op.NoteId} not found.");

            if (op.LetRing.HasValue)
            {
                SetToggleElement(noteEl, "LetRing", op.LetRing.Value);
            }

            UpsertPropertyBool(noteEl, "PalmMuted", op.PalmMuted);
            UpsertPropertyBool(noteEl, "Muted", op.Muted);
            UpsertPropertyBool(noteEl, "HopoOrigin", op.HopoOrigin);
            UpsertPropertyBool(noteEl, "HopoDestination", op.HopoDestination);

            if (op.SlideFlags.HasValue)
            {
                UpsertPropertyFlags(noteEl, "Slide", op.SlideFlags.Value);
            }

            diagnostics.Add("update-note-articulation", $"Updated Note {op.NoteId}");
        }
    }

    private static XElement ResolveBarElement(
        int trackId,
        int masterBarIndex,
        IReadOnlyList<XElement> trackList,
        IReadOnlyList<XElement> masterBarList,
        XElement barsEl)
    {
        var trackOrderIndex = trackList.ToList().FindIndex(t => ParseInt(t.Attribute("id")?.Value) == trackId);
        if (trackOrderIndex < 0)
        {
            throw new InvalidOperationException($"Track id {trackId} not found.");
        }

        if (masterBarIndex < 0 || masterBarIndex >= masterBarList.Count)
        {
            throw new InvalidOperationException($"Master bar index {masterBarIndex} out of range.");
        }

        var masterBar = masterBarList[masterBarIndex];
        var barRefs = SplitRefs(masterBar.Element("Bars")?.Value);
        if (trackOrderIndex >= barRefs.Count)
        {
            throw new InvalidOperationException($"Master bar {masterBarIndex} has no bar for track order index {trackOrderIndex}.");
        }

        var barId = barRefs[trackOrderIndex];
        return barsEl.Elements("Bar").FirstOrDefault(b => ParseInt(b.Attribute("id")?.Value) == barId)
               ?? throw new InvalidOperationException($"Bar id {barId} not found.");
    }

    private static XElement ResolveVoiceElement(
        int trackId,
        int masterBarIndex,
        int voiceIndex,
        IReadOnlyList<XElement> trackList,
        IReadOnlyList<XElement> masterBarList,
        XElement barsEl,
        XElement voicesEl)
    {
        var barEl = ResolveBarElement(trackId, masterBarIndex, trackList, masterBarList, barsEl);

        var voiceRefs = SplitRefs(barEl.Element("Voices")?.Value);
        if (voiceIndex < 0 || voiceIndex >= voiceRefs.Count)
        {
            throw new InvalidOperationException($"Voice index {voiceIndex} not available for bar {ParseInt(barEl.Attribute("id")?.Value)}.");
        }

        var voiceId = voiceRefs[voiceIndex];
        return voicesEl.Elements("Voice").FirstOrDefault(v => ParseInt(v.Attribute("id")?.Value) == voiceId)
               ?? throw new InvalidOperationException($"Voice id {voiceId} not found.");
    }

    private static (int BeatId, int RhythmId, IReadOnlyList<int> NoteIds) BuildBeatWithDependencies(
        string rhythmNoteValue,
        int augmentationDots,
        int? tupletNumerator,
        int? tupletDenominator,
        IReadOnlyList<int> midiPitches,
        XElement rhythmsEl,
        XElement beatsEl,
        XElement notesEl)
    {
        var nextRhythmId = NextId(rhythmsEl, "Rhythm");
        var nextBeatId = NextId(beatsEl, "Beat");
        var nextNoteId = NextId(notesEl, "Note");

        rhythmsEl.Add(BuildRhythm(nextRhythmId, rhythmNoteValue, augmentationDots, tupletNumerator, tupletDenominator));

        var noteIds = new List<int>();
        foreach (var midi in midiPitches)
        {
            notesEl.Add(BuildNote(nextNoteId, midi));
            noteIds.Add(nextNoteId);
            nextNoteId++;
        }

        beatsEl.Add(new XElement("Beat",
            new XAttribute("id", nextBeatId),
            new XElement("Rhythm", new XAttribute("ref", nextRhythmId)),
            new XElement("Notes", JoinRefs(noteIds))));

        return (nextBeatId, nextRhythmId, noteIds);
    }

    private static XElement BuildRhythm(int id, string noteValue, int dots, int? tupletNumerator, int? tupletDenominator)
    {
        var rhythm = new XElement("Rhythm",
            new XAttribute("id", id),
            new XElement("NoteValue", noteValue));

        for (var i = 0; i < dots; i++)
        {
            rhythm.Add(new XElement("AugmentationDot"));
        }

        if (tupletNumerator is > 0 && tupletDenominator is > 0)
        {
            rhythm.Add(new XElement("PrimaryTuplet",
                new XElement("Num", tupletNumerator.Value),
                new XElement("Den", tupletDenominator.Value)));
        }

        return rhythm;
    }

    private static XElement BuildNote(int id, int midi)
    {
        var (step, accidental, octave) = FromMidi(midi);
        return new XElement("Note",
            new XAttribute("id", id),
            new XElement("Properties",
                new XElement("Property",
                    new XAttribute("name", "Pitch"),
                    new XElement("Pitch",
                        new XElement("Step", step),
                        new XElement("Accidental", accidental),
                        new XElement("Octave", octave)))));
    }

    private static void SetToggleElement(XElement parent, string elementName, bool enabled)
    {
        var existing = parent.Element(elementName);
        if (enabled)
        {
            if (existing is null)
            {
                parent.Add(new XElement(elementName));
            }
        }
        else
        {
            existing?.Remove();
        }
    }

    private static void UpsertPropertyBool(XElement noteEl, string propertyName, bool? enabled)
    {
        if (!enabled.HasValue)
        {
            return;
        }

        var props = GetOrCreateProperties(noteEl);
        var prop = props.Elements("Property").FirstOrDefault(p => string.Equals((string?)p.Attribute("name"), propertyName, StringComparison.OrdinalIgnoreCase));

        if (enabled.Value)
        {
            if (prop is null)
            {
                prop = new XElement("Property", new XAttribute("name", propertyName));
                props.Add(prop);
            }

            prop.SetElementValue("Enable", null);
            prop.Add(new XElement("Enable"));
        }
        else
        {
            prop?.Remove();
        }
    }

    private static void UpsertPropertyFlags(XElement noteEl, string propertyName, int flags)
    {
        var props = GetOrCreateProperties(noteEl);
        var prop = props.Elements("Property").FirstOrDefault(p => string.Equals((string?)p.Attribute("name"), propertyName, StringComparison.OrdinalIgnoreCase));
        if (prop is null)
        {
            prop = new XElement("Property", new XAttribute("name", propertyName));
            props.Add(prop);
        }

        prop.SetElementValue("Flags", flags);
    }

    private static XElement GetOrCreateProperties(XElement noteEl)
    {
        var props = noteEl.Element("Properties");
        if (props is null)
        {
            props = new XElement("Properties");
            noteEl.Add(props);
        }

        return props;
    }

    private static int NextId(XElement container, string elementName)
    {
        var max = container.Elements(elementName)
            .Select(e => ParseInt(e.Attribute("id")?.Value))
            .DefaultIfEmpty(0)
            .Max();
        return max + 1;
    }

    private static int ParseInt(string? value) => int.TryParse(value, out var i) ? i : -1;

    private static List<int> SplitRefs(string? refs)
        => string.IsNullOrWhiteSpace(refs)
            ? []
            : refs.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(ParseInt).Where(i => i >= 0).ToList();

    private static string JoinRefs(IEnumerable<int> ids) => string.Join(' ', ids);

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
