namespace Motif.Extensions.GuitarPro.Implementation;

using Motif.Extensions.GuitarPro.Models;
using Motif.Extensions.GuitarPro.Models.Write;
using Motif.Models;
using System.Globalization;

internal static class GpExportDefaultsResolver
{
    public static ScoreMetadata ResolveScoreMetadata(Score score, WriteDiagnostics diagnostics)
    {
        var existing = score.GetGuitarPro()?.Metadata;
        if (existing is not null)
        {
            return CloneScoreMetadata(existing);
        }

        var resolved = GpTemplateDefaults.CreateScoreMetadata();
        diagnostics.Info(
            code: "GP_TEMPLATE_DEFAULTS_APPLIED",
            category: "Defaults",
            message: "Source-free GP export seeded score-level defaults from the embedded Guitar Pro template.");
        return resolved;
    }

    public static MasterTrackMetadata ResolveMasterTrackMetadata(Score score, IReadOnlyList<Track> orderedTracks, WriteDiagnostics diagnostics)
    {
        var existing = score.GetGuitarPro()?.MasterTrack;
        var resolved = existing is not null
            ? CloneMasterTrackMetadata(existing)
            : GpTemplateDefaults.CreateMasterTrackMetadata();

        resolved.TrackIds = orderedTracks.Select(track => track.Id).ToArray();
        resolved.Anacrusis = score.Anacrusis;
        resolved.Automations = ResolveMasterTrackAutomations(score, resolved, diagnostics, existing is not null);
        resolved.TempoMap = resolved.Automations
            .Where(automation => string.Equals(automation.Type, "Tempo", StringComparison.OrdinalIgnoreCase))
            .Select(automation => new TempoEventMetadata
            {
                Bar = automation.Bar,
                Position = automation.Position,
                Bpm = ParseBpm(automation.Value),
                DenominatorHint = 2
            })
            .ToArray();

        return resolved;
    }

    public static TrackMetadata ResolveTrackMetadata(Track track, WriteDiagnostics diagnostics)
    {
        var existing = track.GetGuitarPro()?.Metadata;
        var profile = GpTrackProfileCatalog.ResolveProfile(track, existing ?? new TrackMetadata());

        if (existing is not null)
        {
            return CloneTrackMetadata(existing);
        }

        var resolved = CreateTrackMetadataFromProfile(track, profile);
        ApplyCoreTrackSemantics(track, resolved);
        var path = $"/Score/Tracks[@id='{track.Id}']";
        diagnostics.Info(
            code: "GP_DEFAULT_INSTRUMENT_PROFILE_APPLIED",
            category: "Defaults",
            message: $"Source-free GP export applied the '{profile.Kind}' track profile.",
            path: path,
            outputValue: profile.Kind.ToString());

        if (profile.Kind == InstrumentKind.GenericPitched)
        {
            diagnostics.Info(
                code: "GP_GENERIC_PROFILE_FALLBACK",
                category: "Defaults",
                message: "Source-free GP export fell back to the generic pitched track profile because no more specific instrument semantics were available.",
                path: path);
        }

        return resolved;
    }

    public static IReadOnlyList<StaffMetadata> ResolveStaffMetadata(Track track, TrackMetadata resolvedTrackMetadata, WriteDiagnostics diagnostics)
    {
        var trackExtension = track.GetGuitarPro();
        if (trackExtension is not null)
        {
            if (track.Staves.Count == 0)
            {
                return trackExtension.Metadata.Staffs.Select(CloneStaffMetadata).ToArray();
            }

            var preservedStaffs = new List<StaffMetadata>(track.Staves.Count);
            foreach (var staff in track.Staves.OrderBy(staff => staff.StaffIndex))
            {
                if (staff.GetGuitarPro() is { } extension)
                {
                    preservedStaffs.Add(CloneStaffMetadata(extension.Metadata));
                    continue;
                }

                if (staff.StaffIndex >= 0 && staff.StaffIndex < trackExtension.Metadata.Staffs.Count)
                {
                    preservedStaffs.Add(CloneStaffMetadata(trackExtension.Metadata.Staffs[staff.StaffIndex]));
                }
                else
                {
                    preservedStaffs.Add(new StaffMetadata());
                }
            }

            return preservedStaffs;
        }

        var profile = GpTrackProfileCatalog.ResolveProfile(track, new TrackMetadata());

        if (track.Staves.Count == 0)
        {
            if (resolvedTrackMetadata.Staffs.Count > 0)
            {
                return resolvedTrackMetadata.Staffs.Select(CloneStaffMetadata).ToArray();
            }

            return profile.DefaultTuningPitches.Length > 0
                ? [CreateStaffMetadataFromProfile(profile)]
                : Array.Empty<StaffMetadata>();
        }

        var current = new List<StaffMetadata>(track.Staves.Count);
        foreach (var staff in track.Staves.OrderBy(staff => staff.StaffIndex))
        {
            var sourceTrackStaffs = trackExtension?.Metadata.Staffs ?? Array.Empty<StaffMetadata>();
            var resolved = staff.GetGuitarPro() is { } extension
                ? CloneStaffMetadata(extension.Metadata)
                : staff.StaffIndex >= 0 && staff.StaffIndex < sourceTrackStaffs.Count
                    ? CloneStaffMetadata(sourceTrackStaffs[staff.StaffIndex])
                    : CreateStaffMetadataFromProfile(profile);

            ApplyCoreStaffSemantics(staff, resolved, profile, diagnostics, track.Id);
            current.Add(resolved);
        }

        return current;
    }

    public static string ResolveClef(Track track, int staffIndex, string currentClef)
    {
        if (!string.IsNullOrWhiteSpace(currentClef) || track.GetGuitarPro() is not null)
        {
            return currentClef;
        }

        var profile = GpTrackProfileCatalog.ResolveProfile(track, track.GetGuitarPro()?.Metadata ?? new TrackMetadata());
        return profile.Clef;
    }

    private static IReadOnlyList<AutomationMetadata> ResolveMasterTrackAutomations(
        Score score,
        MasterTrackMetadata resolved,
        WriteDiagnostics diagnostics,
        bool hasSourceMetadata)
    {
        if (hasSourceMetadata && !HasExplicitCoreTempoChanges(score, resolved))
        {
            return resolved.Automations.Select(CloneAutomation).ToArray();
        }

        var preservedNonTempoAutomations = resolved.Automations
            .Where(automation => !string.Equals(automation.Type, "Tempo", StringComparison.OrdinalIgnoreCase))
            .Select(CloneAutomation)
            .ToList();

        if (score.TempoChanges.Count > 0)
        {
            var tempoAutomations = score.TempoChanges
                .OrderBy(change => change.BarIndex)
                .ThenBy(change => change.Position)
                .Select(change => new AutomationMetadata
                {
                    Type = "Tempo",
                    Linear = false,
                    Bar = change.BarIndex,
                    Position = change.Position,
                    Visible = true,
                    Value = $"{change.BeatsPerMinute.ToString(CultureInfo.InvariantCulture)} 2"
                });

            resolved.AutomationsXml = string.Empty;
            preservedNonTempoAutomations.AddRange(tempoAutomations);
            return preservedNonTempoAutomations;
        }

        if (resolved.Automations.Any(automation => string.Equals(automation.Type, "Tempo", StringComparison.OrdinalIgnoreCase)))
        {
            return resolved.Automations.Select(CloneAutomation).ToArray();
        }

        if (hasSourceMetadata)
        {
            resolved.AutomationsXml = string.Empty;
            return preservedNonTempoAutomations;
        }

        diagnostics.Info(
            code: "GP_DEFAULT_TEMPO_APPLIED",
            category: "Defaults",
            message: "Source-free GP export defaulted the score tempo to 120 BPM because no core tempo changes were provided.");
        resolved.AutomationsXml = string.Empty;
        preservedNonTempoAutomations.Add(new AutomationMetadata
        {
            Type = "Tempo",
            Linear = false,
            Bar = 0,
            Position = 0,
            Visible = true,
            Value = "120 2"
        });
        return preservedNonTempoAutomations;
    }

    private static bool HasExplicitCoreTempoChanges(Score score, MasterTrackMetadata resolved)
    {
        var coreTempos = score.TempoChanges
            .OrderBy(change => change.BarIndex)
            .ThenBy(change => change.Position)
            .Select(change => $"{change.BarIndex}|{change.Position}|{change.BeatsPerMinute.ToString(CultureInfo.InvariantCulture)}")
            .ToArray();
        var sourceTempos = resolved.TempoMap
            .OrderBy(change => change.Bar ?? 0)
            .ThenBy(change => change.Position ?? 0)
            .Select(change => $"{change.Bar ?? 0}|{change.Position ?? 0}|{(change.Bpm ?? 0m).ToString(CultureInfo.InvariantCulture)}")
            .ToArray();

        return !coreTempos.SequenceEqual(sourceTempos, StringComparer.Ordinal);
    }

    private static void FillMissingTrackDefaults(TrackMetadata target, GpTrackProfile profile)
    {
        if (string.IsNullOrWhiteSpace(target.PlayingStyle))
        {
            target.PlayingStyle = profile.PlayingStyle;
        }

        if (target.InstrumentSet is null || string.IsNullOrWhiteSpace(target.InstrumentSet.Name))
        {
            target.InstrumentSet = CreateInstrumentSet(profile);
        }

        if (target.Sounds.Count == 0)
        {
            target.Sounds = [CreateSound(profile)];
            target.Automations = [CreateSoundAutomation(profile)];
            target.PlaybackState = new PlaybackStateMetadata { Value = profile.PlaybackState };
            target.AudioEngineState = new AudioEngineStateMetadata { Value = profile.AudioEngineState };
            target.MidiConnection = new MidiConnectionMetadata
            {
                Port = 0,
                PrimaryChannel = profile.PrimaryChannel,
                SecondaryChannel = profile.SecondaryChannel,
                ForceOneChannelPerString = profile.ForceOneChannelPerString
            };
            target.Rse = new RseMetadata
            {
                Bank = profile.ChannelBank,
                ChannelStripVersion = profile.ChannelStripVersion,
                ChannelStripParameters = profile.ChannelStripParameters
            };
        }

        if ((target.Transpose.Chromatic is null && target.Transpose.Octave is null)
            && (profile.TransposeChromatic != 0 || profile.TransposeOctave != 0))
        {
            target.Transpose = new TransposeMetadata
            {
                Chromatic = profile.TransposeChromatic,
                Octave = profile.TransposeOctave
            };
        }

        if (target.TuningPitches.Length == 0 && profile.DefaultTuningPitches.Length > 0)
        {
            target.TuningPitches = profile.DefaultTuningPitches.ToArray();
            target.TuningInstrument = profile.TuningInstrument;
            target.TuningLabel = profile.DefaultTuningLabel;
            target.TuningLabelVisible = true;
            target.HasTrackTuningProperty = true;
        }

        if (target.Staffs.Count == 0 && profile.DefaultTuningPitches.Length > 0)
        {
            target.Staffs = [CreateStaffMetadataFromProfile(profile)];
        }
    }

    private static TrackMetadata CreateTrackMetadataFromProfile(Track track, GpTrackProfile profile)
    {
        var staffMetadata = profile.DefaultTuningPitches.Length > 0
            ? [CreateStaffMetadataFromProfile(profile)]
            : Array.Empty<StaffMetadata>();

        return new TrackMetadata
        {
            ShortName = CreateShortName(track.Name),
            Color = profile.TrackColor,
            SystemsDefaultLayout = profile.SystemsDefaultLayout,
            SystemsLayout = profile.SystemsLayout,
            PalmMute = profile.PalmMute,
            AutoAccentuation = profile.AutoAccentuation,
            AutoBrush = profile.AutoBrush,
            PlayingStyle = profile.PlayingStyle,
            UseOneChannelPerString = profile.UseOneChannelPerString,
            IconId = profile.IconId,
            ForcedSound = profile.ForcedSound,
            InstrumentSet = CreateInstrumentSet(profile),
            Sounds = [CreateSound(profile)],
            Rse = new RseMetadata
            {
                Bank = profile.ChannelBank,
                ChannelStripVersion = profile.ChannelStripVersion,
                ChannelStripParameters = profile.ChannelStripParameters,
                Automations = profile.ChannelStripAutomations.Select(CloneAutomation).ToArray()
            },
            PlaybackState = new PlaybackStateMetadata
            {
                Value = profile.PlaybackState
            },
            AudioEngineState = new AudioEngineStateMetadata
            {
                Value = profile.AudioEngineState
            },
            MidiConnection = new MidiConnectionMetadata
            {
                Port = 0,
                PrimaryChannel = profile.PrimaryChannel,
                SecondaryChannel = profile.SecondaryChannel,
                ForceOneChannelPerString = profile.ForceOneChannelPerString
            },
            Lyrics = CreateLyrics(profile),
            Transpose = new TransposeMetadata
            {
                Chromatic = profile.TransposeChromatic,
                Octave = profile.TransposeOctave
            },
            Automations = [CreateSoundAutomation(profile)],
            Staffs = staffMetadata
        };
    }

    private static void ApplyCoreTrackSemantics(Track track, TrackMetadata target)
    {
        if (HasExplicitCoreTransposition(track))
        {
            target.TransposeXml = string.Empty;
            target.Transpose = new TransposeMetadata
            {
                Chromatic = track.Transposition.Chromatic,
                Octave = track.Transposition.Octave
            };
        }

        if (track.Staves.Count == 1 && track.Staves[0].Tuning.Pitches.Count > 0)
        {
            target.HasTrackTuningProperty = false;
            target.TuningPitches = Array.Empty<int>();
            target.TuningInstrument = string.Empty;
            target.TuningLabel = string.Empty;
            target.TuningLabelVisible = null;
        }
    }

    private static InstrumentSetMetadata CreateInstrumentSet(GpTrackProfile profile)
        => new()
        {
            Name = profile.InstrumentSetName,
            Type = profile.InstrumentSetType,
            LineCount = profile.InstrumentSetLineCount,
            Elements =
            [
                new InstrumentElementMetadata
                {
                    Name = profile.InstrumentElementName,
                    Type = profile.InstrumentElementType,
                    SoundbankName = profile.InstrumentElementSoundbankName,
                    Articulations = profile.InstrumentArticulations.Select(articulation => new InstrumentArticulationMetadata
                    {
                        Name = articulation.Name,
                        StaffLine = articulation.StaffLine,
                        Noteheads = articulation.Noteheads,
                        TechniquePlacement = articulation.TechniquePlacement,
                        TechniqueSymbol = articulation.TechniqueSymbol,
                        InputMidiNumbers = articulation.InputMidiNumbers,
                        OutputRseSound = articulation.OutputRseSound,
                        OutputMidiNumber = articulation.OutputMidiNumber
                    }).ToArray()
                }
            ]
        };

    private static SoundMetadata CreateSound(GpTrackProfile profile)
        => new()
        {
            Name = profile.SoundName,
            Label = profile.SoundLabel,
            Path = profile.SoundPath,
            Role = "Factory",
            MidiLsb = 0,
            MidiMsb = 0,
            MidiProgram = profile.MidiProgram,
            Rse = new SoundRseMetadata
            {
                SoundbankPatch = profile.SoundbankPatch,
                SoundbankSet = profile.SoundbankSet,
                Pickups = new SoundRsePickupsMetadata
                {
                    OverloudPosition = profile.SoundRsePickupsOverloudPosition,
                    Volumes = profile.SoundRsePickupsVolumes,
                    Tones = profile.SoundRsePickupsTones
                },
                EffectChain = profile.SoundEffectChain.Select(effect => new RseEffectMetadata
                {
                    Id = effect.Id,
                    Bypass = effect.Bypass,
                    Parameters = effect.Parameters
                }).ToArray()
            }
        };

    private static LyricsMetadata CreateLyrics(GpTrackProfile profile)
        => new()
        {
            Dispatched = profile.LyricsDispatched,
            Lines = Enumerable.Range(0, profile.DefaultLyricsLineCount)
                .Select(_ => new LyricsLineMetadata
                {
                    Text = string.Empty,
                    Offset = 0
                })
                .ToArray()
        };

    private static AutomationMetadata CreateSoundAutomation(GpTrackProfile profile)
        => new()
        {
            Type = "Sound",
            Linear = false,
            Bar = 0,
            Position = 0,
            Visible = true,
            Value = $"{profile.SoundPath};{profile.SoundLabel};Factory"
        };

    private static StaffMetadata CreateStaffMetadataFromProfile(GpTrackProfile profile)
    {
        var properties = new Dictionary<string, string>();
        if (profile.DefaultTuningPitches.Length > 0)
        {
            properties["Tuning"] = string.Join(' ', profile.DefaultTuningPitches);
        }

        if (profile.DefaultCapoFret.HasValue)
        {
            properties["CapoFret"] = profile.DefaultCapoFret.Value.ToString(CultureInfo.InvariantCulture);
        }

        return new StaffMetadata
        {
            TuningPitches = profile.DefaultTuningPitches.ToArray(),
            TuningInstrument = profile.TuningInstrument,
            TuningLabel = profile.DefaultTuningLabel,
            TuningLabelVisible = profile.DefaultTuningPitches.Length > 0 ? true : null,
            EmitTuningFlatElement = profile.IncludeTuningFlatElement,
            EmitTuningFlatProperty = profile.IncludeTuningFlatProperty,
            CapoFret = profile.DefaultCapoFret,
            FretCount = profile.DefaultFretCount,
            PartialCapoFret = profile.DefaultPartialCapoFret,
            PartialCapoStringFlags = profile.IncludeTuningFlatProperty && profile.DefaultTuningPitches.Length > 0
                ? new string('0', profile.DefaultTuningPitches.Length)
                : string.Empty,
            EmitChordCollection = profile.IncludeChordCollection,
            EmitChordWorkingSet = profile.IncludeChordWorkingSet,
            EmitDiagramCollection = profile.IncludeDiagramCollection,
            EmitDiagramWorkingSet = profile.IncludeDiagramWorkingSet,
            Name = profile.DefaultTuningName,
            Properties = properties
        };
    }

    private static void ApplyCoreStaffSemantics(Staff staff, StaffMetadata target, GpTrackProfile profile, WriteDiagnostics diagnostics, int trackId)
    {
        if (staff.Tuning.Pitches.Count > 0)
        {
            target.TuningPitches = staff.Tuning.Pitches.ToArray();
            target.Properties = target.Properties
                .Where(pair => !string.Equals(pair.Key, "Tuning", StringComparison.OrdinalIgnoreCase))
                .ToDictionary(pair => pair.Key, pair => pair.Value);
            ((Dictionary<string, string>)target.Properties)["Tuning"] = string.Join(' ', target.TuningPitches);
            target.TuningInstrument = string.IsNullOrWhiteSpace(target.TuningInstrument)
                ? profile.TuningInstrument
                : target.TuningInstrument;
            target.TuningLabelVisible ??= true;
        }
        else if (target.TuningPitches.Length == 0 && profile.DefaultTuningPitches.Length > 0)
        {
            target.TuningPitches = profile.DefaultTuningPitches.ToArray();
            target.Properties = target.Properties
                .Where(pair => !string.Equals(pair.Key, "Tuning", StringComparison.OrdinalIgnoreCase))
                .ToDictionary(pair => pair.Key, pair => pair.Value);
            ((Dictionary<string, string>)target.Properties)["Tuning"] = string.Join(' ', target.TuningPitches);

            diagnostics.Info(
                code: "GP_DEFAULT_TUNING_APPLIED",
                category: "Defaults",
                message: "Source-free GP export applied the profile's default tuning because the staff did not define one.",
                path: $"/Score/Tracks[@id='{trackId}']/Staves[@index='{staff.StaffIndex}']");
        }

        if (!string.IsNullOrWhiteSpace(staff.Tuning.Label))
        {
            target.Name = staff.Tuning.Label;
        }
        else if (string.IsNullOrWhiteSpace(target.Name))
        {
            target.Name = profile.DefaultTuningName;
        }

        if (staff.CapoFret.HasValue)
        {
            target.CapoFret = staff.CapoFret;
            target.PartialCapoFret = staff.CapoFret;
            target.Properties = target.Properties
                .Where(pair => !string.Equals(pair.Key, "CapoFret", StringComparison.OrdinalIgnoreCase))
                .ToDictionary(pair => pair.Key, pair => pair.Value);
            ((Dictionary<string, string>)target.Properties)["CapoFret"] = staff.CapoFret.Value.ToString(CultureInfo.InvariantCulture);
        }
    }

    private static string CreateShortName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        var trimmed = name.Trim();
        return trimmed.Length <= 12
            ? trimmed
            : trimmed[..12];
    }

    private static decimal? ParseBpm(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var firstToken = value.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return decimal.TryParse(firstToken, NumberStyles.Number, CultureInfo.InvariantCulture, out var bpm)
            ? bpm
            : null;
    }

    private static bool HasExplicitCoreTransposition(Track track)
        => track.Transposition.IsSpecified
           || track.Transposition.Chromatic != 0
           || track.Transposition.Octave != 0;

    private static void MergeScoreMetadata(ScoreMetadata target, ScoreMetadata source)
    {
        if (!string.IsNullOrWhiteSpace(source.ScoreXml)) target.ScoreXml = source.ScoreXml;
        if (source.ExplicitEmptyOptionalElements.Length > 0) target.ExplicitEmptyOptionalElements = source.ExplicitEmptyOptionalElements.ToArray();
        if (!string.IsNullOrWhiteSpace(source.GpVersion)) target.GpVersion = source.GpVersion;
        if (!string.IsNullOrWhiteSpace(source.GpRevisionXml)) target.GpRevisionXml = source.GpRevisionXml;
        if (!string.IsNullOrWhiteSpace(source.GpRevisionRequired)) target.GpRevisionRequired = source.GpRevisionRequired;
        if (!string.IsNullOrWhiteSpace(source.GpRevisionRecommended)) target.GpRevisionRecommended = source.GpRevisionRecommended;
        if (!string.IsNullOrWhiteSpace(source.GpRevisionValue)) target.GpRevisionValue = source.GpRevisionValue;
        if (!string.IsNullOrWhiteSpace(source.EncodingDescription)) target.EncodingDescription = source.EncodingDescription;
        if (!string.IsNullOrWhiteSpace(source.ScoreViewsXml)) target.ScoreViewsXml = source.ScoreViewsXml;
        if (!string.IsNullOrWhiteSpace(source.SubTitle)) target.SubTitle = source.SubTitle;
        if (!string.IsNullOrWhiteSpace(source.Words)) target.Words = source.Words;
        if (!string.IsNullOrWhiteSpace(source.Music)) target.Music = source.Music;
        if (!string.IsNullOrWhiteSpace(source.WordsAndMusic)) target.WordsAndMusic = source.WordsAndMusic;
        if (!string.IsNullOrWhiteSpace(source.Copyright)) target.Copyright = source.Copyright;
        if (!string.IsNullOrWhiteSpace(source.Tabber)) target.Tabber = source.Tabber;
        if (!string.IsNullOrWhiteSpace(source.Instructions)) target.Instructions = source.Instructions;
        if (!string.IsNullOrWhiteSpace(source.Notices)) target.Notices = source.Notices;
        if (!string.IsNullOrWhiteSpace(source.FirstPageHeader)) target.FirstPageHeader = source.FirstPageHeader;
        if (!string.IsNullOrWhiteSpace(source.FirstPageFooter)) target.FirstPageFooter = source.FirstPageFooter;
        if (!string.IsNullOrWhiteSpace(source.PageHeader)) target.PageHeader = source.PageHeader;
        if (!string.IsNullOrWhiteSpace(source.PageFooter)) target.PageFooter = source.PageFooter;
        if (!string.IsNullOrWhiteSpace(source.ScoreSystemsDefaultLayout)) target.ScoreSystemsDefaultLayout = source.ScoreSystemsDefaultLayout;
        if (!string.IsNullOrWhiteSpace(source.ScoreSystemsLayout)) target.ScoreSystemsLayout = source.ScoreSystemsLayout;
        if (!string.IsNullOrWhiteSpace(source.ScoreZoomPolicy)) target.ScoreZoomPolicy = source.ScoreZoomPolicy;
        if (!string.IsNullOrWhiteSpace(source.ScoreZoom)) target.ScoreZoom = source.ScoreZoom;
        if (!string.IsNullOrWhiteSpace(source.PageSetupXml)) target.PageSetupXml = source.PageSetupXml;
        if (!string.IsNullOrWhiteSpace(source.MultiVoice)) target.MultiVoice = source.MultiVoice;
        if (!string.IsNullOrWhiteSpace(source.BackingTrackXml)) target.BackingTrackXml = source.BackingTrackXml;
        if (!string.IsNullOrWhiteSpace(source.AudioTracksXml)) target.AudioTracksXml = source.AudioTracksXml;
        if (!string.IsNullOrWhiteSpace(source.AssetsXml)) target.AssetsXml = source.AssetsXml;
    }

    private static void MergeMasterTrackMetadata(MasterTrackMetadata target, MasterTrackMetadata source)
    {
        if (!string.IsNullOrWhiteSpace(source.Xml)) target.Xml = source.Xml;
        if (source.TrackIds.Length > 0) target.TrackIds = source.TrackIds.ToArray();
        if (!string.IsNullOrWhiteSpace(source.AutomationsXml)) target.AutomationsXml = source.AutomationsXml;
        if (source.Automations.Count > 0) target.Automations = source.Automations.Select(CloneAutomation).ToArray();
        if (source.AutomationTimeline.Count > 0) target.AutomationTimeline = source.AutomationTimeline.Select(CloneAutomationTimelineEvent).ToArray();
        if (source.DynamicMap.Count > 0) target.DynamicMap = source.DynamicMap.Select(CloneDynamicEvent).ToArray();
        target.Anacrusis = source.Anacrusis;
        if (!string.IsNullOrWhiteSpace(source.RseXml)) target.RseXml = source.RseXml;
        if (source.Rse.MasterEffects.Count > 0)
        {
            target.Rse = new MasterTrackRseMetadata
            {
                MasterEffects = source.Rse.MasterEffects.Select(effect => new RseEffectMetadata
                {
                    Id = effect.Id,
                    Bypass = effect.Bypass,
                    Parameters = effect.Parameters
                }).ToArray()
            };
        }
    }

    private static TrackMetadata CloneTrackMetadata(TrackMetadata source)
        => new()
        {
            Xml = source.Xml,
            ShortName = source.ShortName,
            HasExplicitEmptyShortName = source.HasExplicitEmptyShortName,
            Color = source.Color,
            SystemsDefaultLayout = source.SystemsDefaultLayout,
            SystemsLayout = source.SystemsLayout,
            HasExplicitEmptySystemsLayout = source.HasExplicitEmptySystemsLayout,
            PalmMute = source.PalmMute,
            AutoAccentuation = source.AutoAccentuation,
            AutoBrush = source.AutoBrush,
            LetRingThroughout = source.LetRingThroughout,
            PlayingStyle = source.PlayingStyle,
            UseOneChannelPerString = source.UseOneChannelPerString,
            IconId = source.IconId,
            ForcedSound = source.ForcedSound,
            TuningPitches = source.TuningPitches.ToArray(),
            TuningInstrument = source.TuningInstrument,
            TuningLabel = source.TuningLabel,
            TuningLabelVisible = source.TuningLabelVisible,
            HasTrackTuningProperty = source.HasTrackTuningProperty,
            Properties = source.Properties.ToDictionary(pair => pair.Key, pair => pair.Value),
            InstrumentSetXml = source.InstrumentSetXml,
            StavesXml = source.StavesXml,
            SoundsXml = source.SoundsXml,
            RseXml = source.RseXml,
            NotationPatchXml = source.NotationPatchXml,
            InstrumentSet = CloneInstrumentSet(source.InstrumentSet),
            Sounds = source.Sounds.Select(CloneSound).ToArray(),
            Rse = CloneRse(source.Rse),
            PlaybackStateXml = source.PlaybackStateXml,
            AudioEngineStateXml = source.AudioEngineStateXml,
            PlaybackState = new PlaybackStateMetadata { Value = source.PlaybackState.Value },
            AudioEngineState = new AudioEngineStateMetadata { Value = source.AudioEngineState.Value },
            Automations = source.Automations.Select(CloneAutomation).ToArray(),
            MidiConnectionXml = source.MidiConnectionXml,
            LyricsXml = source.LyricsXml,
            AutomationsXml = source.AutomationsXml,
            TransposeXml = source.TransposeXml,
            MidiConnection = new MidiConnectionMetadata
            {
                Port = source.MidiConnection.Port,
                PrimaryChannel = source.MidiConnection.PrimaryChannel,
                SecondaryChannel = source.MidiConnection.SecondaryChannel,
                ForceOneChannelPerString = source.MidiConnection.ForceOneChannelPerString
            },
            Lyrics = new LyricsMetadata
            {
                Dispatched = source.Lyrics.Dispatched,
                Lines = source.Lyrics.Lines.Select(line => new LyricsLineMetadata
                {
                    Text = line.Text,
                    Offset = line.Offset
                }).ToArray()
            },
            Transpose = new TransposeMetadata
            {
                Chromatic = source.Transpose.Chromatic,
                Octave = source.Transpose.Octave
            },
            Staffs = source.Staffs.Select(CloneStaffMetadata).ToArray()
        };

    private static ScoreMetadata CloneScoreMetadata(ScoreMetadata source)
        => new()
        {
            ScoreXml = source.ScoreXml,
            ExplicitEmptyOptionalElements = source.ExplicitEmptyOptionalElements.ToArray(),
            GpVersion = source.GpVersion,
            GpRevisionXml = source.GpRevisionXml,
            GpRevisionRequired = source.GpRevisionRequired,
            GpRevisionRecommended = source.GpRevisionRecommended,
            GpRevisionValue = source.GpRevisionValue,
            EncodingDescription = source.EncodingDescription,
            ScoreViewsXml = source.ScoreViewsXml,
            SubTitle = source.SubTitle,
            Words = source.Words,
            Music = source.Music,
            WordsAndMusic = source.WordsAndMusic,
            Copyright = source.Copyright,
            Tabber = source.Tabber,
            Instructions = source.Instructions,
            Notices = source.Notices,
            FirstPageHeader = source.FirstPageHeader,
            FirstPageFooter = source.FirstPageFooter,
            PageHeader = source.PageHeader,
            PageFooter = source.PageFooter,
            ScoreSystemsDefaultLayout = source.ScoreSystemsDefaultLayout,
            ScoreSystemsLayout = source.ScoreSystemsLayout,
            ScoreZoomPolicy = source.ScoreZoomPolicy,
            ScoreZoom = source.ScoreZoom,
            PageSetupXml = source.PageSetupXml,
            MultiVoice = source.MultiVoice,
            BackingTrackXml = source.BackingTrackXml,
            AudioTracksXml = source.AudioTracksXml,
            AssetsXml = source.AssetsXml
        };

    private static MasterTrackMetadata CloneMasterTrackMetadata(MasterTrackMetadata source)
        => new()
        {
            Xml = source.Xml,
            TrackIds = source.TrackIds.ToArray(),
            AutomationsXml = source.AutomationsXml,
            Automations = source.Automations.Select(CloneAutomation).ToArray(),
            AutomationTimeline = source.AutomationTimeline.Select(CloneAutomationTimelineEvent).ToArray(),
            DynamicMap = source.DynamicMap.Select(CloneDynamicEvent).ToArray(),
            Anacrusis = source.Anacrusis,
            RseXml = source.RseXml,
            Rse = new MasterTrackRseMetadata
            {
                MasterEffects = source.Rse.MasterEffects.Select(effect => new RseEffectMetadata
                {
                    Id = effect.Id,
                    Bypass = effect.Bypass,
                    Parameters = effect.Parameters
                }).ToArray()
            },
            TempoMap = source.TempoMap.Select(tempo => new TempoEventMetadata
            {
                Bar = tempo.Bar,
                Position = tempo.Position,
                Bpm = tempo.Bpm,
                DenominatorHint = tempo.DenominatorHint
            }).ToArray()
        };

    private static InstrumentSetMetadata CloneInstrumentSet(InstrumentSetMetadata source)
        => new()
        {
            Name = source.Name,
            Type = source.Type,
            LineCount = source.LineCount,
            Elements = source.Elements.Select(element => new InstrumentElementMetadata
            {
                Name = element.Name,
                Type = element.Type,
                SoundbankName = element.SoundbankName,
                Articulations = element.Articulations.Select(articulation => new InstrumentArticulationMetadata
                {
                    Name = articulation.Name,
                    StaffLine = articulation.StaffLine,
                    Noteheads = articulation.Noteheads,
                    TechniquePlacement = articulation.TechniquePlacement,
                    TechniqueSymbol = articulation.TechniqueSymbol,
                    InputMidiNumbers = articulation.InputMidiNumbers,
                    OutputRseSound = articulation.OutputRseSound,
                    OutputMidiNumber = articulation.OutputMidiNumber
                }).ToArray()
            }).ToArray()
        };

    private static SoundMetadata CloneSound(SoundMetadata source)
        => new()
        {
            Name = source.Name,
            Label = source.Label,
            Path = source.Path,
            Role = source.Role,
            MidiLsb = source.MidiLsb,
            MidiMsb = source.MidiMsb,
            MidiProgram = source.MidiProgram,
            Rse = new SoundRseMetadata
            {
                SoundbankPatch = source.Rse.SoundbankPatch,
                SoundbankSet = source.Rse.SoundbankSet,
                ElementsSettingsXml = source.Rse.ElementsSettingsXml,
                Pickups = new SoundRsePickupsMetadata
                {
                    OverloudPosition = source.Rse.Pickups.OverloudPosition,
                    Volumes = source.Rse.Pickups.Volumes,
                    Tones = source.Rse.Pickups.Tones
                },
                EffectChain = source.Rse.EffectChain.Select(effect => new RseEffectMetadata
                {
                    Id = effect.Id,
                    Bypass = effect.Bypass,
                    Parameters = effect.Parameters
                }).ToArray()
            }
        };

    private static RseMetadata CloneRse(RseMetadata source)
        => new()
        {
            Bank = source.Bank,
            ChannelStripVersion = source.ChannelStripVersion,
            ChannelStripParameters = source.ChannelStripParameters,
            Automations = source.Automations.Select(CloneAutomation).ToArray()
        };

    private static StaffMetadata CloneStaffMetadata(StaffMetadata source)
        => new()
        {
            Id = source.Id,
            Cref = source.Cref,
            TuningPitches = source.TuningPitches.ToArray(),
            TuningInstrument = source.TuningInstrument,
            TuningLabel = source.TuningLabel,
            TuningLabelVisible = source.TuningLabelVisible,
            EmitTuningFlatElement = source.EmitTuningFlatElement,
            EmitTuningFlatProperty = source.EmitTuningFlatProperty,
            CapoFret = source.CapoFret,
            FretCount = source.FretCount,
            PartialCapoFret = source.PartialCapoFret,
            PartialCapoStringFlags = source.PartialCapoStringFlags,
            EmitChordCollection = source.EmitChordCollection,
            EmitChordWorkingSet = source.EmitChordWorkingSet,
            EmitDiagramCollection = source.EmitDiagramCollection,
            EmitDiagramWorkingSet = source.EmitDiagramWorkingSet,
            Name = source.Name,
            Properties = source.Properties.ToDictionary(pair => pair.Key, pair => pair.Value),
            Xml = source.Xml
        };

    private static AutomationMetadata CloneAutomation(AutomationMetadata source)
        => new()
        {
            Type = source.Type,
            Linear = source.Linear,
            Bar = source.Bar,
            Position = source.Position,
            Visible = source.Visible,
            Value = source.Value
        };

    private static AutomationTimelineEventMetadata CloneAutomationTimelineEvent(AutomationTimelineEventMetadata source)
        => new()
        {
            Scope = source.Scope,
            TrackId = source.TrackId,
            Type = source.Type,
            Linear = source.Linear,
            Bar = source.Bar,
            Position = source.Position,
            Visible = source.Visible,
            Value = source.Value,
            NumericValue = source.NumericValue,
            ReferenceHint = source.ReferenceHint,
            Tempo = source.Tempo is null
                ? null
                : new TempoEventMetadata
                {
                    Bar = source.Tempo.Bar,
                    Position = source.Tempo.Position,
                    Bpm = source.Tempo.Bpm,
                    DenominatorHint = source.Tempo.DenominatorHint
                }
        };

    private static DynamicEventMetadata CloneDynamicEvent(DynamicEventMetadata source)
        => new()
        {
            TrackId = source.TrackId,
            MeasureIndex = source.MeasureIndex,
            VoiceIndex = source.VoiceIndex,
            BeatId = source.BeatId,
            BeatOffset = source.BeatOffset,
            Dynamic = source.Dynamic,
            Kind = source.Kind
        };
}
