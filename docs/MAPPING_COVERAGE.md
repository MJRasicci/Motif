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
- ✅ Notes (pitch + articulation/effect coverage tracked below)

## Metadata subsystem blocks
- ✅ Master-track RSE `Master` effects typed + read/map/write parity
- ✅ Track RSE channel-strip core (`Bank`, version/parameters, channel-strip automations)
- ✅ Track audio engine state (`AudioEngineState`)
- ✅ Track MIDI connection (`Port`, channels, one-channel-per-string flag)
- ✅ Track lyrics block (`dispatched` + line text/offset)
- ✅ Track transpose block (`Chromatic`, `Octave`)
- ✅ Deeper instrument/sound typing (`InstrumentSet` elements/articulations + sound-level RSE core)

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
- ✅ Harmonics (typed semantic kind + `HType`/`HFret` GPIF parity)
- ✅ Slide mapping (semantic enum projection validated against schema fixture cases)
- ✅ Hammer-on / pull-off semantics (adjacent-note linkage + inferred HO/PO type)
- ✅ Palm mute semantics (note property + beat-level effect projection)
- ✅ Bend mapping (normalized curve units + inferred bend-type semantics)
- ✅ Beat whammy/tremolo-bar curve semantics (`WhammyBar*` property-family normalization with /50 value and /100 offset scaling)
- ✅ Rasgueado pattern semantics (`Property name="Rasgueado"` enable flag)
- ✅ Dead-slapped beat semantics (`<DeadSlapped />` element presence)
- ✅ Arpeggio/brush differentiation and timing (`<Arpeggio>` vs `Brush` plus brush-duration XProperties `687935489`/`687931393`, with Android-parity default `Brush` duration = 60 ticks)
- ✅ Trill speed semantics (`XProperty id="688062467"` tempo bucket decoding: ≥240→16th, ≥120→32nd, ≥60→64th, <60→128th)
- ✅ Additional beat-effect semantics (`<Tremolo>` with speed value, `<Chord>` ID, `<FreeText>` text)

## Tempo / automation / dynamics
- ✅ Track/master automation capture + round-trip
- ✅ Tempo map projection from tempo automations
- ✅ Unified automation timeline synthesis across master + track automations (ordered timeline with parsed value/reference hints)
- ✅ Dynamic map integration (beat-level dynamic change points synthesized per track/voice)

## Validation and quality
- 🟡 Fixture-based tests (started)
- ⛔ Schema coverage matrix (XSD element-by-element)
- ✅ Playback-sequence edge-case tests for repeat/jump behavior (DS/DC/Coda/Fine, alternate endings, anacrusis, legacy direction aliases)

## Immediate next targets
1. Add explicit schema coverage report generation
2. Expand fixture corpus for advanced patch planner structural diffs
