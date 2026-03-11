namespace Motif.Core.UnitTests;

using FluentAssertions;
using Motif;
using Motif.Models;

public class MutableDomainModelTests
{
    [Fact]
    public void Domain_model_supports_post_construction_mutation()
    {
        var score = new Score();
        var track = new Track();
        var primaryStaff = new Staff();
        var secondaryStaff = new Staff();
        var primaryStaffMeasure = new StaffMeasure();
        var secondaryStaffMeasure = new StaffMeasure();
        var timelineBar = new TimelineBar();
        var beat = new Beat();
        var note = new Note();

        score.Title = "Motif";
        score.Artist = "Artist";
        score.Anacrusis = true;
        score.PlaybackMasterBarSequence = [0, 1];
        score.TimelineBars = [timelineBar];

        track.Id = 1;
        track.Name = "Lead";
        track.Staves = [primaryStaff, secondaryStaff];

        primaryStaff.StaffIndex = 0;
        primaryStaff.Measures = [primaryStaffMeasure];

        secondaryStaff.StaffIndex = 1;
        secondaryStaff.Measures = [secondaryStaffMeasure];

        primaryStaffMeasure.Index = 0;
        primaryStaffMeasure.StaffIndex = 0;
        primaryStaffMeasure.Clef = "Treble";

        secondaryStaffMeasure.Index = 0;
        secondaryStaffMeasure.StaffIndex = 1;
        secondaryStaffMeasure.Clef = "Bass";

        timelineBar.Index = 0;
        timelineBar.TimeSignature = "4/4";

        primaryStaffMeasure.Voices =
        [
            new Voice
            {
                VoiceIndex = 0
            }
        ];

        beat.Id = 10;
        beat.Dynamic = "mf";
        beat.FreeText = "quarter pulse";

        note.Id = 20;
        note.MidiPitch = 64;
        note.Duration = 1m;
        note.Articulation = new NoteArticulation
        {
            LetRing = true
        };

        beat.Notes = [note];
        primaryStaffMeasure.Beats = [beat];
        score.Tracks = [track];

        score.Title = "Mutable Motif";
        score.Anacrusis = false;
        score.PlaybackMasterBarSequence = [0, 1, 2];
        timelineBar.TimeSignature = "7/8";
        timelineBar.SectionLetter = "A";

        track.Name = "Rhythm";
        primaryStaffMeasure.Clef = "Bass";
        secondaryStaffMeasure.Clef = "Tenor";

        beat.Dynamic = "ff";
        beat.FreeText = "eighth pulse";

        note.MidiPitch = 67;
        note.ConcertPitch = new PitchValue
        {
            Step = "G",
            Octave = 4
        };
        note.Articulation.Bend = new Bend
        {
            Enabled = true,
            DestinationValue = 1.5m
        };

        score.Title.Should().Be("Mutable Motif");
        score.Anacrusis.Should().BeFalse();
        score.PlaybackMasterBarSequence.Should().Equal(0, 1, 2);
        score.TimelineBars.Should().ContainSingle().Which.Should().BeSameAs(timelineBar);
        timelineBar.TimeSignature.Should().Be("7/8");
        timelineBar.SectionLetter.Should().Be("A");
        score.Tracks.Should().ContainSingle().Which.Should().BeSameAs(track);

        track.Name.Should().Be("Rhythm");
        track.Staves.Should().HaveCount(2);
        track.Staves[0].Should().BeSameAs(primaryStaff);
        track.Staves[1].Should().BeSameAs(secondaryStaff);

        primaryStaff.StaffIndex.Should().Be(0);
        primaryStaff.Measures.Should().ContainSingle().Which.Should().BeSameAs(primaryStaffMeasure);
        secondaryStaff.StaffIndex.Should().Be(1);
        secondaryStaff.Measures.Should().ContainSingle().Which.Should().BeSameAs(secondaryStaffMeasure);

        primaryStaffMeasure.Clef.Should().Be("Bass");
        secondaryStaffMeasure.Clef.Should().Be("Tenor");
        primaryStaffMeasure.Beats.Should().ContainSingle().Which.Should().BeSameAs(beat);

        beat.Dynamic.Should().Be("ff");
        beat.FreeText.Should().Be("eighth pulse");
        beat.Notes.Should().ContainSingle().Which.Should().BeSameAs(note);

        note.MidiPitch.Should().Be(67);
        note.ConcertPitch.Should().NotBeNull();
        note.ConcertPitch!.Step.Should().Be("G");
        note.Articulation.LetRing.Should().BeTrue();
        note.Articulation.Bend.Should().NotBeNull();
        note.Articulation.Bend!.Enabled.Should().BeTrue();
        note.Articulation.Bend.DestinationValue.Should().Be(1.5m);

        ScoreNavigation.RebuildPlaybackSequence(score).Should().Equal(0);
    }
}
