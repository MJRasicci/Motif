namespace GPIO.NET.Models.Patching;

public sealed class GpPatchDocument
{
    public IReadOnlyList<AppendNotesPatch> AppendNotes { get; init; } = Array.Empty<AppendNotesPatch>();

    public IReadOnlyList<InsertBeatPatch> InsertBeats { get; init; } = Array.Empty<InsertBeatPatch>();

    public IReadOnlyList<UpdateNoteArticulationPatch> UpdateNoteArticulations { get; init; } = Array.Empty<UpdateNoteArticulationPatch>();

    public IReadOnlyList<AppendBarPatch> AppendBars { get; init; } = Array.Empty<AppendBarPatch>();

    public IReadOnlyList<AppendVoicePatch> AppendVoices { get; init; } = Array.Empty<AppendVoicePatch>();
}

public sealed class AppendNotesPatch
{
    public int TrackId { get; init; }

    public int MasterBarIndex { get; init; }

    public int VoiceIndex { get; init; }

    public string RhythmNoteValue { get; init; } = "Quarter";

    public int AugmentationDots { get; init; }

    public int? TupletNumerator { get; init; }

    public int? TupletDenominator { get; init; }

    public IReadOnlyList<int> MidiPitches { get; init; } = Array.Empty<int>();
}

public sealed class InsertBeatPatch
{
    public int TrackId { get; init; }

    public int MasterBarIndex { get; init; }

    public int VoiceIndex { get; init; }

    public int BeatInsertIndex { get; init; }

    public string RhythmNoteValue { get; init; } = "Quarter";

    public int AugmentationDots { get; init; }

    public int? TupletNumerator { get; init; }

    public int? TupletDenominator { get; init; }

    public IReadOnlyList<int> MidiPitches { get; init; } = Array.Empty<int>();
}

public sealed class UpdateNoteArticulationPatch
{
    public int NoteId { get; init; }

    public bool? LetRing { get; init; }

    public bool? PalmMuted { get; init; }

    public bool? Muted { get; init; }

    public bool? HopoOrigin { get; init; }

    public bool? HopoDestination { get; init; }

    public int? SlideFlags { get; init; }
}

public sealed class AppendBarPatch
{
    public int MasterBarIndex { get; init; }

    public int TrackId { get; init; }

    public int NewBarVoiceCount { get; init; } = 1;
}

public sealed class AppendVoicePatch
{
    public int TrackId { get; init; }

    public int MasterBarIndex { get; init; }

    public IReadOnlyList<int> InitialBeatIds { get; init; } = Array.Empty<int>();
}
