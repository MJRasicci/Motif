namespace GPIO.NET.Implementation;

using GPIO.NET.Models.Raw;
using GPIO.NET.Models.Write;
using GPIO.NET.Utilities;
using System.Xml.Linq;

public static class GpifWriteFidelityDiagnostics
{
    private static readonly string[] OptionalScoreElementNames =
    [
        "SubTitle",
        "Words",
        "Music",
        "WordsAndMusic",
        "Copyright",
        "Tabber",
        "Instructions",
        "Notices",
        "FirstPageHeader",
        "FirstPageFooter",
        "PageHeader",
        "PageFooter",
        "ScoreSystemsDefaultLayout",
        "ScoreSystemsLayout",
        "ScoreZoomPolicy",
        "ScoreZoom",
        "MultiVoice"
    ];

    public static void AppendNoOpSourceFidelityWarnings(
        GpifDocument sourceRaw,
        GpifDocument outputRaw,
        byte[] sourceGpifBytes,
        byte[] outputGpifBytes,
        WriteDiagnostics diagnostics)
    {
        ArgumentNullException.ThrowIfNull(sourceRaw);
        ArgumentNullException.ThrowIfNull(outputRaw);
        ArgumentNullException.ThrowIfNull(sourceGpifBytes);
        ArgumentNullException.ThrowIfNull(outputGpifBytes);
        ArgumentNullException.ThrowIfNull(diagnostics);

        WarnIfRawObjectCountsChanged(sourceRaw, outputRaw, diagnostics);
        WarnIfReferenceCountsChanged(sourceRaw, outputRaw, diagnostics);
        WarnIfMasterBarSlotCountsChanged(sourceRaw, outputRaw, diagnostics);
        WarnIfEmptyScoreNodesDropped(sourceGpifBytes, outputGpifBytes, diagnostics);

        if (!sourceGpifBytes.AsSpan().SequenceEqual(outputGpifBytes))
        {
            diagnostics.Warn(
                code: "RAW_GPIF_BYTE_DRIFT",
                category: "RawFidelity",
                message: "No-op full write changed Content/score.gpif bytes relative to the source archive.");
        }
    }

    private static void WarnIfRawObjectCountsChanged(GpifDocument sourceRaw, GpifDocument outputRaw, WriteDiagnostics diagnostics)
    {
        var drift = new List<string>();
        AppendCountDrift(drift, "MasterBars", sourceRaw.MasterBars.Count, outputRaw.MasterBars.Count);
        AppendCountDrift(drift, "Bars", sourceRaw.BarsById.Count, outputRaw.BarsById.Count);
        AppendCountDrift(drift, "Voices", sourceRaw.VoicesById.Count, outputRaw.VoicesById.Count);
        AppendCountDrift(drift, "Beats", sourceRaw.BeatsById.Count, outputRaw.BeatsById.Count);
        AppendCountDrift(drift, "Notes", sourceRaw.NotesById.Count, outputRaw.NotesById.Count);
        AppendCountDrift(drift, "Rhythms", sourceRaw.RhythmsById.Count, outputRaw.RhythmsById.Count);

        if (drift.Count == 0)
        {
            return;
        }

        diagnostics.Warn(
            code: "RAW_OBJECT_COUNT_DRIFT",
            category: "RawFidelity",
            message: $"No-op full write changed raw GPIF object counts: {string.Join(", ", drift)}.");
    }

    private static void WarnIfReferenceCountsChanged(GpifDocument sourceRaw, GpifDocument outputRaw, WriteDiagnostics diagnostics)
    {
        var drift = new List<string>();

        var sourceBarSlots = sourceRaw.MasterBars.Sum(masterBar => ReferenceListParser.SplitRefs(masterBar.BarsReferenceList).Count);
        var outputBarSlots = outputRaw.MasterBars.Sum(masterBar => ReferenceListParser.SplitRefs(masterBar.BarsReferenceList).Count);
        AppendCountDrift(drift, "MasterBar/Bars refs", sourceBarSlots, outputBarSlots);

        var sourceBeatRefs = sourceRaw.VoicesById.Values.Sum(voice => ReferenceListParser.SplitRefs(voice.BeatsReferenceList).Count);
        var outputBeatRefs = outputRaw.VoicesById.Values.Sum(voice => ReferenceListParser.SplitRefs(voice.BeatsReferenceList).Count);
        AppendCountDrift(drift, "Voice/Beats refs", sourceBeatRefs, outputBeatRefs);

        var sourceNoteRefs = sourceRaw.BeatsById.Values.Sum(beat => ReferenceListParser.SplitRefs(beat.NotesReferenceList).Count);
        var outputNoteRefs = outputRaw.BeatsById.Values.Sum(beat => ReferenceListParser.SplitRefs(beat.NotesReferenceList).Count);
        AppendCountDrift(drift, "Beat/Notes refs", sourceNoteRefs, outputNoteRefs);

        if (drift.Count == 0)
        {
            return;
        }

        diagnostics.Warn(
            code: "RAW_REFERENCE_COUNT_DRIFT",
            category: "RawFidelity",
            message: $"No-op full write changed raw GPIF reference totals: {string.Join(", ", drift)}.");
    }

    private static void WarnIfMasterBarSlotCountsChanged(GpifDocument sourceRaw, GpifDocument outputRaw, WriteDiagnostics diagnostics)
    {
        var maxMasterBarCount = Math.Max(sourceRaw.MasterBars.Count, outputRaw.MasterBars.Count);
        var drift = new List<string>();

        for (var index = 0; index < maxMasterBarCount; index++)
        {
            var sourceCount = index < sourceRaw.MasterBars.Count
                ? ReferenceListParser.SplitRefs(sourceRaw.MasterBars[index].BarsReferenceList).Count
                : 0;
            var outputCount = index < outputRaw.MasterBars.Count
                ? ReferenceListParser.SplitRefs(outputRaw.MasterBars[index].BarsReferenceList).Count
                : 0;

            if (sourceCount != outputCount)
            {
                drift.Add($"{index}: {sourceCount} -> {outputCount}");
            }
        }

        if (drift.Count == 0)
        {
            return;
        }

        diagnostics.Warn(
            code: "MASTER_BAR_SLOT_COUNT_DRIFT",
            category: "RawFidelity",
            message: $"No-op full write changed MasterBar/Bars slot counts at {FormatSample(drift)}.");
    }

    private static void WarnIfEmptyScoreNodesDropped(byte[] sourceGpifBytes, byte[] outputGpifBytes, WriteDiagnostics diagnostics)
    {
        try
        {
            var sourceScore = LoadScore(sourceGpifBytes);
            var outputScore = LoadScore(outputGpifBytes);
            if (sourceScore is null || outputScore is null)
            {
                return;
            }

            var dropped = OptionalScoreElementNames
                .Where(name => HasEmptyChild(sourceScore, name) && outputScore.Element(name) is null)
                .ToArray();

            if (dropped.Length == 0)
            {
                return;
            }

            diagnostics.Warn(
                code: "EMPTY_SCORE_NODES_DROPPED",
                category: "RawFidelity",
                message: $"No-op full write dropped empty optional <Score> children: {string.Join(", ", dropped)}.");
        }
        catch (Exception ex)
        {
            diagnostics.Warn(
                code: "RAW_FIDELITY_DIAGNOSTICS_FAILED",
                category: "RawFidelity",
                message: $"Raw fidelity diagnostics failed: {ex.Message}");
        }
    }

    private static XElement? LoadScore(byte[] gpifBytes)
    {
        using var stream = new MemoryStream(gpifBytes, writable: false);
        var document = XDocument.Load(stream);
        return document.Root?.Element("Score");
    }

    private static bool HasEmptyChild(XElement parent, string elementName)
    {
        var child = parent.Element(elementName);
        return child is not null
               && !child.HasElements
               && string.IsNullOrWhiteSpace(child.Value);
    }

    private static void AppendCountDrift(List<string> drift, string label, int sourceCount, int outputCount)
    {
        if (sourceCount != outputCount)
        {
            drift.Add($"{label} {sourceCount} -> {outputCount}");
        }
    }

    private static string FormatSample(IReadOnlyList<string> values)
    {
        const int sampleSize = 5;
        if (values.Count <= sampleSize)
        {
            return string.Join(", ", values);
        }

        return $"{string.Join(", ", values.Take(sampleSize))} (+{values.Count - sampleSize} more)";
    }
}
