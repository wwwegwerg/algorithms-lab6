using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace algorithms_lab6.Charts;

public static class OpenAddressingHashTableBenchmarks {
    public sealed record Config(
        int ElementCount = 10_000,
        int Capacity = 10_000,
        int OverflowCapacity = 9_000,
        int Trials = 20,
        int Seed = 42
    );

    private sealed record ChartDef(Func<Config, string> Title, Func<Config, ChartData> Run);

    private sealed record OaCase(string Name, Func<IProbingStrategy<int>> Probe);

    private static IReadOnlyList<OaCase> OaCases => [
        new(
            "Линейное (h'=деление)",
            () => new LinearProbingStrategy<int>(new DivisionHashStrategy())
        ),
        new(
            "Квадратичное c1=1 c2=3 (h'=деление)",
            () => new QuadraticProbingStrategy<int>(new DivisionHashStrategy(), c1: 1, c2: 3)
        ),
        new(
            "Двойное (h1=деление, h2=умножение)",
            () => new DoubleHashingStrategy<int>(new DivisionHashStrategy(), new MultiplicationHashStrategy())
        ),
        new(
            "Псевдослучайное (h'=деление)",
            () => new PseudoRandomProbingStrategy<int>(new DivisionHashStrategy())
        ),
        new(
            "Квадратичное + сдвиг shift=7 (h'=деление)",
            () => new QuadraticShiftProbingStrategy<int>(new DivisionHashStrategy(), shift: 7)
        )
    ];

    private static IReadOnlyList<ChartDef> Defs => [
        new(
            c => $"Открытая адресация: генерация {c.ElementCount:N0} ключей",
            Gen
        ),
        new(
            c => $"Открытая адресация: вставка {c.ElementCount:N0} элементов",
            Insert
        ),
        new(
            c => $"Открытая адресация: переполнение (m={c.OverflowCapacity})",
            Overflow
        ),
        new(
            c => $"Открытая адресация: максимальный кластер ({c.ElementCount:N0} элементов)",
            MaxCluster
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

        if (c.OverflowCapacity <= 0) {
            throw new ArgumentOutOfRangeException(nameof(c.OverflowCapacity), "OverflowCapacity must be positive.");
        }

        if (c.Trials <= 0) {
            throw new ArgumentOutOfRangeException(nameof(c.Trials), "Trials must be positive.");
        }

        if (c.OverflowCapacity >= c.Capacity) {
            throw new ArgumentOutOfRangeException(nameof(c.OverflowCapacity), "OverflowCapacity must be smaller than Capacity.");
        }

        if (c.ElementCount > c.Capacity) {
            // For the non-overflow charts we want α <= 1.
            throw new ArgumentOutOfRangeException(nameof(c.Capacity), "Capacity must be >= ElementCount for open addressing benchmarks.");
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
            title: $"Открытая адресация: генерация {c.ElementCount:N0} ключей",
            results: new List<(string, IList<DataPoint>)> { ("Генерация", p) },
            xAxisTitle: "Номер прогона",
            yAxisTitle: "Время, мс",
            totalExecTimeSeconds: sum / 1000.0
        );
    }

    private static ChartData Insert(Config c) {
        var cases = OaCases;
        var series = new List<DataPoint>[cases.Count];

        for (var i = 0; i < series.Length; i++) {
            series[i] = new List<DataPoint>(c.Trials);
        }

        double sum = 0;

        for (var t = 1; t <= c.Trials; t++) {
            var keys = BenchmarkUtils.Keys(c.ElementCount, c.Seed + t);

            for (var s = 0; s < cases.Count; s++) {
                var table = new OpenAddressingHashTable<int, int>(cases[s].Probe(), c.Capacity);

                var sw = Stopwatch.StartNew();
                for (var i = 0; i < keys.Length; i++) {
                    table.AddOrUpdate(keys[i], keys[i]);
                }

                sw.Stop();

                var ms = sw.Elapsed.TotalMilliseconds;
                sum += ms;
                series[s].Add(new DataPoint(t, ms));
            }
        }

        var results = new List<(string, IList<DataPoint>)>(cases.Count);
        for (var i = 0; i < cases.Count; i++) {
            results.Add((cases[i].Name, series[i]));
        }

        return new ChartData(
            title: $"Открытая адресация: вставка {c.ElementCount:N0} элементов",
            results: results,
            xAxisTitle: "Номер прогона",
            yAxisTitle: "Время, мс",
            totalExecTimeSeconds: sum / 1000.0
        );
    }

    private static ChartData Overflow(Config c) {
        var cases = OaCases;
        var insertedSeries = new List<DataPoint>[cases.Count];

        for (var i = 0; i < insertedSeries.Length; i++) {
            insertedSeries[i] = new List<DataPoint>(c.Trials);
        }

        double sum = 0;

        for (var t = 1; t <= c.Trials; t++) {
            var keys = BenchmarkUtils.Keys(c.ElementCount, c.Seed + t);

            for (var s = 0; s < cases.Count; s++) {
                var table = new OpenAddressingHashTable<int, int>(cases[s].Probe(), c.OverflowCapacity);

                var inserted = 0;
                var sw = Stopwatch.StartNew();

                for (var i = 0; i < keys.Length; i++) {
                    try {
                        table.AddOrUpdate(keys[i], keys[i]);
                        inserted++;
                    } catch (InvalidOperationException) {
                        break;
                    }
                }

                sw.Stop();
                sum += sw.Elapsed.TotalMilliseconds;

                insertedSeries[s].Add(new DataPoint(t, inserted));
            }
        }

        var results = new List<(string, IList<DataPoint>)>(cases.Count);
        for (var i = 0; i < cases.Count; i++) {
            results.Add((cases[i].Name, insertedSeries[i]));
        }

        return new ChartData(
            title: $"Открытая адресация: переполнение (m={c.OverflowCapacity})",
            results: results,
            xAxisTitle: "Номер прогона",
            yAxisTitle: "Вставлено до переполнения",
            totalExecTimeSeconds: sum / 1000.0
        );
    }

    private static ChartData MaxCluster(Config c) {
        var cases = OaCases;
        var series = new List<DataPoint>[cases.Count];

        for (var i = 0; i < series.Length; i++) {
            series[i] = new List<DataPoint>(c.Trials);
        }

        double sum = 0;

        for (var t = 1; t <= c.Trials; t++) {
            var keys = BenchmarkUtils.Keys(c.ElementCount, c.Seed + t);

            for (var s = 0; s < cases.Count; s++) {
                var table = new OpenAddressingHashTable<int, int>(cases[s].Probe(), c.Capacity);

                var sw = Stopwatch.StartNew();
                for (var i = 0; i < keys.Length; i++) {
                    table.AddOrUpdate(keys[i], keys[i]);
                }

                var maxCluster = table.GetMaxClusterLength();
                sw.Stop();

                sum += sw.Elapsed.TotalMilliseconds;
                series[s].Add(new DataPoint(t, maxCluster));
            }
        }

        var results = new List<(string, IList<DataPoint>)>(cases.Count);
        for (var i = 0; i < cases.Count; i++) {
            results.Add((cases[i].Name, series[i]));
        }

        return new ChartData(
            title: $"Открытая адресация: максимальный кластер ({c.ElementCount:N0} элементов)",
            results: results,
            xAxisTitle: "Номер прогона",
            yAxisTitle: "Максимальная длина кластера",
            totalExecTimeSeconds: sum / 1000.0
        );
    }
}
