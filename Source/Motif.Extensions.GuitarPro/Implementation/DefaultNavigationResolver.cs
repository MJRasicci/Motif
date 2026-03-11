namespace Motif.Extensions.GuitarPro.Implementation;

using Motif.Extensions.GuitarPro.Abstractions;
using Motif.Extensions.GuitarPro.Models.Raw;
using Motif.Extensions.GuitarPro.Utilities;

internal sealed class DefaultNavigationResolver : INavigationResolver
{
    public IReadOnlyList<int> BuildPlaybackSequence(IReadOnlyList<GpifMasterBar> masterBars, bool anacrusis = false)
    {
        var ordered = masterBars
            .OrderBy(m => m.Index)
            .Select((m, ordinal) => MasterBarState.From(m, ordinal))
            .ToArray();

        if (ordered.Length == 0)
        {
            return Array.Empty<int>();
        }

        var directions = DirectionMap.From(ordered);
        var unroller = new AndroidParityUnroller(ordered, directions, anacrusis);
        return unroller.Build();
    }

    private static bool IsDirectionToken(MasterBarState bar, string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        if (TokenEquals(bar.Jump, token) || TokenEquals(bar.Target, token))
        {
            return true;
        }

        if (bar.DirectionProperties.TryGetValue("Jump", out var jump) && TokenEquals(jump, token))
        {
            return true;
        }

        if (bar.DirectionProperties.TryGetValue("Target", out var target) && TokenEquals(target, token))
        {
            return true;
        }

        foreach (var kvp in bar.DirectionProperties)
        {
            if (TokenEquals(kvp.Key, token) || TokenEquals(kvp.Value, token))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TokenEquals(string? value, string token)
        => string.Equals(NormalizeToken(value), NormalizeToken(token), StringComparison.OrdinalIgnoreCase);

    private static string NormalizeToken(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = new string(value.Where(char.IsLetterOrDigit).ToArray());
        return normalized switch
        {
            "DaCapoAlDbleCoda" => "DaCapoAlDoubleCoda",
            "DaSegnoAlDbleCoda" => "DaSegnoAlDoubleCoda",
            "DaSegnoSegnoAlDbleCoda" => "DaSegnoSegnoAlDoubleCoda",
            "DaDbleCoda" => "DaDoubleCoda",
            _ => normalized
        };
    }

    private sealed class AndroidParityUnroller
    {
        private readonly MasterBarState[] masterBars;
        private readonly DirectionMap directions;
        private readonly bool anacrusis;
        private readonly List<int> playlist;
        private readonly bool[] ignore;

        private readonly List<int> repeatStart = new();
        private readonly Dictionary<int, int> repeatEnd = new();

        private int passCount;
        private int currentRepeatStartIndex;
        private int scoreConcreteStart;

        public AndroidParityUnroller(MasterBarState[] masterBars, DirectionMap directions, bool anacrusis)
        {
            this.masterBars = masterBars;
            this.directions = directions;
            this.anacrusis = anacrusis;
            playlist = new List<int>(Math.Max(16, masterBars.Length * 2));
            ignore = new bool[DirectionCount];
        }

        public IReadOnlyList<int> Build()
        {
            playlist.Clear();
            Array.Fill(ignore, false);

            repeatStart.Clear();
            repeatEnd.Clear();

            var hasDaSegnoFamily = directions.DaSegno >= 0
                || directions.DaSegnoAlCoda >= 0
                || directions.DaSegnoAlDoubleCoda >= 0
                || directions.DaSegnoAlFine >= 0;

            var daSegnoSegnoAllowed = !hasDaSegnoFamily;
            var daDoubleCodaAllowed = directions.DaCoda < 0;

            scoreConcreteStart = 0;
            if (anacrusis && masterBars.Length > 0 && !masterBars[0].RepeatStart)
            {
                scoreConcreteStart = 1;
            }

            PopulateRepeatStructures();
            passCount = -1;
            currentRepeatStartIndex = -1;

            var daCodaActivated = false;
            var daDoubleCodaActivated = false;
            var fineActivated = false;
            var pendingRepeatResolution = false;

            var index = 0;
            var guard = 0;
            var guardLimit = Math.Max(50_000, masterBars.Length * 1024);

            while (index >= 0 && index < masterBars.Length && guard++ < guardLimit)
            {
                var masterBar = masterBars[index];

                if (index == 0 || masterBar.RepeatStart)
                {
                    if (currentRepeatStartIndex != index)
                    {
                        currentRepeatStartIndex = index;
                        passCount = 1;
                    }
                    else
                    {
                        passCount++;
                    }
                }

                playlist.Add(index);

                if (fineActivated && BarHasDirection(index, Direction.Fine))
                {
                    break;
                }

                var remainingRepeats = repeatEnd.TryGetValue(index, out var remaining) ? remaining : 0;
                if (remainingRepeats != 0)
                {
                    var skipRepeat = false;

                    if (currentRepeatStartIndex >= 0
                        && masterBar.HasAlternateEndings
                        && HasExtendedAlternateEndings(index)
                        && IsAlternateEndingSet(masterBar.AlternateEndingMask, passCount)
                        && HasAnyActiveDirectionJump(index, daSegnoSegnoAllowed, daCodaActivated, daDoubleCodaActivated, daDoubleCodaAllowed, fineActivated))
                    {
                        skipRepeat = true;
                    }
                    else
                    {
                        for (var i = Math.Max(0, currentRepeatStartIndex); i <= index && i < masterBars.Length; i++)
                        {
                            var scanned = masterBars[i];
                            if (scanned.HasAlternateEndings && HasExtendedAlternateEndings(i))
                            {
                                if (IsAlternateEndingSet(scanned.AlternateEndingMask, passCount))
                                {
                                    skipRepeat = false;
                                    break;
                                }

                                skipRepeat = true;
                            }
                        }
                    }

                    if (!skipRepeat && remainingRepeats > 0 && !pendingRepeatResolution)
                    {
                        pendingRepeatResolution = true;
                        repeatEnd[index] = remainingRepeats - 1;

                        var repeatDestination = 0;
                        foreach (var candidate in repeatStart)
                        {
                            if (candidate > index)
                            {
                                break;
                            }

                            repeatDestination = candidate;
                        }

                        index = repeatDestination;
                        continue;
                    }

                    repeatEnd[index] = masterBar.RepeatCount - 1;

                    if (pendingRepeatResolution)
                    {
                        pendingRepeatResolution = false;

                        for (var i = index + 1; i < masterBars.Length; i++)
                        {
                            if (masterBars[i].RepeatEnd)
                            {
                                pendingRepeatResolution = true;
                                break;
                            }

                            if (masterBars[i].RepeatStart)
                            {
                                break;
                            }
                        }
                    }
                }

                if (!ignore[(int)Direction.DaCapo] && BarHasDirection(index, Direction.DaCapo))
                {
                    ignore[(int)Direction.DaCapo] = true;
                    Goto(0);
                    index = 0;
                    continue;
                }

                if (!ignore[(int)Direction.DaCapoAlFine] && BarHasDirection(index, Direction.DaCapoAlFine))
                {
                    ignore[(int)Direction.DaCapoAlFine] = true;
                    Goto(0);
                    index = 0;
                    fineActivated = true;
                    continue;
                }

                if (!ignore[(int)Direction.DaCapoAlCoda] && BarHasDirection(index, Direction.DaCapoAlCoda))
                {
                    ignore[(int)Direction.DaCapoAlCoda] = true;
                    Goto(0);
                    index = 0;
                    daCodaActivated = true;
                    continue;
                }

                if (!ignore[(int)Direction.DaCapoAlDoubleCoda] && BarHasDirection(index, Direction.DaCapoAlDoubleCoda))
                {
                    ignore[(int)Direction.DaCapoAlDoubleCoda] = true;
                    Goto(0);
                    index = 0;
                    daDoubleCodaActivated = true;
                    continue;
                }

                if (!ignore[(int)Direction.DaSegno] && BarHasDirection(index, Direction.DaSegno))
                {
                    ignore[(int)Direction.DaSegno] = true;
                    var segno = directions.Segno;
                    if (segno >= 0)
                    {
                        Goto(segno);
                        index = segno;
                        daSegnoSegnoAllowed = true;
                        continue;
                    }

                    daSegnoSegnoAllowed = true;
                }

                if (!ignore[(int)Direction.DaSegnoSegno] && BarHasDirection(index, Direction.DaSegnoSegno) && daSegnoSegnoAllowed)
                {
                    ignore[(int)Direction.DaSegnoSegno] = true;
                    var segnoSegno = directions.SegnoSegno;
                    if (segnoSegno >= 0)
                    {
                        Goto(segnoSegno);
                        index = segnoSegno;
                        continue;
                    }
                }

                if (!ignore[(int)Direction.DaSegnoAlFine] && BarHasDirection(index, Direction.DaSegnoAlFine))
                {
                    ignore[(int)Direction.DaSegnoAlFine] = true;
                    var segno = directions.Segno;
                    if (segno >= 0)
                    {
                        Goto(segno);
                        index = segno;
                        daSegnoSegnoAllowed = true;
                        fineActivated = true;
                        continue;
                    }

                    daSegnoSegnoAllowed = true;
                    fineActivated = true;
                }

                if (!ignore[(int)Direction.DaSegnoSegnoAlFine] && BarHasDirection(index, Direction.DaSegnoSegnoAlFine) && daSegnoSegnoAllowed)
                {
                    ignore[(int)Direction.DaSegnoSegnoAlFine] = true;
                    var segnoSegno = directions.SegnoSegno;
                    if (segnoSegno >= 0)
                    {
                        Goto(segnoSegno);
                        index = segnoSegno;
                        fineActivated = true;
                        continue;
                    }

                    fineActivated = true;
                }

                if (!ignore[(int)Direction.DaSegnoAlCoda] && BarHasDirection(index, Direction.DaSegnoAlCoda))
                {
                    ignore[(int)Direction.DaSegnoAlCoda] = true;
                    var segno = directions.Segno;
                    if (segno >= 0)
                    {
                        Goto(segno);
                        index = segno;
                        daSegnoSegnoAllowed = true;
                        daCodaActivated = true;
                        continue;
                    }

                    daSegnoSegnoAllowed = true;
                    daCodaActivated = true;
                }

                if (!ignore[(int)Direction.DaSegnoSegnoAlCoda] && BarHasDirection(index, Direction.DaSegnoSegnoAlCoda) && daSegnoSegnoAllowed)
                {
                    ignore[(int)Direction.DaSegnoSegnoAlCoda] = true;
                    var segnoSegno = directions.SegnoSegno;
                    if (segnoSegno >= 0)
                    {
                        Goto(segnoSegno);
                        index = segnoSegno;
                        daCodaActivated = true;
                        continue;
                    }

                    daCodaActivated = true;
                }

                if (!ignore[(int)Direction.DaSegnoAlDoubleCoda] && BarHasDirection(index, Direction.DaSegnoAlDoubleCoda))
                {
                    ignore[(int)Direction.DaSegnoAlDoubleCoda] = true;
                    var segno = directions.Segno;
                    if (segno >= 0)
                    {
                        Goto(segno);
                        index = segno;
                        daSegnoSegnoAllowed = true;
                        daDoubleCodaActivated = true;
                        continue;
                    }

                    daSegnoSegnoAllowed = true;
                    daDoubleCodaActivated = true;
                }

                if (!ignore[(int)Direction.DaSegnoSegnoAlDoubleCoda] && BarHasDirection(index, Direction.DaSegnoSegnoAlDoubleCoda) && daSegnoSegnoAllowed)
                {
                    ignore[(int)Direction.DaSegnoSegnoAlDoubleCoda] = true;
                    var segnoSegno = directions.SegnoSegno;
                    if (segnoSegno >= 0)
                    {
                        Goto(segnoSegno);
                        index = segnoSegno;
                        daDoubleCodaActivated = true;
                        continue;
                    }

                    daDoubleCodaActivated = true;
                }

                if (daCodaActivated && !ignore[(int)Direction.DaCoda] && BarHasDirection(index, Direction.DaCoda))
                {
                    ignore[(int)Direction.DaCoda] = true;
                    var coda = directions.Coda;
                    if (coda >= 0)
                    {
                        Goto(coda);
                        index = coda;
                        daDoubleCodaAllowed = true;
                        fineActivated = true;
                        continue;
                    }

                    daDoubleCodaAllowed = true;
                    fineActivated = true;
                }

                if (daDoubleCodaActivated
                    && !ignore[(int)Direction.DaDoubleCoda]
                    && BarHasDirection(index, Direction.DaDoubleCoda)
                    && daDoubleCodaAllowed)
                {
                    ignore[(int)Direction.DaDoubleCoda] = true;
                    var doubleCoda = directions.DoubleCoda;
                    if (doubleCoda >= 0)
                    {
                        Goto(doubleCoda);
                        index = doubleCoda;
                        fineActivated = true;
                        continue;
                    }

                    fineActivated = true;
                }

                index++;
            }

            if (!daCodaActivated && !ignore[(int)Direction.DaCoda])
            {
                UnrollUnusedDaCoda();
            }

            if (!daDoubleCodaActivated && !ignore[(int)Direction.DaDoubleCoda])
            {
                UnrollUnusedDaDoubleCoda();
            }

            PruneExtendedAlternateEndings();

            var result = playlist.Where(i => i >= 0).ToArray();
            return result;
        }

        private void Goto(int barIndex)
        {
            passCount = 0;
            repeatStart.Clear();
            repeatEnd.Clear();
            currentRepeatStartIndex = barIndex != 0 ? 0 : -1;
            currentRepeatStartIndex = LowerRepeatStartIndex(barIndex);

            if (barIndex >= 0 && barIndex < masterBars.Length && HasExtendedAlternateEndings(barIndex))
            {
                for (var i = 0; i < 8; i++)
                {
                    if (IsExtendedAlternateEndingSet(barIndex, i))
                    {
                        passCount = i;
                        break;
                    }
                }
            }

            playlist.Add(-1);
            PopulateRepeatStructures();
        }

        private void PopulateRepeatStructures()
        {
            for (var i = 0; i < masterBars.Length; i++)
            {
                if (masterBars[i].RepeatEnd)
                {
                    repeatEnd[i] = masterBars[i].RepeatCount - 1;
                }

                if (masterBars[i].RepeatStart || i == scoreConcreteStart)
                {
                    repeatStart.Add(i);
                }
            }
        }

        private bool HasAnyActiveDirectionJump(
            int index,
            bool daSegnoSegnoAllowed,
            bool daCodaActivated,
            bool daDoubleCodaActivated,
            bool daDoubleCodaAllowed,
            bool fineActivated)
        {
            for (var i = 0; i < DirectionCount; i++)
            {
                if (HasActiveDirectionJump(index, (Direction)i, daSegnoSegnoAllowed, daCodaActivated, daDoubleCodaActivated, daDoubleCodaAllowed, fineActivated))
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasActiveDirectionJump(
            int index,
            Direction direction,
            bool daSegnoSegnoAllowed,
            bool daCodaActivated,
            bool daDoubleCodaActivated,
            bool daDoubleCodaAllowed,
            bool fineActivated)
        {
            return direction switch
            {
                Direction.Fine => fineActivated && BarHasDirection(index, Direction.Fine),
                Direction.DaCapo => !ignore[(int)Direction.DaCapo] && BarHasDirection(index, Direction.DaCapo),
                Direction.DaCapoAlCoda => !ignore[(int)Direction.DaCapoAlCoda] && BarHasDirection(index, Direction.DaCapoAlCoda),
                Direction.DaCapoAlDoubleCoda => !ignore[(int)Direction.DaCapoAlDoubleCoda] && BarHasDirection(index, Direction.DaCapoAlDoubleCoda),
                Direction.DaCapoAlFine => !ignore[(int)Direction.DaCapoAlFine] && BarHasDirection(index, Direction.DaCapoAlFine),
                Direction.DaSegno => !ignore[(int)Direction.DaSegno] && BarHasDirection(index, Direction.DaSegno),
                Direction.DaSegnoAlCoda => !ignore[(int)Direction.DaSegnoAlCoda] && BarHasDirection(index, Direction.DaSegnoAlCoda),
                Direction.DaSegnoAlDoubleCoda => !ignore[(int)Direction.DaSegnoAlDoubleCoda] && BarHasDirection(index, Direction.DaSegnoAlDoubleCoda),
                Direction.DaSegnoAlFine => !ignore[(int)Direction.DaSegnoAlFine] && BarHasDirection(index, Direction.DaSegnoAlFine),
                Direction.DaSegnoSegno => !ignore[(int)Direction.DaSegnoSegno] && BarHasDirection(index, Direction.DaSegnoSegno) && daSegnoSegnoAllowed,
                Direction.DaSegnoSegnoAlCoda => !ignore[(int)Direction.DaSegnoSegnoAlCoda] && BarHasDirection(index, Direction.DaSegnoSegnoAlCoda) && daSegnoSegnoAllowed,
                Direction.DaSegnoSegnoAlDoubleCoda => !ignore[(int)Direction.DaSegnoSegnoAlDoubleCoda] && BarHasDirection(index, Direction.DaSegnoSegnoAlDoubleCoda) && daSegnoSegnoAllowed,
                Direction.DaSegnoSegnoAlFine => !ignore[(int)Direction.DaSegnoSegnoAlFine] && BarHasDirection(index, Direction.DaSegnoSegnoAlFine) && daSegnoSegnoAllowed,
                Direction.DaCoda => daCodaActivated && !ignore[(int)Direction.DaCoda] && BarHasDirection(index, Direction.DaCoda),
                Direction.DaDoubleCoda => daDoubleCodaActivated && !ignore[(int)Direction.DaDoubleCoda] && BarHasDirection(index, Direction.DaDoubleCoda) && daDoubleCodaAllowed,
                _ => false
            };
        }

        private bool BarHasDirection(int index, Direction direction)
        {
            var marker = directions.Get(direction);
            return marker >= 0 && marker == index;
        }

        private int LowerRepeatStartIndex(int barIndex)
        {
            for (var i = barIndex; i >= 0; i--)
            {
                if (masterBars[i].RepeatStart)
                {
                    return i;
                }
            }

            return -1;
        }

        private bool HasExtendedAlternateEndings(int index)
        {
            for (var i = index; i >= 0; i--)
            {
                var bar = masterBars[i];
                if (bar.HasAlternateEndings)
                {
                    var hasRepeatStart = bar.RepeatStart;
                    var previousHasRepeatEnd = i > 0 && masterBars[i - 1].RepeatEnd;
                    return !(previousHasRepeatEnd && hasRepeatStart);
                }

                if (bar.RepeatStart)
                {
                    break;
                }
            }

            return false;
        }

        private bool IsExtendedAlternateEndingSet(int index, int pass)
        {
            for (var i = index; i >= 0; i--)
            {
                var bar = masterBars[i];
                if (bar.HasAlternateEndings)
                {
                    return IsAlternateEndingSet(bar.AlternateEndingMask, pass);
                }

                if (bar.RepeatStart)
                {
                    return false;
                }
            }

            return false;
        }

        private static bool IsAlternateEndingSet(int mask, int pass)
        {
            if (pass <= 0 || pass > 31)
            {
                return false;
            }

            return (mask & (1 << (pass - 1))) != 0;
        }

        private void UnrollUnusedDaCoda()
        {
            if (directions.Coda == -1 || directions.DaCoda == -1 || directions.Coda <= directions.DaCoda)
            {
                return;
            }

            var toErase = new List<int>();
            var start = LastOccurrenceInPlaylist(directions.DaCoda) + 1;
            if (start == 0)
            {
                return;
            }

            while (start < playlist.Count && playlist[start] != directions.Coda)
            {
                toErase.Add(start);
                start++;
            }

            EraseFromPlaylist(toErase);
        }

        private void UnrollUnusedDaDoubleCoda()
        {
            if (directions.DoubleCoda == -1 || directions.DaDoubleCoda == -1 || directions.DoubleCoda <= directions.DaDoubleCoda)
            {
                return;
            }

            var toErase = new List<int>();
            var start = LastOccurrenceInPlaylist(directions.DaDoubleCoda) + 1;
            if (start == 0)
            {
                return;
            }

            while (start < playlist.Count && playlist[start] != directions.DoubleCoda)
            {
                toErase.Add(start);
                start++;
            }

            EraseFromPlaylist(toErase);
        }

        private int LastOccurrenceInPlaylist(int barIndex)
        {
            for (var i = playlist.Count - 1; i >= 0; i--)
            {
                if (playlist[i] == barIndex)
                {
                    return i;
                }
            }

            return -1;
        }

        private void EraseFromPlaylist(List<int> toErase)
        {
            for (var i = 0; i < toErase.Count; i++)
            {
                playlist.RemoveAt(toErase[i]);

                for (var j = i; j < toErase.Count; j++)
                {
                    toErase[j]--;
                }
            }
        }

        private void PruneExtendedAlternateEndings()
        {
            var toErase = new List<int>();
            var position = 0;
            var loopEnd = -1;
            var loopStart = -1;
            var pass = -1;

            foreach (var barIndex in playlist)
            {
                if (barIndex == -1)
                {
                    toErase.Add(position);
                    position++;
                    loopEnd = -1;
                    loopStart = -1;
                    pass = -1;
                    continue;
                }

                var bar = masterBars[barIndex];

                if (loopEnd >= 0 && barIndex > loopEnd)
                {
                    loopStart = -1;
                }

                if ((bar.RepeatStart || barIndex == scoreConcreteStart) && loopStart != barIndex)
                {
                    loopEnd = barIndex;
                    loopStart = barIndex;

                    for (var i = barIndex + 1; i < masterBars.Length; i++)
                    {
                        if (masterBars[i].RepeatStart)
                        {
                            break;
                        }

                        if (masterBars[i].RepeatEnd)
                        {
                            loopEnd = i;
                        }
                    }

                    pass = 1;
                }

                var skip = false;
                if (loopStart >= 0
                    && barIndex >= loopStart
                    && barIndex <= loopEnd
                    && HasExtendedAlternateEndings(barIndex)
                    && !IsExtendedAlternateEndingSet(barIndex, pass))
                {
                    toErase.Add(position);
                    skip = true;
                }

                if (barIndex == loopEnd)
                {
                    skip = false;
                }

                if (!skip && bar.RepeatEnd)
                {
                    pass++;
                }

                position++;
            }

            if (toErase.Count > 0)
            {
                EraseFromPlaylist(toErase);
            }
        }
    }

    private sealed record MasterBarState(
        int Index,
        bool RepeatStart,
        bool RepeatEnd,
        int RepeatCount,
        int AlternateEndingMask,
        string Jump,
        string Target,
        IReadOnlyDictionary<string, string> DirectionProperties)
    {
        public bool HasAlternateEndings => AlternateEndingMask != 0;

        public static MasterBarState From(GpifMasterBar source, int index)
            => new(
                Index: index,
                RepeatStart: source.RepeatStart,
                RepeatEnd: source.RepeatEnd,
                RepeatCount: Math.Max(0, source.RepeatCount),
                AlternateEndingMask: ParseAlternateEndingMask(source.AlternateEndings),
                Jump: source.Jump,
                Target: source.Target,
                DirectionProperties: source.DirectionProperties);

        private static int ParseAlternateEndingMask(string endings)
        {
            var mask = 0;
            foreach (var ending in ReferenceListParser.SplitRefs(endings))
            {
                if (ending is > 0 and <= 31)
                {
                    mask |= 1 << (ending - 1);
                }
            }

            return mask;
        }
    }

    private readonly record struct DirectionMap(
        int Coda,
        int DoubleCoda,
        int Segno,
        int SegnoSegno,
        int Fine,
        int DaCapo,
        int DaCapoAlCoda,
        int DaCapoAlDoubleCoda,
        int DaCapoAlFine,
        int DaSegno,
        int DaSegnoAlCoda,
        int DaSegnoAlDoubleCoda,
        int DaSegnoAlFine,
        int DaSegnoSegno,
        int DaSegnoSegnoAlCoda,
        int DaSegnoSegnoAlDoubleCoda,
        int DaSegnoSegnoAlFine,
        int DaCoda,
        int DaDoubleCoda)
    {
        public static DirectionMap From(IReadOnlyList<MasterBarState> bars)
            => new(
                Coda: FindLastMarker(bars, "Coda"),
                DoubleCoda: FindLastMarker(bars, "DoubleCoda"),
                Segno: FindLastMarker(bars, "Segno"),
                SegnoSegno: FindLastMarker(bars, "SegnoSegno"),
                Fine: FindLastMarker(bars, "Fine"),
                DaCapo: FindLastMarker(bars, "DaCapo"),
                DaCapoAlCoda: FindLastMarker(bars, "DaCapoAlCoda"),
                DaCapoAlDoubleCoda: FindLastMarker(bars, "DaCapoAlDoubleCoda"),
                DaCapoAlFine: FindLastMarker(bars, "DaCapoAlFine"),
                DaSegno: FindLastMarker(bars, "DaSegno"),
                DaSegnoAlCoda: FindLastMarker(bars, "DaSegnoAlCoda"),
                DaSegnoAlDoubleCoda: FindLastMarker(bars, "DaSegnoAlDoubleCoda"),
                DaSegnoAlFine: FindLastMarker(bars, "DaSegnoAlFine"),
                DaSegnoSegno: FindLastMarker(bars, "DaSegnoSegno"),
                DaSegnoSegnoAlCoda: FindLastMarker(bars, "DaSegnoSegnoAlCoda"),
                DaSegnoSegnoAlDoubleCoda: FindLastMarker(bars, "DaSegnoSegnoAlDoubleCoda"),
                DaSegnoSegnoAlFine: FindLastMarker(bars, "DaSegnoSegnoAlFine"),
                DaCoda: FindLastMarker(bars, "DaCoda"),
                DaDoubleCoda: FindLastMarker(bars, "DaDoubleCoda"));

        public int Get(Direction direction)
            => direction switch
            {
                Direction.Coda => Coda,
                Direction.DoubleCoda => DoubleCoda,
                Direction.Segno => Segno,
                Direction.SegnoSegno => SegnoSegno,
                Direction.Fine => Fine,
                Direction.DaCapo => DaCapo,
                Direction.DaCapoAlCoda => DaCapoAlCoda,
                Direction.DaCapoAlDoubleCoda => DaCapoAlDoubleCoda,
                Direction.DaCapoAlFine => DaCapoAlFine,
                Direction.DaSegno => DaSegno,
                Direction.DaSegnoAlCoda => DaSegnoAlCoda,
                Direction.DaSegnoAlDoubleCoda => DaSegnoAlDoubleCoda,
                Direction.DaSegnoAlFine => DaSegnoAlFine,
                Direction.DaSegnoSegno => DaSegnoSegno,
                Direction.DaSegnoSegnoAlCoda => DaSegnoSegnoAlCoda,
                Direction.DaSegnoSegnoAlDoubleCoda => DaSegnoSegnoAlDoubleCoda,
                Direction.DaSegnoSegnoAlFine => DaSegnoSegnoAlFine,
                Direction.DaCoda => DaCoda,
                Direction.DaDoubleCoda => DaDoubleCoda,
                _ => -1
            };

        private static int FindLastMarker(IReadOnlyList<MasterBarState> bars, string token)
        {
            var index = -1;

            for (var i = 0; i < bars.Count; i++)
            {
                if (IsDirectionToken(bars[i], token))
                {
                    index = i;
                }
            }

            return index;
        }
    }

    private enum Direction
    {
        Coda = 0,
        DoubleCoda = 1,
        Segno = 2,
        SegnoSegno = 3,
        Fine = 4,
        DaCapo = 5,
        DaCapoAlCoda = 6,
        DaCapoAlDoubleCoda = 7,
        DaCapoAlFine = 8,
        DaSegno = 9,
        DaSegnoAlCoda = 10,
        DaSegnoAlDoubleCoda = 11,
        DaSegnoAlFine = 12,
        DaSegnoSegno = 13,
        DaSegnoSegnoAlCoda = 14,
        DaSegnoSegnoAlDoubleCoda = 15,
        DaSegnoSegnoAlFine = 16,
        DaCoda = 17,
        DaDoubleCoda = 18
    }

    private const int DirectionCount = 19;
}
