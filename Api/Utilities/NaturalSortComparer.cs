using System.Text.RegularExpressions;

namespace Api.Utilities;

public sealed partial class NaturalSortComparer : IComparer<string?>
{
    public static readonly NaturalSortComparer Instance = new();

    public int Compare(string? x, string? y)
    {
        if (x == y) return 0;
        if (x is null) return -1;
        if (y is null) return 1;

        var xParts = SplitRegex().Matches(x);
        var yParts = SplitRegex().Matches(y);

        var len = Math.Min(xParts.Count, yParts.Count);
        for (var i = 0; i < len; i++)
        {
            var xVal = xParts[i].Value;
            var yVal = yParts[i].Value;

            int cmp;
            if (int.TryParse(xVal, out var xNum) && int.TryParse(yVal, out var yNum))
            {
                cmp = xNum.CompareTo(yNum);
            }
            else
            {
                cmp = string.Compare(xVal, yVal, StringComparison.OrdinalIgnoreCase);
            }

            if (cmp != 0) return cmp;
        }

        return xParts.Count.CompareTo(yParts.Count);
    }

    [GeneratedRegex(@"(\d+|\D+)")]
    private static partial Regex SplitRegex();
}
