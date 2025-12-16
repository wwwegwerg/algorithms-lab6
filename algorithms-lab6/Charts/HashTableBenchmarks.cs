using System;
using System.Collections.Generic;

namespace algorithms_lab6.Charts;

public static class HashTableBenchmarks {
    public static IReadOnlyList<string> GetChartTitles() {
        var r = new List<string>();

        r.AddRange(ChainedHashTableBenchmarks.GetChartTitles());
        r.AddRange(OpenAddressingHashTableBenchmarks.GetChartTitles());

        return r;
    }

    public static ChartData Build(string title) {
        if (ChainedHashTableBenchmarks.TryBuild(title, out var chained)) {
            return chained;
        }

        if (OpenAddressingHashTableBenchmarks.TryBuild(title, out var oa)) {
            return oa;
        }

        throw new ArgumentOutOfRangeException(nameof(title), "Unknown chart title.");
    }
}
