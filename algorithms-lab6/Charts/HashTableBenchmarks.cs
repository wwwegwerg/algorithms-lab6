using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace algorithms_lab6.Charts;

public static class HashTableBenchmarks {
    public sealed record Config(
        int ElementCount = 100_000,
        int Capacity = 1000,
        int Trials = 20,
        int Seed = 42
    );

    private sealed record ChartDef(Func<Config, string> Title, Func<Config, ChartData> Run);

    private static IReadOnlyList<ChartDef> Defs => [
        new(
            c => $"Генерация {c.ElementCount:N0} ключей",
            Gen
        ),
        new(
            c => $"Вставка {c.ElementCount:N0} элементов",
            Ins
        ),
        new(
            c => $"Коэффициент заполнения α = n/m ({c.ElementCount:N0} элементов)",
            Lf
        ),
        new(
            c => $"Средний коэффициент заполнения α (по {c.Trials} прогонам)",
            LfAvg
        ),
        new(
            c => $"Максимальная длина цепочки ({c.ElementCount:N0} элементов)",
            Max
        ),
        new(
            _ => "Минимальная длина цепочки (включая пустые ячейки)",
            Min0
        ),
        new(
            _ => "Минимальная длина цепочки (без пустых ячеек)",
            Min1
        )
    ];

    public static IReadOnlyList<string> GetChartTitles(Config? config = null) {
        var c = config ?? new Config();
        var a = Defs;
        var r = new string[a.Count];

        for (var i = 0; i < r.Length; i++) {
            r[i] = a[i].Title(c);
        }

        return r;
    }

    public static ChartData Build(string title, Config? config = null) {
        var c = config ?? new Config();
        Check(c);

        var a = Defs;
        for (var i = 0; i < a.Count; i++) {
            var d = a[i];
            if (d.Title(c) != title) {
                continue;
            }

            return d.Run(c);
        }

        throw new ArgumentOutOfRangeException(nameof(title), "Unknown chart title.");
    }

    private static void Check(Config c) {
        if (c.ElementCount <= 0) {
            throw new ArgumentOutOfRangeException(nameof(c.ElementCount), "ElementCount must be positive.");
        }

        if (c.Capacity <= 0) {
            throw new ArgumentOutOfRangeException(nameof(c.Capacity), "Capacity must be positive.");
        }

        if (c.Trials <= 0) {
            throw new ArgumentOutOfRangeException(nameof(c.Trials), "Trials must be positive.");
        }
    }

    private static ChartData Gen(Config c) {
        var p = new List<DataPoint>(c.Trials);
        double sum = 0;

        for (var t = 1; t <= c.Trials; t++) {
            var sw = Stopwatch.StartNew();
            _ = Keys(c.ElementCount, c.Seed + t);
            sw.Stop();

            var ms = sw.Elapsed.TotalMilliseconds;
            sum += ms;
            p.Add(new DataPoint(t, ms));
        }

        return new ChartData(
            title: $"Генерация {c.ElementCount:N0} ключей",
            results: new List<(string, IList<DataPoint>)> { ("Генерация", p) },
            xAxisTitle: "Номер прогона",
            yAxisTitle: "Время, мс",
            totalExecTimeSeconds: sum / 1000.0
        );
    }

    private static ChartData Ins(Config c) {
        Warm(c);

        var d = new List<DataPoint>(c.Trials);
        var m = new List<DataPoint>(c.Trials);
        double sum = 0;

        for (var t = 1; t <= c.Trials; t++) {
            var k = Keys(c.ElementCount, c.Seed + t);

            var a = InsOnly(new DivisionHashStrategy(), k, c.Capacity);
            d.Add(new DataPoint(t, a));
            sum += a;

            var b = InsOnly(new MultiplicationHashStrategy(), k, c.Capacity);
            m.Add(new DataPoint(t, b));
            sum += b;
        }

        return new ChartData(
            title: $"Вставка {c.ElementCount:N0} элементов",
            results: new List<(string, IList<DataPoint>)> {
                ("Деление", d),
                ("Умножение", m)
            },
            xAxisTitle: "Номер прогона",
            yAxisTitle: "Время, мс",
            totalExecTimeSeconds: sum / 1000.0
        );
    }

    private static ChartData Lf(Config c) {
        Warm(c);

        var d = new List<DataPoint>(c.Trials);
        var m = new List<DataPoint>(c.Trials);
        double sum = 0;

        for (var t = 1; t <= c.Trials; t++) {
            var k = Keys(c.ElementCount, c.Seed + t);

            var a = LfOnce(new DivisionHashStrategy(), k, c.Capacity);
            d.Add(new DataPoint(t, a.Lf));
            sum += a.Ms;

            var b = LfOnce(new MultiplicationHashStrategy(), k, c.Capacity);
            m.Add(new DataPoint(t, b.Lf));
            sum += b.Ms;
        }

        return new ChartData(
            title: $"Коэффициент заполнения α = n/m ({c.ElementCount:N0} элементов)",
            results: new List<(string, IList<DataPoint>)> {
                ("Деление", d),
                ("Умножение", m)
            },
            xAxisTitle: "Номер прогона",
            yAxisTitle: "α",
            totalExecTimeSeconds: sum / 1000.0
        );
    }

    private static ChartData LfAvg(Config c) {
        Warm(c);

        double sum = 0;
        double a = 0;
        double b = 0;

        for (var t = 1; t <= c.Trials; t++) {
            var k = Keys(c.ElementCount, c.Seed + t);

            var d = LfOnce(new DivisionHashStrategy(), k, c.Capacity);
            a += d.Lf;
            sum += d.Ms;

            var m = LfOnce(new MultiplicationHashStrategy(), k, c.Capacity);
            b += m.Lf;
            sum += m.Ms;
        }

        a /= c.Trials;
        b /= c.Trials;

        return new ChartData(
            title: $"Средний коэффициент заполнения α (по {c.Trials} прогонам)",
            results: new List<(string, IList<DataPoint>)> {
                ("Деление", new List<DataPoint> { new DataPoint(1, a) }),
                ("Умножение", new List<DataPoint> { new DataPoint(1, b) })
            },
            xAxisTitle: "Стратегия (1 = среднее)",
            yAxisTitle: "Средний α",
            totalExecTimeSeconds: sum / 1000.0
        );
    }

    private static ChartData Max(Config c) {
        Warm(c);

        var d = new List<DataPoint>(c.Trials);
        var m = new List<DataPoint>(c.Trials);
        double sum = 0;

        for (var t = 1; t <= c.Trials; t++) {
            var k = Keys(c.ElementCount, c.Seed + t);

            var a = Chain(new DivisionHashStrategy(), k, c.Capacity);
            d.Add(new DataPoint(t, a.Max));
            sum += a.Ms;

            var b = Chain(new MultiplicationHashStrategy(), k, c.Capacity);
            m.Add(new DataPoint(t, b.Max));
            sum += b.Ms;
        }

        return new ChartData(
            title: $"Максимальная длина цепочки ({c.ElementCount:N0} элементов)",
            results: new List<(string, IList<DataPoint>)> {
                ("Деление", d),
                ("Умножение", m)
            },
            xAxisTitle: "Номер прогона",
            yAxisTitle: "Длина цепочки",
            totalExecTimeSeconds: sum / 1000.0
        );
    }

    private static ChartData Min0(Config c) {
        Warm(c);

        var d = new List<DataPoint>(c.Trials);
        var m = new List<DataPoint>(c.Trials);
        double sum = 0;

        for (var t = 1; t <= c.Trials; t++) {
            var k = Keys(c.ElementCount, c.Seed + t);

            var a = Chain(new DivisionHashStrategy(), k, c.Capacity);
            d.Add(new DataPoint(t, a.Min0));
            sum += a.Ms;

            var b = Chain(new MultiplicationHashStrategy(), k, c.Capacity);
            m.Add(new DataPoint(t, b.Min0));
            sum += b.Ms;
        }

        return new ChartData(
            title: "Минимальная длина цепочки (включая пустые ячейки)",
            results: new List<(string, IList<DataPoint>)> {
                ("Деление", d),
                ("Умножение", m)
            },
            xAxisTitle: "Номер прогона",
            yAxisTitle: "Длина цепочки",
            totalExecTimeSeconds: sum / 1000.0
        );
    }

    private static ChartData Min1(Config c) {
        Warm(c);

        var d = new List<DataPoint>(c.Trials);
        var m = new List<DataPoint>(c.Trials);
        double sum = 0;

        for (var t = 1; t <= c.Trials; t++) {
            var k = Keys(c.ElementCount, c.Seed + t);

            var a = Chain(new DivisionHashStrategy(), k, c.Capacity);
            d.Add(new DataPoint(t, a.Min1));
            sum += a.Ms;

            var b = Chain(new MultiplicationHashStrategy(), k, c.Capacity);
            m.Add(new DataPoint(t, b.Min1));
            sum += b.Ms;
        }

        return new ChartData(
            title: "Минимальная длина цепочки (без пустых ячеек)",
            results: new List<(string, IList<DataPoint>)> {
                ("Деление", d),
                ("Умножение", m)
            },
            xAxisTitle: "Номер прогона",
            yAxisTitle: "Длина цепочки",
            totalExecTimeSeconds: sum / 1000.0
        );
    }

    private static void Warm(Config c) {
        var w = c.ElementCount;
        if (w > 10_000) {
            w = 10_000;
        }

        var k = Keys(w, c.Seed);
        _ = LfOnce(new DivisionHashStrategy(), k, c.Capacity);
        _ = LfOnce(new MultiplicationHashStrategy(), k, c.Capacity);
    }

    private static int[] Keys(int n, int seed) {
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

    private static double InsOnly(IHashStrategy<int> s, int[] k, int cap) {
        var t = new ChainedHashTable<int, int>(s, cap);

        var sw = Stopwatch.StartNew();
        for (var i = 0; i < k.Length; i++) {
            t.AddOrUpdate(k[i], k[i]);
        }

        sw.Stop();
        return sw.Elapsed.TotalMilliseconds;
    }

    private static LfStat LfOnce(IHashStrategy<int> s, int[] k, int cap) {
        var t = new ChainedHashTable<int, int>(s, cap);

        var sw = Stopwatch.StartNew();
        for (var i = 0; i < k.Length; i++) {
            t.AddOrUpdate(k[i], k[i]);
        }

        var a = t.LoadFactor;
        sw.Stop();

        return new LfStat(sw.Elapsed.TotalMilliseconds, a);
    }

    private static ChainStat Chain(IHashStrategy<int> s, int[] k, int cap) {
        var t = new ChainedHashTable<int, int>(s, cap);

        var sw = Stopwatch.StartNew();
        for (var i = 0; i < k.Length; i++) {
            t.AddOrUpdate(k[i], k[i]);
        }

        t.GetChainLengthStats(ignoreEmpty: false, out var min0, out var max);
        t.GetChainLengthStats(ignoreEmpty: true, out var min1, out _);
        sw.Stop();

        return new ChainStat(sw.Elapsed.TotalMilliseconds, min0, min1, max);
    }

    private readonly record struct LfStat(double Ms, double Lf);
    private readonly record struct ChainStat(double Ms, int Min0, int Min1, int Max);
}
