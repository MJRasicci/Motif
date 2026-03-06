namespace GPIO.NET.UnitTests;

using FluentAssertions;
using GPIO.NET.Implementation;
using GPIO.NET.Models.Patching;
using System.IO.Compression;

public class GuitarProPatcherTests
{
    [Fact]
    public async Task Patcher_appends_note_to_existing_voice_without_full_rewrite_model_loss()
    {
        var source = Path.Combine(AppContext.BaseDirectory, "Fixtures", "test.gp");
        File.Exists(source).Should().BeTrue();

        var output = Path.Combine(Path.GetTempPath(), $"gpio-patched-{Guid.NewGuid():N}.gp");

        try
        {
            var reader = new GPIO.NET.GuitarProReader();
            var before = await reader.ReadAsync(source, cancellationToken: TestContext.Current.CancellationToken);

            var patcher = new GuitarProPatcher();
            var result = await patcher.PatchAsync(
                source,
                output,
                new GpPatchDocument
                {
                    AppendNotes =
                    [
                        new AppendNotesPatch
                        {
                            TrackId = before.Tracks[0].Id,
                            MasterBarIndex = 0,
                            VoiceIndex = 0,
                            RhythmNoteValue = "Quarter",
                            MidiPitches = [64]
                        }
                    ]
                },
                TestContext.Current.CancellationToken);

            result.Diagnostics.Entries.Should().NotBeEmpty();
            result.Diagnostics.Entries.Any(e => e.Operation == "append-notes").Should().BeTrue();

            File.Exists(output).Should().BeTrue();

            var after = await reader.ReadAsync(output, cancellationToken: TestContext.Current.CancellationToken);
            after.Tracks.Count.Should().Be(before.Tracks.Count);

            var beforeNotes = before.Tracks[0].Measures[0].Beats.SelectMany(b => b.Notes).Count();
            var afterNotes = after.Tracks[0].Measures[0].Beats.SelectMany(b => b.Notes).Count();
            afterNotes.Should().BeGreaterThan(beforeNotes);
        }
        finally
        {
            if (File.Exists(output))
            {
                File.Delete(output);
            }
        }
    }

    [Fact]
    public async Task Patcher_can_insert_beat_at_start_of_voice_sequence()
    {
        var source = Path.Combine(AppContext.BaseDirectory, "Fixtures", "test.gp");
        var output = Path.Combine(Path.GetTempPath(), $"gpio-patched-insert-{Guid.NewGuid():N}.gp");

        try
        {
            var reader = new GPIO.NET.GuitarProReader();
            var before = await reader.ReadAsync(source, cancellationToken: TestContext.Current.CancellationToken);

            var firstBeforePitch = before.Tracks[0].Measures[0].Beats.SelectMany(b => b.Notes).FirstOrDefault()?.MidiPitch;

            var patcher = new GuitarProPatcher();
            await patcher.PatchAsync(
                source,
                output,
                new GpPatchDocument
                {
                    InsertBeats =
                    [
                        new InsertBeatPatch
                        {
                            TrackId = before.Tracks[0].Id,
                            MasterBarIndex = 0,
                            VoiceIndex = 0,
                            BeatInsertIndex = 0,
                            RhythmNoteValue = "Quarter",
                            MidiPitches = [72]
                        }
                    ]
                },
                TestContext.Current.CancellationToken);

            var after = await reader.ReadAsync(output, cancellationToken: TestContext.Current.CancellationToken);
            var firstAfterPitch = after.Tracks[0].Measures[0].Beats.SelectMany(b => b.Notes).FirstOrDefault()?.MidiPitch;

            firstAfterPitch.Should().Be(72);
            firstAfterPitch.Should().NotBe(firstBeforePitch);
        }
        finally
        {
            if (File.Exists(output)) File.Delete(output);
        }
    }

    [Fact]
    public async Task Patcher_can_update_existing_note_articulation_by_note_id()
    {
        var source = Path.Combine(AppContext.BaseDirectory, "Fixtures", "test.gp");
        var output = Path.Combine(Path.GetTempPath(), $"gpio-patched-artic-{Guid.NewGuid():N}.gp");

        try
        {
            var reader = new GPIO.NET.GuitarProReader();
            var before = await reader.ReadAsync(source, cancellationToken: TestContext.Current.CancellationToken);
            var targetNote = before.Tracks[0].Measures[0].Beats.SelectMany(b => b.Notes).First(n => n.Id > 0);

            var patcher = new GuitarProPatcher();
            await patcher.PatchAsync(
                source,
                output,
                new GpPatchDocument
                {
                    UpdateNoteArticulations =
                    [
                        new UpdateNoteArticulationPatch
                        {
                            NoteId = targetNote.Id,
                            LetRing = true,
                            PalmMuted = true,
                            SlideFlags = 8
                        }
                    ]
                },
                TestContext.Current.CancellationToken);

            var after = await reader.ReadAsync(output, cancellationToken: TestContext.Current.CancellationToken);
            var patched = after.Tracks[0].Measures[0].Beats.SelectMany(b => b.Notes).First(n => n.Id == targetNote.Id);

            patched.Articulation.LetRing.Should().BeTrue();
            patched.Articulation.PalmMuted.Should().BeTrue();
            patched.Articulation.SlideFlags.Should().Be(8);
        }
        finally
        {
            if (File.Exists(output)) File.Delete(output);
        }
    }

    [Fact]
    public async Task Patcher_can_append_new_voice_to_existing_bar()
    {
        var source = Path.Combine(AppContext.BaseDirectory, "Fixtures", "test.gp");
        var output = Path.Combine(Path.GetTempPath(), $"gpio-patched-voice-{Guid.NewGuid():N}.gp");

        try
        {
            var patcher = new GuitarProPatcher();
            await patcher.PatchAsync(
                source,
                output,
                new GpPatchDocument
                {
                    AppendVoices =
                    [
                        new AppendVoicePatch
                        {
                            TrackId = 0,
                            MasterBarIndex = 0
                        }
                    ],
                    AppendNotes =
                    [
                        new AppendNotesPatch
                        {
                            TrackId = 0,
                            MasterBarIndex = 0,
                            VoiceIndex = 1,
                            RhythmNoteValue = "Quarter",
                            MidiPitches = [67]
                        }
                    ]
                },
                TestContext.Current.CancellationToken);

            using var zip = System.IO.Compression.ZipFile.OpenRead(output);
            var entry = zip.GetEntry("Content/score.gpif");
            entry.Should().NotBeNull();
            using var stream = entry!.Open();
            var doc = await System.Xml.Linq.XDocument.LoadAsync(stream, System.Xml.Linq.LoadOptions.None, TestContext.Current.CancellationToken);

            var masterBar = doc.Root!.Element("MasterBars")!.Elements("MasterBar").First();
            var barId = int.Parse(masterBar.Element("Bars")!.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries).First());
            var bar = doc.Root!.Element("Bars")!.Elements("Bar").First(b => (int)b.Attribute("id")! == barId);
            var voices = bar.Element("Voices")!.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            voices.Length.Should().BeGreaterThan(1);
        }
        finally
        {
            if (File.Exists(output)) File.Delete(output);
        }
    }

    [Fact]
    public async Task Patcher_can_append_bar_slot_for_track_in_master_bar()
    {
        var source = Path.Combine(AppContext.BaseDirectory, "Fixtures", "test.gp");
        var output = Path.Combine(Path.GetTempPath(), $"gpio-patched-bar-{Guid.NewGuid():N}.gp");

        try
        {
            var patcher = new GuitarProPatcher();
            await patcher.PatchAsync(
                source,
                output,
                new GpPatchDocument
                {
                    AppendBars =
                    [
                        new AppendBarPatch
                        {
                            TrackId = 0,
                            MasterBarIndex = 0,
                            NewBarVoiceCount = 2
                        }
                    ]
                },
                TestContext.Current.CancellationToken);

            File.Exists(output).Should().BeTrue();
            // smoke-level compatibility check
            var reader = new GPIO.NET.GuitarProReader();
            var readBack = await reader.ReadAsync(output, cancellationToken: TestContext.Current.CancellationToken);
            readBack.Tracks.Should().NotBeEmpty();
        }
        finally
        {
            if (File.Exists(output)) File.Delete(output);
        }
    }

    [Fact]
    public async Task Patcher_preserves_non_score_archive_entries()
    {
        var source = Path.Combine(AppContext.BaseDirectory, "Fixtures", "test.gp");
        var output = Path.Combine(Path.GetTempPath(), $"gpio-patched-preserve-{Guid.NewGuid():N}.gp");

        try
        {
            var patcher = new GuitarProPatcher();
            await patcher.PatchAsync(source, output, new GpPatchDocument(), TestContext.Current.CancellationToken);

            using var sourceZip = ZipFile.OpenRead(source);
            using var outputZip = ZipFile.OpenRead(output);

            var sourceEntryNames = sourceZip.Entries.Select(e => e.FullName).ToArray();
            var outputEntryNames = outputZip.Entries.Select(e => e.FullName).ToArray();
            outputEntryNames.Should().BeEquivalentTo(sourceEntryNames);

            foreach (var entryName in new[] { "Content/PartConfiguration", "Content/LayoutConfiguration", "Content/Preferences.json" })
            {
                var sourceEntry = sourceZip.GetEntry(entryName);
                var outputEntry = outputZip.GetEntry(entryName);
                sourceEntry.Should().NotBeNull();
                outputEntry.Should().NotBeNull();

                var sourceBytes = await ReadAllBytesAsync(sourceEntry!, TestContext.Current.CancellationToken);
                var outputBytes = await ReadAllBytesAsync(outputEntry!, TestContext.Current.CancellationToken);
                outputBytes.Should().Equal(sourceBytes, $"entry '{entryName}' payload should be preserved");
            }
        }
        finally
        {
            if (File.Exists(output))
            {
                File.Delete(output);
            }
        }
    }

    private static async Task<byte[]> ReadAllBytesAsync(ZipArchiveEntry entry, CancellationToken cancellationToken)
    {
        await using var stream = entry.Open();
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken);
        return buffer.ToArray();
    }
}
