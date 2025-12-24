using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace algorithms_lab6.Charts;

public static class ChainedHashTableBenchmarks {
    public sealed record Config(
        int ElementCount = 100_000,
        int Capacity = 1000,
        int Trials = 20,
        int Seed = 42
    );

    private sealed record ChartDef(Func<Config, string> Title, Func<Config, ChartData> Run);

    private sealed record HashCase(string Name, Func<IHashStrategy<int>> Hash);

    private static IReadOnlyList<HashCase> HashCases => [
        new("Деление", () => new DivisionHashStrategy()),
        new("Умножение", () => new MultiplicationHashStrategy()),
        new("BitMix", () => new BitMixHashStrategy()),
        new("FNV-like", () => new FnvLikeHashStrategy()),
        new("XorShift", () => new XorShiftHashStrategy())
    ];

    private static IReadOnlyList<ChartDef> Defs => [
        new(
            c => $"Цепочки: генерация {c.ElementCount:N0} ключей",
            Gen
        ),
        new(
            c => $"Цепочки: вставка {c.ElementCount:N0} элементов",
            Ins
        ),
        new(
            c => $"Цепочки: коэффициент заполнения α = n/m ({c.ElementCount:N0} элементов)",
            Lf
        ),
        new(
            c => $"Цепочки: средний коэффициент заполнения α (по {c.Trials} прогонам)",
            LfAvg
        ),
        new(
            c => $"Цепочки: максимальная длина цепочки ({c.ElementCount:N0} элементов)",
            Max
        ),
        new(
            _ => "Цепочки: минимальная длина цепочки (включая пустые ячейки)",
            Min0
        ),
        new(
            _ => "Цепочки: минимальная длина цепочки (без пустых ячеек)",
            Min1
        )
    ];

    public static IReadOnlyList<string> GetChartTitles(Config? config = null) {
        var c = config ?? new Config();
        Check(c);

        var a = Defs;
        var r = new string[a.Count];

        for (var i = 0; i < r.Length; i++) {
            r[i] = a[i].Title(c);
        }

        return r;
    }

    public static bool TryBuild(string title, out ChartData data, Config? config = null) {
        var c = config ?? new Config();
        Check(c);

        var a = Defs;
        for (var i = 0; i < a.Count; i++) {
            var d = a[i];
            if (d.Title(c) != title) {
                continue;
            }

            data = d.Run(c);
            return true;
        }

        data = null!;
        return false;
    }

    public static ChartData Build(string title, Config? config = null) {
        if (TryBuild(title, out var data, config)) {
            return data;
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
            _ = BenchmarkUtils.Keys(c.ElementCount, c.Seed + t);
            sw.Stop();

            var ms = sw.Elapsed.TotalMilliseconds;
            sum += ms;
            p.Add(new DataPoint(t, ms));
        }

        return new ChartData(
            title: $"Цепочки: генерация {c.ElementCount:N0} ключей",
            results: new List<(string, IList<DataPoint>)> { ("Генерация", p) },
            xAxisTitle: "Номер прогона",
            yAxisTitle: "Время, мс",
            totalExecTimeSeconds: sum / 1000.0
        );
    }

    private static ChartData Ins(Config c) {
        Warm(c);

        var cases = HashCases;
        var series = new List<DataPoint>[cases.Count];
        for (var i = 0; i < series.Length; i++) {
            series[i] = new List<DataPoint>(c.Trials);
        }

        double sum = 0;

        for (var t = 1; t <= c.Trials; t++) {
            var k = BenchmarkUtils.Keys(c.ElementCount, c.Seed + t);

            for (var s = 0; s < cases.Count; s++) {
                var ms = InsOnly(cases[s].Hash(), k, c.Capacity);
                series[s].Add(new DataPoint(t, ms));
                sum += ms;
            }
        }

        var results = new List<(string, IList<DataPoint>)>(cases.Count);
        for (var i = 0; i < cases.Count; i++) {
            results.Add((cases[i].Name, series[i]));
        }

        return new ChartData(
            title: $"Цепочки: вставка {c.ElementCount:N0} элементов",
            results: results,
            xAxisTitle: "Номер прогона",
            yAxisTitle: "Время, мс",
            totalExecTimeSeconds: sum / 1000.0
        );
    }

    private static ChartData Lf(Config c) {
        Warm(c);

        var cases = HashCases;
        var series = new List<DataPoint>[cases.Count];
        for (var i = 0; i < series.Length; i++) {
            series[i] = new List<DataPoint>(c.Trials);
        }

        double sum = 0;

        for (var t = 1; t <= c.Trials; t++) {
            var k = BenchmarkUtils.Keys(c.ElementCount, c.Seed + t);

            for (var s = 0; s < cases.Count; s++) {
                var a = LfOnce(cases[s].Hash(), k, c.Capacity);
                series[s].Add(new DataPoint(t, a.Lf));
                sum += a.Ms;
            }
        }

        var results = new List<(string, IList<DataPoint>)>(cases.Count);
        for (var i = 0; i < cases.Count; i++) {
            results.Add((cases[i].Name, series[i]));
        }

        return new ChartData(
            title: $"Цепочки: коэффициент заполнения α = n/m ({c.ElementCount:N0} элементов)",
            results: results,
            xAxisTitle: "Номер прогона",
            yAxisTitle: "α",
            totalExecTimeSeconds: sum / 1000.0
        );
    }

    private static ChartData LfAvg(Config c) {
        Warm(c);

        var cases = HashCases;
        var avg = new double[cases.Count];
        double sum = 0;

        for (var t = 1; t <= c.Trials; t++) {
            var k = BenchmarkUtils.Keys(c.ElementCount, c.Seed + t);

            for (var s = 0; s < cases.Count; s++) {
                var d = LfOnce(cases[s].Hash(), k, c.Capacity);
                avg[s] += d.Lf;
                sum += d.Ms;
            }
        }

        var results = new List<(string, IList<DataPoint>)>(cases.Count);
        for (var i = 0; i < cases.Count; i++) {
            results.Add((cases[i].Name, new List<DataPoint> { new DataPoint(1, avg[i] / c.Trials) }));
        }

        return new ChartData(
            title: $"Цепочки: средний коэффициент заполнения α (по {c.Trials} прогонам)",
            results: results,
            xAxisTitle: "Стратегия (точка = среднее)",
            yAxisTitle: "Средний α",
            totalExecTimeSeconds: sum / 1000.0
        );
    }

    private static ChartData Max(Config c) {
        Warm(c);

        var cases = HashCases;
        var series = new List<DataPoint>[cases.Count];
        for (var i = 0; i < series.Length; i++) {
            series[i] = new List<DataPoint>(c.Trials);
        }

        double sum = 0;

        for (var t = 1; t <= c.Trials; t++) {
            var k = BenchmarkUtils.Keys(c.ElementCount, c.Seed + t);

            for (var s = 0; s < cases.Count; s++) {
                var a = Chain(cases[s].Hash(), k, c.Capacity);
                series[s].Add(new DataPoint(t, a.Max));
                sum += a.Ms;
            }
        }

        var results = new List<(string, IList<DataPoint>)>(cases.Count);
        for (var i = 0; i < cases.Count; i++) {
            results.Add((cases[i].Name, series[i]));
        }

        return new ChartData(
            title: $"Цепочки: максимальная длина цепочки ({c.ElementCount:N0} элементов)",
            results: results,
            xAxisTitle: "Номер прогона",
            yAxisTitle: "Длина цепочки",
            totalExecTimeSeconds: sum / 1000.0
        );
    }

    private static ChartData Min0(Config c) {
        Warm(c);

        var cases = HashCases;
        var series = new List<DataPoint>[cases.Count];
        for (var i = 0; i < series.Length; i++) {
            series[i] = new List<DataPoint>(c.Trials);
        }

        double sum = 0;

        for (var t = 1; t <= c.Trials; t++) {
            var k = BenchmarkUtils.Keys(c.ElementCount, c.Seed + t);

            for (var s = 0; s < cases.Count; s++) {
                var a = Chain(cases[s].Hash(), k, c.Capacity);
                series[s].Add(new DataPoint(t, a.Min0));
                sum += a.Ms;
            }
        }

        var results = new List<(string, IList<DataPoint>)>(cases.Count);
        for (var i = 0; i < cases.Count; i++) {
            results.Add((cases[i].Name, series[i]));
        }

        return new ChartData(
            title: "Цепочки: минимальная длина цепочки (включая пустые ячейки)",
            results: results,
            xAxisTitle: "Номер прогона",
            yAxisTitle: "Длина цепочки",
            totalExecTimeSeconds: sum / 1000.0
        );
    }

    private static ChartData Min1(Config c) {
        Warm(c);

        var cases = HashCases;
        var series = new List<DataPoint>[cases.Count];
        for (var i = 0; i < series.Length; i++) {
            series[i] = new List<DataPoint>(c.Trials);
        }

        double sum = 0;

        for (var t = 1; t <= c.Trials; t++) {
            var k = BenchmarkUtils.Keys(c.ElementCount, c.Seed + t);

            for (var s = 0; s < cases.Count; s++) {
                var a = Chain(cases[s].Hash(), k, c.Capacity);
                series[s].Add(new DataPoint(t, a.Min1));
                sum += a.Ms;
            }
        }

        var results = new List<(string, IList<DataPoint>)>(cases.Count);
        for (var i = 0; i < cases.Count; i++) {
            results.Add((cases[i].Name, series[i]));
        }

        return new ChartData(
            title: "Цепочки: минимальная длина цепочки (без пустых ячеек)",
            results: results,
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

        var k = BenchmarkUtils.Keys(w, c.Seed);
        var cases = HashCases;

        for (var i = 0; i < cases.Count; i++) {
            _ = LfOnce(cases[i].Hash(), k, c.Capacity);
        }
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
