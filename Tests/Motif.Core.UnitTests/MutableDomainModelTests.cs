namespace Motif.Core.UnitTests;

using FluentAssertions;
using Motif.Models;

public class MutableDomainModelTests
{
    [Fact]
    public void Domain_model_supports_post_construction_mutation()
    {
        var score = new GuitarProScore();
        var track = new TrackModel();
        var measure = new MeasureModel();
        var beat = new BeatModel();
        var note = new NoteModel();

        score.Title = "Motif";
        score.Artist = "Artist";
        score.Metadata = new ScoreMetadata { SubTitle = "Initial subtitle" };
        score.MasterTrack = new MasterTrackMetadata { Anacrusis = true };
        score.PlaybackMasterBarSequence = [0, 1];

        track.Id = 1;
        track.Name = "Lead";
        track.Metadata = new TrackMetadata { ShortName = "Ld" };

        measure.Index = 0;
        measure.TimeSignature = "4/4";
        measure.Voices =
        [
            new MeasureVoiceModel
            {
                VoiceIndex = 0
            }
        ];

        beat.Id = 10;
        beat.Dynamic = "mf";
        beat.SourceRhythm = new RhythmShapeModel
        {
            NoteValue = "quarter"
        };

        note.Id = 20;
        note.MidiPitch = 64;
        note.Duration = 1m;
        note.Articulation = new NoteArticulationModel
        {
            LetRing = true
        };

        beat.Notes = [note];
        measure.Beats = [beat];
        track.Measures = [measure];
        score.Tracks = [track];

        score.Title = "Mutable Motif";
        score.Metadata.SubTitle = "Updated subtitle";
        score.MasterTrack.Anacrusis = false;
        score.PlaybackMasterBarSequence = [0, 1, 2];

        track.Name = "Rhythm";
        track.Metadata.ShortName = "Rhy";

        measure.TimeSignature = "7/8";
        measure.AdditionalStaffBars =
        [
            new MeasureStaffModel
            {
                StaffIndex = 1,
                Clef = "Bass"
            }
        ];

        beat.Dynamic = "ff";
        beat.SourceRhythm.NoteValue = "eighth";

        note.MidiPitch = 67;
        note.ConcertPitch = new PitchValueModel
        {
            Step = "G",
            Octave = 4
        };
        note.Articulation.Bend = new BendModel
        {
            Enabled = true,
            DestinationValue = 1.5m
        };

        score.Title.Should().Be("Mutable Motif");
        score.Metadata.SubTitle.Should().Be("Updated subtitle");
        score.MasterTrack.Anacrusis.Should().BeFalse();
        score.PlaybackMasterBarSequence.Should().Equal(0, 1, 2);
        score.Tracks.Should().ContainSingle().Which.Should().BeSameAs(track);

        track.Name.Should().Be("Rhythm");
        track.Metadata.ShortName.Should().Be("Rhy");
        track.Measures.Should().ContainSingle().Which.Should().BeSameAs(measure);

        measure.TimeSignature.Should().Be("7/8");
        measure.AdditionalStaffBars.Should().ContainSingle();
        measure.Beats.Should().ContainSingle().Which.Should().BeSameAs(beat);

        beat.Dynamic.Should().Be("ff");
        beat.SourceRhythm.Should().NotBeNull();
        beat.SourceRhythm!.NoteValue.Should().Be("eighth");
        beat.Notes.Should().ContainSingle().Which.Should().BeSameAs(note);

        note.MidiPitch.Should().Be(67);
        note.ConcertPitch.Should().NotBeNull();
        note.ConcertPitch!.Step.Should().Be("G");
        note.Articulation.LetRing.Should().BeTrue();
        note.Articulation.Bend.Should().NotBeNull();
        note.Articulation.Bend!.Enabled.Should().BeTrue();
        note.Articulation.Bend.DestinationValue.Should().Be(1.5m);
    }
}
