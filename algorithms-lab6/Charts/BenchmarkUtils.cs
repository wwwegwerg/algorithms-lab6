using System;

namespace algorithms_lab6.Charts;

public static class BenchmarkUtils {
    public static int[] Keys(int n, int seed) {
        if (n < 0) {
            throw new ArgumentOutOfRangeException(nameof(n));
        }

        var a = new int[n];
        for (var i = 0; i < n; i++) {
            a[i] = i;
        }

        var r = new Random(seed);
        for (var i = n - 1; i > 0; i--) {
            var j = r.Next(i + 1);
            var tmp = a[i];
            a[i] = a[j];
            a[j] = tmp;
        }

        return a;
    }
}
