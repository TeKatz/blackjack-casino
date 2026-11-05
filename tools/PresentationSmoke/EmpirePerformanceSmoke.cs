using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using BlackjackCasino.Core.Empire;
using BlackjackCasino.Desktop;

// A repeatable fixture, not a hardware certification. Captures actual values on
// the executing Windows runner; unavailable counters are explicitly identified.
internal static class EmpirePerformanceSmoke
{
    internal static async Task Run(MainWindow main, string output)
    {
        var results = new List<object>();
        var environment = Find<ModernLobbyView>(main).FirstOrDefault()?.Browser.CoreWebView2?.Environment;
        var host = new Window { Title = "Empire performance fixture", Width = 1366, Height = 768, Owner = main,
            WindowStartupLocation = WindowStartupLocation.CenterOwner };
        bool visible = main.IsVisible;
        main.Hide();
        try
        {
            foreach (var size in new[] { (1080d, 720d), (1366d, 768d), (1920d, 1080d) })
            {
                host.Width = size.Item1; host.Height = size.Item2;
                foreach (bool modern in new[] { false, true })
                {
                    var service = EmpireWebSmoke.Fixture();
                    ModernEmpireView? web = null; EmpireView? legacy = null;
                    if (modern)
                    {
                        web = new ModernEmpireView(() => new { Language = "en", AnimationsEnabled = true,
                            SoundEnabled = false, Detached = true, CanClaimDividend = false,
                            Economy = EmpireModernSnapshot.Create(service) }, _ => new(false, "Read-only fixture"),
                            _ => new { }, () => { });
                        host.Content = web;
                    }
                    else
                    {
                        legacy = new EmpireView(service, _ => false, () => false, () => { }, () => true);
                        host.Content = legacy;
                    }
                    host.Show(); host.UpdateLayout();
                    var tick = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromSeconds(1) };
                    tick.Tick += (_, _) => { service.Accrue(DateTimeOffset.UtcNow); web?.RefreshLive(); legacy?.RefreshLive(); };
                    tick.Start();
                    try
                    {
                        if (web is not null)
                        {
                            await EmpireWebSmoke.Wait(() => web.Ready, "Performance fixture did not load.");
                            await EmpireWebSmoke.Click(web, "floor-3");
                            await EmpireWebSmoke.Click(web, "room-hotel");
                            environment = web.Browser.CoreWebView2.Environment;
                            await EmpireWebSmoke.Script(web, "window.__empireFrames=[];let previous=0;window.__empireSampling=true;const frame=t=>{if(!window.__empireSampling)return;if(previous)window.__empireFrames.push(t-previous);previous=t;requestAnimationFrame(frame);};requestAnimationFrame(frame);true");
                        }
                        else
                        {
                            Find<Button>(host).Single(b => b.Name == "EmpireFloor3Button").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                            Find<Button>(host).Single(b => b.Name == "EmpireRoom_hotel").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                        }
                        await Task.Delay(500);
                        int[] Processes() => new[] { Environment.ProcessId }.Concat(environment?.GetProcessInfos().Select(p => p.ProcessId) ?? []).Distinct().ToArray();
                        var before = ReadProcesses(Processes());
                        var frames = new List<double>(); TimeSpan previous = TimeSpan.Zero;
                        void Render(object? sender, EventArgs e)
                        {
                            if (e is not RenderingEventArgs frame || frame.RenderingTime == previous) return;
                            double ms = (frame.RenderingTime - previous).TotalMilliseconds;
                            if (previous != TimeSpan.Zero && ms > 0 && ms < 250) frames.Add(ms);
                            previous = frame.RenderingTime;
                        }
                        CompositionTarget.Rendering += Render;
                        var watch = Stopwatch.StartNew();
                        try { await Task.Delay(4000); }
                        finally { CompositionTarget.Rendering -= Render; }
                        var after = ReadProcesses(Processes());
                        double cpuMs = after.Sum(p => Math.Max(0, p.Value.Cpu - before.GetValueOrDefault(p.Key).Cpu));
                        object? browserFrames = null;
                        if (web is not null)
                        {
                            string raw = await EmpireWebSmoke.Script(web, "window.__empireSampling=false;return window.__empireFrames;");
                            browserFrames = Summary(JsonSerializer.Deserialize<double[]>(raw) ?? []);
                            await EmpireWebSmoke.Capture(web, Path.Combine(output, "empire-benchmark-modern-" + (int)size.Item1 + ".png"));
                        }
                        results.Add(new { Presentation = modern ? "React/Pixi/WebView2" : "WPF retained rollback source",
                            Width = size.Item1, Height = size.Item2, PropertyCount = service.OwnedProperties.Count,
                            Scene = "Resort Floor, selected Luxury Hotel, all 15 rooms level 100, prestige 25",
                            AnimationEnabled = true, EconomyHz = 1, SampleSeconds = watch.Elapsed.TotalSeconds,
                            ProcessCount = after.Count, CpuPercentOfMachine = cpuMs / watch.Elapsed.TotalMilliseconds / Environment.ProcessorCount * 100,
                            PrivateBytesBefore = before.Sum(p => p.Value.Private), PrivateBytesAfter = after.Sum(p => p.Value.Private),
                            WorkingSetAfter = after.Sum(p => p.Value.Working), HostCompositionIntervals = Summary(frames), BrowserRafIntervals = browserFrames });
                    }
                    finally { tick.Stop(); web?.Dispose(); legacy?.Dispose(); host.Content = null; }
                }
            }
            File.WriteAllText(Path.Combine(output, "empire-presentation-performance.json"), JsonSerializer.Serialize(new
            {
                Environment = new { OS = Environment.OSVersion.ToString(), Runtime = Environment.Version.ToString(), Environment.ProcessorCount,
                    Dpi = VisualTreeHelper.GetDpi(host).PixelsPerDip },
                Notes = "Sequential read-only Windows fixtures. CPU/private memory include host plus all shared WebView2 environment processes, including hidden lobby. WPF composition and browser rAF are distinct callback clocks, not GPU frame times. Warm-up 500ms; 4s samples are CI diagnostics, not long-session certification.",
                Gpu = "Not measured: no GPU timing, utilization or VRAM counter is available in this smoke.", Results = results,
            }, new JsonSerializerOptions { WriteIndented = true }));
        }
        finally { host.Close(); if (visible) main.Show(); }
    }
    private static object? Summary(IEnumerable<double> values)
    {
        double[] samples = values.Where(v => v > 0 && v < 250).Order().ToArray();
        return samples.Length == 0 ? null : new { Count = samples.Length, MeanMs = samples.Average(),
            P95Ms = samples[(int)Math.Ceiling(samples.Length * .95) - 1] };
    }
    private static Dictionary<int, (double Cpu, long Private, long Working)> ReadProcesses(IEnumerable<int> ids)
    {
        var result = new Dictionary<int, (double, long, long)>();
        foreach (int id in ids)
        {
            try { using var process = Process.GetProcessById(id); result[id] = (process.TotalProcessorTime.TotalMilliseconds, process.PrivateMemorySize64, process.WorkingSet64); }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception) { }
        }
        return result;
    }
    private static IEnumerable<T> Find<T>(DependencyObject root) where T : DependencyObject
    {
        if (root is T item) yield return item;
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            foreach (var child in Find<T>(VisualTreeHelper.GetChild(root, i))) yield return child;
    }
}
