namespace Motif.Extensions.GuitarPro.Implementation;

using Motif.Extensions.GuitarPro.Models;
using System.IO.Compression;
using System.Text;

internal static class GpTemplateDefaults
{
    private const string DefaultTemplateResourceName = "Motif.Extensions.GuitarPro.Resources.DefaultTemplate.gp";
    private const string ScoreEntryPath = "Content/score.gpif";
    private static readonly Lazy<TemplateDefaultsSnapshot> Snapshot = new(LoadSnapshot);

    public static ScoreMetadata CreateScoreMetadata()
        => CloneScoreMetadata(Snapshot.Value.ScoreMetadata);

    public static MasterTrackMetadata CreateMasterTrackMetadata()
        => CloneMasterTrackMetadata(Snapshot.Value.MasterTrackMetadata);

    private static TemplateDefaultsSnapshot LoadSnapshot()
    {
        using var stream = typeof(GpTemplateDefaults).Assembly.GetManifestResourceStream(DefaultTemplateResourceName);
        if (stream is null)
        {
            return new TemplateDefaultsSnapshot(new ScoreMetadata(), new MasterTrackMetadata());
        }

        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);
        var scoreEntry = archive.GetEntry(ScoreEntryPath);
        if (scoreEntry is null)
        {
            return new TemplateDefaultsSnapshot(new ScoreMetadata(), new MasterTrackMetadata());
        }

        using var entryStream = scoreEntry.Open();
        using var buffer = new MemoryStream();
        entryStream.CopyTo(buffer);
        buffer.Position = 0;

        var raw = new XmlGpifDeserializer().DeserializeAsync(buffer, CancellationToken.None).AsTask().GetAwaiter().GetResult();

        return new TemplateDefaultsSnapshot(
            new ScoreMetadata
            {
                ScoreXml = raw.Score.Xml,
                ExplicitEmptyOptionalElements = raw.Score.ExplicitEmptyOptionalElements.ToArray(),
                GpVersion = raw.GpVersion,
                GpRevisionXml = raw.GpRevision.Xml,
                GpRevisionRequired = raw.GpRevision.Required,
                GpRevisionRecommended = raw.GpRevision.Recommended,
                GpRevisionValue = raw.GpRevision.Value,
                EncodingDescription = raw.EncodingDescription,
                ScoreViewsXml = raw.ScoreViewsXml,
                SubTitle = raw.Score.SubTitle,
                Words = raw.Score.Words,
                Music = raw.Score.Music,
                WordsAndMusic = raw.Score.WordsAndMusic,
                Copyright = raw.Score.Copyright,
                Tabber = raw.Score.Tabber,
                Instructions = raw.Score.Instructions,
                Notices = raw.Score.Notices,
                FirstPageHeader = raw.Score.FirstPageHeader,
                FirstPageFooter = raw.Score.FirstPageFooter,
                PageHeader = raw.Score.PageHeader,
                PageFooter = raw.Score.PageFooter,
                ScoreSystemsDefaultLayout = raw.Score.ScoreSystemsDefaultLayout,
                ScoreSystemsLayout = raw.Score.ScoreSystemsLayout,
                ScoreZoomPolicy = raw.Score.ScoreZoomPolicy,
                ScoreZoom = raw.Score.ScoreZoom,
                PageSetupXml = raw.Score.PageSetupXml,
                MultiVoice = raw.Score.MultiVoice,
                BackingTrackXml = raw.BackingTrackXml,
                AudioTracksXml = raw.AudioTracksXml,
                AssetsXml = raw.AssetsXml
            },
            new MasterTrackMetadata
            {
                Xml = raw.MasterTrack.Xml,
                TrackIds = raw.MasterTrack.TrackIds.ToArray(),
                AutomationsXml = raw.MasterTrack.AutomationsXml,
                Automations = raw.MasterTrack.Automations.Select(CloneAutomation).ToArray(),
                Anacrusis = raw.MasterTrack.Anacrusis,
                RseXml = raw.MasterTrack.RseXml,
                Rse = new MasterTrackRseMetadata
                {
                    MasterEffects = raw.MasterTrack.Rse.MasterEffects.Select(effect => new RseEffectMetadata
                    {
                        Id = effect.Id,
                        Bypass = effect.Bypass,
                        Parameters = effect.Parameters
                    }).ToArray()
                }
            });
    }

    private static AutomationMetadata CloneAutomation(Motif.Extensions.GuitarPro.Models.Raw.GpifAutomation automation)
        => new()
        {
            Type = automation.Type,
            Linear = automation.Linear,
            Bar = automation.Bar,
            Position = automation.Position,
            Visible = automation.Visible,
            Value = automation.Value
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
            TempoMap = source.TempoMap.Select(CloneTempoEvent).ToArray()
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
            Offset = source.Offset,
            Visible = source.Visible,
            Value = source.Value,
            NumericValue = source.NumericValue,
            ReferenceHint = source.ReferenceHint,
            Tempo = source.Tempo is null ? null : CloneTempoEvent(source.Tempo)
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

    private static TempoEventMetadata CloneTempoEvent(TempoEventMetadata source)
        => new()
        {
            Bar = source.Bar,
            Offset = source.Offset,
            Bpm = source.Bpm,
            DenominatorHint = source.DenominatorHint
        };

    private sealed record TemplateDefaultsSnapshot(ScoreMetadata ScoreMetadata, MasterTrackMetadata MasterTrackMetadata);
}
