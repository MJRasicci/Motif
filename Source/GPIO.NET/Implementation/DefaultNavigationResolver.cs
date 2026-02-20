namespace GPIO.NET.Implementation;

using GPIO.NET.Abstractions;
using GPIO.NET.Models.Raw;
using GPIO.NET.Utilities;

public sealed class DefaultNavigationResolver : INavigationResolver
{
    public IReadOnlyList<int> BuildPlaybackSequence(IReadOnlyList<GpifMasterBar> masterBars)
    {
        var bars = masterBars.OrderBy(m => m.Index).ToArray();
        if (bars.Length == 0)
        {
            return Array.Empty<int>();
        }

        var result = new List<int>(bars.Length * 2);

        var segnoIndex = bars.FirstOrDefault(b => string.Equals(b.Target, "Segno", StringComparison.OrdinalIgnoreCase))?.Index ?? 0;
        var codaIndex = bars.FirstOrDefault(b => string.Equals(b.Target, "Coda", StringComparison.OrdinalIgnoreCase))?.Index ?? -1;

        var repeatStack = new Stack<int>();
        var repeatVisits = new Dictionary<int, int>();
        var consumedJumps = new HashSet<int>();

        var cursor = 0;
        var guard = 0;
        var guardLimit = Math.Max(10_000, bars.Length * 128);

        while (cursor >= 0 && cursor < bars.Length && guard++ < guardLimit)
        {
            var bar = bars[cursor];

            if (bar.RepeatStart && (repeatStack.Count == 0 || repeatStack.Peek() != cursor))
            {
                repeatStack.Push(cursor);
            }

            var endingVisit = repeatVisits.TryGetValue(cursor, out var visit) ? visit + 1 : 1;
            if (!ShouldPlayAlternateEnding(bar.AlternateEndings, endingVisit))
            {
                cursor++;
                continue;
            }

            result.Add(cursor);

            if (!consumedJumps.Contains(cursor) && TryResolveJump(bar.Jump, segnoIndex, codaIndex, out var jumpIndex))
            {
                consumedJumps.Add(cursor);
                cursor = jumpIndex;
                continue;
            }

            if (bar.RepeatEnd && repeatStack.Count > 0)
            {
                var start = repeatStack.Peek();
                var count = repeatVisits.TryGetValue(cursor, out var done) ? done : 0;
                var maxPasses = Math.Max(2, bar.RepeatCount <= 0 ? 2 : bar.RepeatCount);

                if (count < maxPasses - 1)
                {
                    repeatVisits[cursor] = count + 1;
                    cursor = start;
                    continue;
                }

                repeatStack.Pop();
            }

            cursor++;
        }

        return result;
    }

    private static bool ShouldPlayAlternateEnding(string alternateEndings, int repeatVisit)
    {
        var endings = ReferenceListParser.SplitRefs(alternateEndings);
        if (endings.Count == 0)
        {
            return true;
        }

        return endings.Contains(repeatVisit);
    }

    private static bool TryResolveJump(string jump, int segnoIndex, int codaIndex, out int index)
    {
        index = -1;
        if (string.IsNullOrWhiteSpace(jump))
        {
            return false;
        }

        if (jump.StartsWith("DaCapo", StringComparison.OrdinalIgnoreCase))
        {
            index = 0;
            return true;
        }

        if (jump.StartsWith("DaSegno", StringComparison.OrdinalIgnoreCase))
        {
            index = segnoIndex;
            return true;
        }

        if (jump.StartsWith("DaCoda", StringComparison.OrdinalIgnoreCase) && codaIndex >= 0)
        {
            index = codaIndex;
            return true;
        }

        return false;
    }
}
