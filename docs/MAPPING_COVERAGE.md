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
- ✅ Voices (all voice references per bar mapped; primary-voice compatibility path retained)
- ✅ Beats
- 🟡 Notes (pitch + subset of articulations)

## Navigation / playback flow
- ✅ Repeat start/end metadata
- ✅ Alternate ending metadata
- ✅ Jump/target metadata (Da Capo / Da Segno / Coda fields captured)
- ✅ Playback sequence generation with Android-parity handling for `DaCapo*`, `DaSegno*`, `DaSegnoSegno*`, `DaCoda`, `DaDoubleCoda`, and `Fine`
- ✅ Direction-gating and loop semantics parity (extended alternate endings, ignore-once jumps, and conditional Coda/DoubleCoda activation)
- ✅ Anacrusis-aware repeat anchoring

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
- ✅ Left/right fingering (`LeftFingering`, `RightFingering`)
- ✅ Ornament text (`Ornament`)
- ✅ Grace-note beat typing (`GraceNotes`: `BeforeBeat`/`OnBeat`)
- ✅ Beat effect fields (`PickStroke`, `VibratoWTremBar`, `Brush`, `Slapped`, `Popped`)
- 🟡 Harmonics (enabled/type/fret mapped)
- 🟡 Slide mapping (flags decoded to slide enum; GP naming validation still in progress)
- 🟡 Hammer-on / pull-off semantics (Hopo origin/destination captured)
- 🟡 Palm mute / dead-note toggles (property flags captured; deeper semantics pending)
- 🟡 Bend/whammy mapping to domain model (curve points mapped)

## Tempo / automation / dynamics
- ⛔ Tempo map + automation timeline
- ⛔ Dynamic map integration

## Validation and quality
- 🟡 Fixture-based tests (started)
- ⛔ Schema coverage matrix (XSD element-by-element)
- ✅ Playback-sequence edge-case tests for repeat/jump behavior (DS/DC/Coda/Fine, alternate endings, anacrusis, legacy direction aliases)

## Immediate next targets
1. Expand articulation semantics (harmonics, slides, palm mute, bends) beyond flag-level mapping
2. Integrate tempo/automation timeline mapping
3. Add explicit schema coverage report generation
4. Expand fixture corpus for advanced patch planner structural diffs
