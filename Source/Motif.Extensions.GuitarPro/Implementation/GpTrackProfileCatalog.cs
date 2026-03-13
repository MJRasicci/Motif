namespace Motif.Extensions.GuitarPro.Implementation;

using Motif.Extensions.GuitarPro.Models;
using Motif.Models;

internal static class GpTrackProfileCatalog
{
    private static readonly IReadOnlyList<GpTrackProfile> Profiles =
    [
        CreateSteelStringGuitar(),
        CreateNylonGuitar(),
        CreateElectricGuitar(),
        CreateAcousticBass(),
        CreateElectricBass(),
        CreateFretlessBass(),
        CreateSynthBass(),
        CreateAcousticPiano(),
        CreateElectricPiano(),
        CreateKeyboard(),
        CreateDrumKit(),
        CreateUkulele(),
        CreateMandolin(),
        CreateBanjo(),
        CreateViolin(),
        CreateViola(),
        CreateCello(),
        CreateContrabass(),
        CreateGenericPitched()
    ];

    public static GpTrackProfile ResolveProfile(Track track, TrackMetadata existingMetadata)
    {
        if (TryResolveFromInstrument(track.Instrument, out var fromInstrument))
        {
            return fromInstrument;
        }

        if (TryResolveFromMetadata(existingMetadata, track.Name, out var fromMetadata))
        {
            return fromMetadata;
        }

        if (TryResolveFromTuning(track, existingMetadata, out var fromTuning))
        {
            return fromTuning;
        }

        if (TryResolveFromTrackName(track.Name, out var fromName))
        {
            return fromName;
        }

        return Profiles[^1];
    }

    public static TrackInstrument InferInstrument(TrackMetadata metadata, string trackName)
        => ResolveProfile(
            new Track
            {
                Name = trackName
            },
            metadata).ToInstrument();

    private static bool TryResolveFromInstrument(TrackInstrument instrument, out GpTrackProfile profile)
    {
        profile = null!;

        if (instrument.Kind != InstrumentKind.Unknown)
        {
            profile = Profiles.FirstOrDefault(candidate => candidate.Kind == instrument.Kind)!;
            if (profile is not null)
            {
                return true;
            }
        }

        if (instrument.Family == InstrumentFamilyKind.Unknown && instrument.Role == TrackRoleKind.Unknown)
        {
            return false;
        }

        profile = Profiles.FirstOrDefault(candidate =>
            candidate.Family == instrument.Family
            && (instrument.Role == TrackRoleKind.Unknown || candidate.Role == instrument.Role))
            ?? Profiles[^1];
        return true;
    }

    private static bool TryResolveFromMetadata(TrackMetadata metadata, string trackName, out GpTrackProfile profile)
    {
        var search = string.Join(
            ' ',
            [
                trackName,
                metadata.ShortName,
                metadata.InstrumentSet.Name,
                metadata.InstrumentSet.Type,
                .. metadata.Sounds.Select(sound => sound.Name),
                .. metadata.Sounds.Select(sound => sound.Path),
                .. metadata.Sounds.Select(sound => sound.Rse.SoundbankPatch)
            ]);

        if (TryResolveByKeywords(search, out profile))
        {
            return true;
        }

        if (metadata.InstrumentSet.Elements.Any(element =>
                string.Equals(element.Type, "percussion", StringComparison.OrdinalIgnoreCase)
                || element.Name.Contains("Perc", StringComparison.OrdinalIgnoreCase))
            || metadata.Sounds.Any(sound => string.Equals(sound.Name, "Drumkit", StringComparison.OrdinalIgnoreCase)))
        {
            profile = Profiles.Single(candidate => candidate.Kind == InstrumentKind.DrumKit);
            return true;
        }

        profile = null!;
        return false;
    }

    private static bool TryResolveFromTuning(Track track, TrackMetadata metadata, out GpTrackProfile profile)
    {
        var tuning = track.Staves
            .OrderBy(staff => staff.StaffIndex)
            .SelectMany(staff => staff.Tuning.Pitches)
            .ToArray();
        if (tuning.Length == 0)
        {
            tuning = metadata.TuningPitches;
        }

        profile = null!;
        if (tuning.Length == 0)
        {
            return false;
        }

        return TryResolveByTuningPitches(tuning, out profile);
    }

    private static bool TryResolveFromTrackName(string trackName, out GpTrackProfile profile)
        => TryResolveByKeywords(trackName, out profile);

    private static bool TryResolveByKeywords(string? value, out GpTrackProfile profile)
    {
        profile = null!;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = value.ToLowerInvariant();
        profile = normalized switch
        {
            var candidate when candidate.Contains("drum") || candidate.Contains("perc") => Profiles.Single(p => p.Kind == InstrumentKind.DrumKit),
            var candidate when candidate.Contains("nylon") || candidate.Contains("classical") => Profiles.Single(p => p.Kind == InstrumentKind.NylonStringGuitar),
            var candidate when candidate.Contains("steel") => Profiles.Single(p => p.Kind == InstrumentKind.SteelStringGuitar),
            var candidate when candidate.Contains("electric bass") || candidate.Contains("bass finger") || candidate.Contains("pre-bass") => Profiles.Single(p => p.Kind == InstrumentKind.ElectricBass),
            var candidate when candidate.Contains("fretless") => Profiles.Single(p => p.Kind == InstrumentKind.FretlessBass),
            var candidate when candidate.Contains("synth bass") => Profiles.Single(p => p.Kind == InstrumentKind.SynthBass),
            var candidate when candidate.Contains("acoustic bass") => Profiles.Single(p => p.Kind == InstrumentKind.AcousticBass),
            var candidate when candidate.Contains("bass") => Profiles.Single(p => p.Kind == InstrumentKind.ElectricBass),
            var candidate when candidate.Contains("ukulele") => Profiles.Single(p => p.Kind == InstrumentKind.Ukulele),
            var candidate when candidate.Contains("mandolin") => Profiles.Single(p => p.Kind == InstrumentKind.Mandolin),
            var candidate when candidate.Contains("banjo") => Profiles.Single(p => p.Kind == InstrumentKind.Banjo),
            var candidate when candidate.Contains("viola") => Profiles.Single(p => p.Kind == InstrumentKind.Viola),
            var candidate when candidate.Contains("violin") || candidate.Contains("fiddle") => Profiles.Single(p => p.Kind == InstrumentKind.Violin),
            var candidate when candidate.Contains("cello") => Profiles.Single(p => p.Kind == InstrumentKind.Cello),
            var candidate when candidate.Contains("contrabass") => Profiles.Single(p => p.Kind == InstrumentKind.Contrabass),
            var candidate when candidate.Contains("piano") && candidate.Contains("electric") => Profiles.Single(p => p.Kind == InstrumentKind.ElectricPiano),
            var candidate when candidate.Contains("piano") => Profiles.Single(p => p.Kind == InstrumentKind.AcousticPiano),
            var candidate when candidate.Contains("keyboard") => Profiles.Single(p => p.Kind == InstrumentKind.Keyboard),
            var candidate when candidate.Contains("guitar") && (candidate.Contains("clean") || candidate.Contains("jazz") || candidate.Contains("electric")) => Profiles.Single(p => p.Kind == InstrumentKind.ElectricGuitar),
            var candidate when candidate.Contains("guitar") => Profiles.Single(p => p.Kind == InstrumentKind.SteelStringGuitar),
            _ => null!
        };

        return profile is not null;
    }

    private static bool TryResolveByTuningPitches(int[] tuningPitches, out GpTrackProfile profile)
    {
        profile = null!;

        if (tuningPitches.SequenceEqual([40, 45, 50, 55, 59, 64])
            || tuningPitches.SequenceEqual([35, 40, 45, 50, 55, 59, 64])
            || tuningPitches.SequenceEqual([30, 35, 40, 45, 50, 55, 59, 64]))
        {
            profile = Profiles.Single(candidate => candidate.Kind == InstrumentKind.SteelStringGuitar);
            return true;
        }

        if (tuningPitches.SequenceEqual([28, 33, 38, 43])
            || tuningPitches.SequenceEqual([23, 28, 33, 38, 43])
            || tuningPitches.SequenceEqual([23, 28, 33, 38, 43, 48])
            || tuningPitches.SequenceEqual([18, 23, 28, 33, 38, 43, 48]))
        {
            profile = Profiles.Single(candidate => candidate.Kind == InstrumentKind.ElectricBass);
            return true;
        }

        if (tuningPitches.SequenceEqual([67, 60, 64, 69]))
        {
            profile = Profiles.Single(candidate => candidate.Kind == InstrumentKind.Ukulele);
            return true;
        }

        if (tuningPitches.SequenceEqual([55, 62, 69, 76]))
        {
            profile = Profiles.Single(candidate => candidate.Kind == InstrumentKind.Mandolin);
            return true;
        }

        if (tuningPitches.SequenceEqual([48, 55, 62, 69]))
        {
            profile = Profiles.Single(candidate => candidate.Kind == InstrumentKind.Viola);
            return true;
        }

        if (tuningPitches.SequenceEqual([36, 43, 50, 57]))
        {
            profile = Profiles.Single(candidate => candidate.Kind == InstrumentKind.Cello);
            return true;
        }

        if (tuningPitches.SequenceEqual([62, 50, 55, 59, 62])
            || tuningPitches.SequenceEqual([67, 50, 55, 59, 62]))
        {
            profile = Profiles.Single(candidate => candidate.Kind == InstrumentKind.Banjo);
            return true;
        }

        return false;
    }

    private static GpTrackProfile CreateSteelStringGuitar()
        => CreateStringProfile(
            InstrumentKind.SteelStringGuitar,
            InstrumentFamilyKind.Guitar,
            "Steel Guitar",
            "steelGuitar",
            "Steel Mart",
            "Steel Mart",
            "Stringed/Acoustic Guitars/Steel Guitar",
            25,
            "D-Steel",
            [40, 45, 50, 55, 59, 64],
            transposeOctave: -1,
            clef: "G2");

    private static GpTrackProfile CreateNylonGuitar()
        => CreateStringProfile(
            InstrumentKind.NylonStringGuitar,
            InstrumentFamilyKind.Guitar,
            "Nylon Guitar",
            "nylonGuitar",
            "Nylon Guitar",
            "Nylon Guitar",
            "Stringed/Acoustic Guitars/Nylon Guitar",
            24,
            "Concerto-Nylon",
            [40, 45, 50, 55, 59, 64],
            transposeOctave: -1,
            clef: "G2");

    private static GpTrackProfile CreateElectricGuitar()
        => CreateStringProfile(
            InstrumentKind.ElectricGuitar,
            InstrumentFamilyKind.Guitar,
            "Electric Guitar",
            "electricGuitar",
            "Clean El Guitar",
            "Clean El Guitar",
            "Stringed/Electric Guitars/Clean El Guitar",
            27,
            "Classic-Guitar",
            [40, 45, 50, 55, 59, 64],
            transposeOctave: -1,
            clef: "G2");

    private static GpTrackProfile CreateAcousticBass()
        => CreateStringProfile(
            InstrumentKind.AcousticBass,
            InstrumentFamilyKind.Bass,
            "Acoustic Bass",
            "acousticBass",
            "Acoustic Bass",
            "Acoustic Bass",
            "Stringed/Basses/Acoustic Bass",
            32,
            "Acoustic-Bass",
            [28, 33, 38, 43],
            transposeOctave: -1,
            clef: "F4");

    private static GpTrackProfile CreateElectricBass()
        => CreateStringProfile(
            InstrumentKind.ElectricBass,
            InstrumentFamilyKind.Bass,
            "Electric Bass Finger",
            "electricBassFinger",
            "Electric Bass Finger",
            "Electric Bass Finger",
            "Stringed/Basses/Electric Bass Finger",
            33,
            "Pre-Bass",
            [28, 33, 38, 43],
            transposeOctave: -1,
            clef: "F4");

    private static GpTrackProfile CreateFretlessBass()
        => CreateStringProfile(
            InstrumentKind.FretlessBass,
            InstrumentFamilyKind.Bass,
            "Fretless Bass",
            "fretlessBass",
            "Fretless Bass",
            "Fretless Bass",
            "Stringed/Basses/Fretless Bass",
            35,
            "Pre-Bass",
            [28, 33, 38, 43],
            transposeOctave: -1,
            clef: "F4");

    private static GpTrackProfile CreateSynthBass()
        => CreateStringProfile(
            InstrumentKind.SynthBass,
            InstrumentFamilyKind.Bass,
            "Synth Bass",
            "synthBass",
            "Synth Bass",
            "Synth Bass",
            "Stringed/Basses/Synth Bass",
            38,
            "Pre-Bass",
            [28, 33, 38, 43],
            transposeOctave: -1,
            clef: "F4");

    private static GpTrackProfile CreateAcousticPiano()
        => CreateKeyboardProfile(InstrumentKind.AcousticPiano, InstrumentFamilyKind.Piano, "Ac Piano", "acousticPiano", "AcPiano", 1, "German-APiano");

    private static GpTrackProfile CreateElectricPiano()
        => CreateKeyboardProfile(InstrumentKind.ElectricPiano, InstrumentFamilyKind.Piano, "El Piano", "electricPiano", "ElPiano", 2, "MarkI-EPiano");

    private static GpTrackProfile CreateKeyboard()
        => CreateKeyboardProfile(InstrumentKind.Keyboard, InstrumentFamilyKind.Keyboard, "Keyboard", "keyboard", "ElPiano", 2, "MarkI-EPiano");

    private static GpTrackProfile CreateDrumKit()
        => new()
        {
            Family = InstrumentFamilyKind.Percussion,
            Kind = InstrumentKind.DrumKit,
            Role = TrackRoleKind.Percussion,
            InstrumentSetName = "Drumkit",
            InstrumentSetType = "drumKit",
            InstrumentSetLineCount = 5,
            InstrumentElementName = "Percussion",
            InstrumentElementType = "percussion",
            InstrumentElementSoundbankName = "Drumkit",
            SoundName = "Drumkit",
            SoundLabel = "Drumkit",
            SoundPath = "Percussion/Drumkit",
            MidiProgram = 0,
            SoundbankPatch = "Drumkit-Master",
            SoundbankSet = "Factory",
            ChannelBank = "Drumkit-Master",
            ChannelStripVersion = "E56",
            ChannelStripParameters = "0.5 0.5 0.5 0.5 0.5 0.5 0.5 0.5 0.5 0 0.5 0.5 0.795 0.5 0.5 0.5",
            ChannelStripAutomations = CreateDefaultChannelStripAutomations(),
            SoundRsePickupsOverloudPosition = string.Empty,
            SoundRsePickupsVolumes = string.Empty,
            SoundRsePickupsTones = string.Empty,
            PlayingStyle = "Default",
            PlaybackState = "Default",
            AudioEngineState = "RSE",
            PrimaryChannel = 8,
            SecondaryChannel = 9,
            ForceOneChannelPerString = false,
            Clef = "Neutral"
        };

    private static GpTrackProfile CreateUkulele()
        => CreateStringProfile(
            InstrumentKind.Ukulele,
            InstrumentFamilyKind.Ukulele,
            "Ukulele",
            "ukulele",
            "Ukulele",
            "Ukulele",
            "Stringed/Ukulele/Ukulele",
            106,
            "CC-Ukulele",
            [67, 60, 64, 69],
            transposeOctave: 0,
            clef: "G2");

    private static GpTrackProfile CreateMandolin()
        => CreateStringProfile(
            InstrumentKind.Mandolin,
            InstrumentFamilyKind.Mandolin,
            "Mandolin",
            "mandolin",
            "Mandolin",
            "Mandolin",
            "Stringed/World/Mandolin",
            25,
            "D-Steel",
            [55, 62, 69, 76],
            transposeOctave: 0,
            clef: "G2");

    private static GpTrackProfile CreateBanjo()
        => CreateStringProfile(
            InstrumentKind.Banjo,
            InstrumentFamilyKind.Banjo,
            "Banjo",
            "banjo",
            "Banjo",
            "Banjo",
            "Stringed/World/Banjo",
            105,
            "5-Banjo",
            [67, 50, 55, 59, 62],
            transposeOctave: 0,
            clef: "G2");

    private static GpTrackProfile CreateViolin()
        => CreateStringProfile(
            InstrumentKind.Violin,
            InstrumentFamilyKind.BowedStrings,
            "Violin",
            "violin",
            "Violin",
            "Violin",
            "Strings/Bowed/Violin",
            40,
            "Violin-Solo",
            [55, 62, 69, 76],
            transposeOctave: 0,
            clef: "G2");

    private static GpTrackProfile CreateViola()
        => CreateStringProfile(
            InstrumentKind.Viola,
            InstrumentFamilyKind.BowedStrings,
            "Viola",
            "viola",
            "Viola",
            "Viola",
            "Strings/Bowed/Viola",
            41,
            "Viola-Solo",
            [48, 55, 62, 69],
            transposeOctave: 0,
            clef: "C3");

    private static GpTrackProfile CreateCello()
        => CreateStringProfile(
            InstrumentKind.Cello,
            InstrumentFamilyKind.BowedStrings,
            "Cello",
            "cello",
            "Cello",
            "Cello",
            "Strings/Bowed/Cello",
            42,
            "Cello-Solo",
            [36, 43, 50, 57],
            transposeOctave: 0,
            clef: "F4");

    private static GpTrackProfile CreateContrabass()
        => CreateStringProfile(
            InstrumentKind.Contrabass,
            InstrumentFamilyKind.BowedStrings,
            "Contrabass",
            "contrabass",
            "Contrabass",
            "Contrabass",
            "Strings/Bowed/Contrabass",
            43,
            "Contrabass-Solo",
            [28, 33, 38, 43],
            transposeOctave: -1,
            clef: "F4");

    private static GpTrackProfile CreateGenericPitched()
        => new()
        {
            Family = InstrumentFamilyKind.GenericPitched,
            Kind = InstrumentKind.GenericPitched,
            Role = TrackRoleKind.Pitched,
            InstrumentSetName = "Pitched",
            InstrumentSetType = "pitched",
            InstrumentSetLineCount = 5,
            InstrumentElementName = "Pitched",
            InstrumentElementType = "pitched",
            SoundName = "Ac Piano",
            SoundLabel = "Ac Piano",
            SoundPath = "Keyboards/Pianos/Acoustic Grand Piano",
            MidiProgram = 1,
            SoundbankPatch = "German-APiano",
            SoundbankSet = "Factory",
            ChannelBank = "German-APiano",
            ChannelStripVersion = "E56",
            ChannelStripParameters = "0.5 0.5 0.5 0.5 0.5 0.5 0.5 0.5 0.5 0 0.5 0.5 0.795 0.5 0.5 0.5",
            ChannelStripAutomations = CreateDefaultChannelStripAutomations(),
            PlayingStyle = "Default",
            PlaybackState = "Default",
            AudioEngineState = "RSE",
            PrimaryChannel = 0,
            SecondaryChannel = 1,
            ForceOneChannelPerString = false,
            Clef = "G2"
        };

    private static GpTrackProfile CreateStringProfile(
        InstrumentKind kind,
        InstrumentFamilyKind family,
        string instrumentSetName,
        string instrumentSetType,
        string soundName,
        string soundLabel,
        string soundPath,
        int midiProgram,
        string soundbankPatch,
        int[] tuningPitches,
        int transposeOctave,
        string clef)
    {
        var isFrettedString = family is InstrumentFamilyKind.Guitar
            or InstrumentFamilyKind.Bass
            or InstrumentFamilyKind.Ukulele
            or InstrumentFamilyKind.Mandolin
            or InstrumentFamilyKind.Banjo;

        return new GpTrackProfile
        {
            Family = family,
            Kind = kind,
            Role = TrackRoleKind.Pitched,
            InstrumentSetName = instrumentSetName,
            InstrumentSetType = instrumentSetType,
            InstrumentSetLineCount = 5,
            InstrumentElementName = "Pitched",
            InstrumentElementType = "pitched",
            SoundName = soundName,
            SoundLabel = soundLabel,
            SoundPath = soundPath,
            MidiProgram = midiProgram,
            SoundbankPatch = soundbankPatch,
            SoundbankSet = "Factory",
            ChannelBank = soundbankPatch,
            ChannelStripVersion = "E56",
            ChannelStripParameters = "0.5 0.5 0.5 0.5 0.5 0.5 0.5 0.5 0.5 0 0.5 0.5 0.795 0.5 0.5 0.5",
            ChannelStripAutomations = CreateDefaultChannelStripAutomations(),
            SoundRsePickupsOverloudPosition = "0",
            SoundRsePickupsVolumes = "1 1",
            SoundRsePickupsTones = "1 1",
            TrackColor = isFrettedString ? "237 116 116" : string.Empty,
            SystemsDefaultLayout = isFrettedString ? "3" : string.Empty,
            SystemsLayout = isFrettedString ? "2" : string.Empty,
            AutoBrush = isFrettedString,
            PalmMute = isFrettedString ? 0.3m : null,
            AutoAccentuation = isFrettedString ? 0.2m : null,
            PlayingStyle = "StringedPick",
            UseOneChannelPerString = isFrettedString,
            IconId = isFrettedString ? 1 : null,
            ForcedSound = isFrettedString ? -1 : null,
            PlaybackState = "Default",
            AudioEngineState = "RSE",
            LyricsDispatched = isFrettedString,
            DefaultLyricsLineCount = isFrettedString ? 5 : 0,
            InstrumentArticulations = isFrettedString
                ? CreateDefaultPitchedArticulations()
                : Array.Empty<InstrumentArticulationMetadata>(),
            SoundEffectChain = kind == InstrumentKind.SteelStringGuitar
                ? CreateSteelStringEffectChain()
                : Array.Empty<RseEffectMetadata>(),
            PrimaryChannel = 0,
            SecondaryChannel = 1,
            ForceOneChannelPerString = false,
            DefaultTuningPitches = tuningPitches,
            DefaultTuningLabel = string.Empty,
            DefaultTuningName = isFrettedString ? "Standard" : string.Empty,
            TuningInstrument = family switch
            {
                InstrumentFamilyKind.Guitar => "Guitar",
                InstrumentFamilyKind.Bass => "Bass",
                InstrumentFamilyKind.BowedStrings => instrumentSetName,
                _ => instrumentSetName
            },
            DefaultCapoFret = isFrettedString ? 0 : null,
            DefaultFretCount = isFrettedString ? 24 : null,
            DefaultPartialCapoFret = isFrettedString ? 0 : null,
            IncludeChordCollection = isFrettedString,
            IncludeChordWorkingSet = isFrettedString,
            IncludeDiagramCollection = isFrettedString,
            IncludeDiagramWorkingSet = isFrettedString,
            IncludeTuningFlatElement = isFrettedString,
            IncludeTuningFlatProperty = isFrettedString,
            TransposeChromatic = 0,
            TransposeOctave = transposeOctave,
            Clef = clef
        };
    }

    private static GpTrackProfile CreateKeyboardProfile(
        InstrumentKind kind,
        InstrumentFamilyKind family,
        string instrumentSetName,
        string instrumentSetType,
        string soundName,
        int midiProgram,
        string soundbankPatch)
        => new()
        {
            Family = family,
            Kind = kind,
            Role = TrackRoleKind.Pitched,
            InstrumentSetName = instrumentSetName,
            InstrumentSetType = instrumentSetType,
            InstrumentSetLineCount = 5,
            InstrumentElementName = "Pitched",
            InstrumentElementType = "pitched",
            SoundName = soundName,
            SoundLabel = instrumentSetName,
            SoundPath = $"Keyboards/{instrumentSetName}",
            MidiProgram = midiProgram,
            SoundbankPatch = soundbankPatch,
            SoundbankSet = "Factory",
            ChannelBank = soundbankPatch,
            ChannelStripVersion = "E56",
            ChannelStripParameters = "0.5 0.5 0.5 0.5 0.5 0.5 0.5 0.5 0.5 0 0.5 0.5 0.795 0.5 0.5 0.5",
            ChannelStripAutomations = CreateDefaultChannelStripAutomations(),
            PlayingStyle = "Default",
            PlaybackState = "Default",
            AudioEngineState = "RSE",
            PrimaryChannel = 0,
            SecondaryChannel = 1,
            ForceOneChannelPerString = false,
            Clef = "G2"
        };

    private static InstrumentArticulationMetadata[] CreateDefaultPitchedArticulations()
        =>
        [
            new InstrumentArticulationMetadata
            {
                Name = string.Empty,
                StaffLine = 0,
                Noteheads = "noteheadBlack noteheadHalf noteheadWhole",
                TechniquePlacement = "outside",
                TechniqueSymbol = string.Empty,
                InputMidiNumbers = string.Empty,
                OutputRseSound = string.Empty,
                OutputMidiNumber = 0
            }
        ];

    private static RseEffectMetadata[] CreateSteelStringEffectChain()
        =>
        [
            new RseEffectMetadata
            {
                Id = "E30_EqGEq",
                Parameters = "0.5 0.5 0.5 0.5 0.5 0.5 0.5 0.685714"
            },
            new RseEffectMetadata
            {
                Id = "M07_DynamicClassicDynamic",
                Parameters = "0.5 0.5 1"
            },
            new RseEffectMetadata
            {
                Id = "M04_StudioReverbRoomAmbience",
                Parameters = "1 0.43 0.4 0.5 0.2"
            }
        ];

    private static AutomationMetadata[] CreateDefaultChannelStripAutomations()
        =>
        [
            new AutomationMetadata
            {
                Type = "DSPParam_11",
                Linear = false,
                Bar = 0,
                Position = 0,
                Visible = true,
                Value = "0.5"
            },
            new AutomationMetadata
            {
                Type = "DSPParam_12",
                Linear = false,
                Bar = 0,
                Position = 0,
                Visible = true,
                Value = "0.795"
            }
        ];
}

internal sealed class GpTrackProfile
{
    public InstrumentFamilyKind Family { get; init; }

    public InstrumentKind Kind { get; init; }

    public TrackRoleKind Role { get; init; }

    public string InstrumentSetName { get; init; } = string.Empty;

    public string InstrumentSetType { get; init; } = string.Empty;

    public int InstrumentSetLineCount { get; init; } = 5;

    public string InstrumentElementName { get; init; } = string.Empty;

    public string InstrumentElementType { get; init; } = string.Empty;

    public string InstrumentElementSoundbankName { get; init; } = string.Empty;

    public string SoundName { get; init; } = string.Empty;

    public string SoundLabel { get; init; } = string.Empty;

    public string SoundPath { get; init; } = string.Empty;

    public int MidiProgram { get; init; }

    public string SoundbankPatch { get; init; } = string.Empty;

    public string SoundbankSet { get; init; } = string.Empty;

    public string ChannelBank { get; init; } = string.Empty;

    public string ChannelStripVersion { get; init; } = string.Empty;

    public string ChannelStripParameters { get; init; } = string.Empty;

    public IReadOnlyList<AutomationMetadata> ChannelStripAutomations { get; init; } = Array.Empty<AutomationMetadata>();

    public string SoundRsePickupsOverloudPosition { get; init; } = string.Empty;

    public string SoundRsePickupsVolumes { get; init; } = string.Empty;

    public string SoundRsePickupsTones { get; init; } = string.Empty;

    public string TrackColor { get; init; } = string.Empty;

    public string SystemsDefaultLayout { get; init; } = string.Empty;

    public string SystemsLayout { get; init; } = string.Empty;

    public bool AutoBrush { get; init; }

    public decimal? PalmMute { get; init; }

    public decimal? AutoAccentuation { get; init; }

    public string PlayingStyle { get; init; } = string.Empty;

    public bool UseOneChannelPerString { get; init; }

    public int? IconId { get; init; }

    public int? ForcedSound { get; init; }

    public string PlaybackState { get; init; } = string.Empty;

    public string AudioEngineState { get; init; } = string.Empty;

    public bool LyricsDispatched { get; init; }

    public int DefaultLyricsLineCount { get; init; }

    public IReadOnlyList<InstrumentArticulationMetadata> InstrumentArticulations { get; init; } = Array.Empty<InstrumentArticulationMetadata>();

    public IReadOnlyList<RseEffectMetadata> SoundEffectChain { get; init; } = Array.Empty<RseEffectMetadata>();

    public int PrimaryChannel { get; init; }

    public int SecondaryChannel { get; init; }

    public bool ForceOneChannelPerString { get; init; }

    public int[] DefaultTuningPitches { get; init; } = Array.Empty<int>();

    public string DefaultTuningLabel { get; init; } = string.Empty;

    public string DefaultTuningName { get; init; } = string.Empty;

    public string TuningInstrument { get; init; } = string.Empty;

    public int? DefaultCapoFret { get; init; }

    public int? DefaultFretCount { get; init; }

    public int? DefaultPartialCapoFret { get; init; }

    public bool IncludeChordCollection { get; init; }

    public bool IncludeChordWorkingSet { get; init; }

    public bool IncludeDiagramCollection { get; init; }

    public bool IncludeDiagramWorkingSet { get; init; }

    public bool IncludeTuningFlatElement { get; init; }

    public bool IncludeTuningFlatProperty { get; init; }

    public int TransposeChromatic { get; init; }

    public int TransposeOctave { get; init; }

    public string Clef { get; init; } = string.Empty;

    public TrackInstrument ToInstrument()
        => new()
        {
            Family = Family,
            Kind = Kind,
            Role = Role
        };
}
