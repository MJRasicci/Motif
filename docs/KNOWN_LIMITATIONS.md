# Known Limitations

## Guitar Pro GPIF Byte-Level Drift

Motif currently treats exact raw GPIF byte preservation as out of scope for v1 when the
parsed XML remains equivalent.

- Scope: `.gp -> .gp`, `.gp -> .motif -> .gp`, and raw `.gpif` writes may reorder XML
  attributes, normalize whitespace, or emit equivalent defaults differently.
- Diagnostic behavior: the writer emits `RAW_GPIF_BYTE_DRIFT` as `info` when the source
  and output bytes differ but the parsed XML trees are equivalent. It remains a
  `warning` when XML-level differences are detected or the comparison cannot be
  completed safely.

Implications:

- Byte-for-byte diffs are not a reliable regression signal on their own.
- Use writer diagnostics and semantic/XML comparison when validating no-edit round trips.

This limitation is accepted for v1 unless a later serializer change can reduce the drift
without compromising deterministic output or musical fidelity.
