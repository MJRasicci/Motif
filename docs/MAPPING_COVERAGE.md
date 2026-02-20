# GPIO.NET Mapping Coverage Checklist

Status legend:
- ✅ Implemented
- 🟡 Partial
- ⛔ Not implemented

## Core ingestion
- ✅ Open `.gp` archive and extract `Content/score.gpif`
- ✅ Deserialize GPIF XML into raw/intermediate model
- ✅ Resolve space-separated ID reference lists (bars/voices/beats/notes)

## Structural score mapping
- ✅ Score metadata (title/artist/album)
- ✅ Tracks
- ✅ Master bars / measures
- 🟡 Voices (primary voice path mapped; multi-voice strategy incomplete)
- ✅ Beats
- 🟡 Notes (pitch + subset of articulations)

## Navigation / playback flow
- ✅ Repeat start/end metadata
- ✅ Alternate ending metadata
- ✅ Jump/target metadata (Da Capo / Da Segno / Coda fields captured)
- 🟡 Playback sequence generation (basic handling implemented)
- ⛔ Full notation-engine semantics (all DS/DC/Fine edge cases)

## Rhythm model
- ✅ Base note values (whole/half/quarter/eighth/16/32/64)
- ✅ Tuplets (primary/secondary ratio support)
- ✅ Augmentation dot multipliers
- ✅ Tie duration merging across beats/bars (pitch-based stitch)

## Note articulation/effect coverage
- ✅ Let ring
- ✅ Vibrato (presence + value)
- ✅ Tie (origin/destination)
- ✅ Trill
- ✅ Accent / anti-accent
- ✅ Instrument articulation value
- ⛔ Harmonics
- ⛔ Slide types
- ⛔ Hammer-on / pull-off semantics
- ⛔ Palm mute / staccato / dead notes full semantics
- ⛔ Bend/whammy mapping to domain events
- ⛔ Grace note detail
- ⛔ Fingering detail

## Tempo / automation / dynamics
- ⛔ Tempo map + automation timeline
- ⛔ Dynamic map integration

## Validation and quality
- 🟡 Fixture-based tests (started)
- ⛔ Schema coverage matrix (XSD element-by-element)
- ⛔ Golden playback-sequence tests for repeat/jump edge cases

## Immediate next targets
1. Complete rhythm (tuplets + augmentation dots + tie handling)
2. Expand articulations (harmonics, slides, grace, palm mute, bends)
3. Add navigation edge-case test corpus (DS/DC/Coda/Fine)
4. Add explicit schema coverage report generation
