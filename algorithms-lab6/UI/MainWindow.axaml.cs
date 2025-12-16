using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using algorithms_lab6.Charts;

namespace algorithms_lab6.UI;

public partial class MainWindow : Window {
    private bool _run;

    public MainWindow() {
        InitializeComponent();

        ChartComboBox.ItemsSource = HashTableBenchmarks.GetChartTitles();
        StatusTextBlock.Text = "Выберите график.";

        ChartComboBox.SelectionChanged += OnPick;
    }

    private async void OnPick(object? s, SelectionChangedEventArgs e) {
        if (_run) {
            return;
        }

        var title = ChartComboBox.SelectedItem as string;
        if (string.IsNullOrWhiteSpace(title)) {
            return;
        }

        if (TempChartCache.TryGet(title, out var p)) {
            ChartWebView.Url = new UriBuilder { Scheme = Uri.UriSchemeFile, Path = p }.Uri;
            StatusTextBlock.Text = $"Готово: {title}";
            return;
        }

        _run = true;
        ChartComboBox.IsEnabled = false;

        var sw = Stopwatch.StartNew();
        var t = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        t.Tick += (_, _) => StatusTextBlock.Text = $"Выполняется… прошло {sw.Elapsed.TotalSeconds:0.0} сек";
        t.Start();

        try {
            var path = await Task.Run(() => TempChartCache.GetOrCreate(title, () => {
                var cd = HashTableBenchmarks.Build(title);
                return ChartBuilder.Build2DLineChart(cd, TempChartCache.RootDir, promptOnOverwrite: false);
            }));

            ChartWebView.Url = new UriBuilder { Scheme = Uri.UriSchemeFile, Path = path }.Uri;
            StatusTextBlock.Text = $"Готово: {title}";
        } catch (Exception ex) {
            StatusTextBlock.Text = "Ошибка: " + ex.Message;
        } finally {
            t.Stop();
            sw.Stop();

            _run = false;
            ChartComboBox.IsEnabled = true;
        }
    }
}
