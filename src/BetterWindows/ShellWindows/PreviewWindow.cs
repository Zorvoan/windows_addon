using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using BetterWindows.Core;
using BetterWindows.Logic.Dock;
using BetterWindows.Monitors;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;
using Windows.Win32.UI.WindowsAndMessaging;

namespace BetterWindows.ShellWindows;

/// <summary>
/// Náhledy oken nad ikonou v docku. Živý obraz okna kreslí přímo DWM (DwmRegisterThumbnail)
/// do vyhrazeného obdélníku v tomto okně – proto je to nativní WPF okno, ne WebView
/// (DWM náhled se vždy kreslí NAD obsah okna, WebView by ho nepřekryl ani nezarovnal).
/// </summary>
internal sealed class PreviewWindow : ShellWindowBase
{
    private const double MaxThumbW = 220, MaxThumbH = 128, Pad = 8, TitleH = 24;

    private readonly List<(nint Thumb, Border Slot)> _thumbs = [];
    private readonly StackPanel _row = new() { Orientation = Orientation.Horizontal };
    private readonly Border _frame;
    private PixelRect _rect = new(0, 0, 1, 1);

    public event Action<long>? ActivateRequested;
    public event Action<long>? CloseRequested;
    public event Action<bool>? HoverChanged;

    public PreviewWindow(ShellContext context, MonitorInfo monitor)
        : base(null, context, monitor)
    {
        _frame = new Border
        {
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(4),
            Child = _row,
        };
        Content = _frame;
        UseLayoutRounding = true;
        MouseEnter += (_, _) => HoverChanged?.Invoke(true);
        MouseLeave += (_, _) => HoverChanged?.Invoke(false);
        Closed += (_, _) => ClearThumbnails();
    }

    protected override DWM_WINDOW_CORNER_PREFERENCE CornerPreference => DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUND;
    protected override PixelRect ComputeBounds(MonitorInfo monitor) => _rect;

    protected override void Place()
    {
        PInvoke.SetWindowPos(Hwnd, HWND.HWND_TOPMOST, _rect.Left, _rect.Top, _rect.Width, _rect.Height,
            SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE);
    }

    /// <summary>Ukáže náhledy oken nad ikonou (anchor = ikona na obrazovce, fyzické px).</summary>
    public void ShowFor(IReadOnlyList<AppWindowInfo> windows, PixelRect anchor)
    {
        new System.Windows.Interop.WindowInteropHelper(this).EnsureHandle();
        ClearThumbnails();
        _row.Children.Clear();
        _frame.Background = new SolidColorBrush(IsDark ? Color.FromArgb(0x55, 0x14, 0x14, 0x15) : Color.FromArgb(0x55, 0xFA, 0xFA, 0xFA));
        var fg = new SolidColorBrush(IsDark ? Color.FromRgb(0xED, 0xED, 0xED) : Color.FromRgb(0x17, 0x17, 0x17));

        // Kolik místa má jeden náhled, aby se celý panel vešel na monitor.
        var shown = windows.Take(6).ToList();
        var monitorDip = Monitor.Bounds.Width / Monitor.Scale;
        var maxThumbW = Math.Max(80, Math.Min(MaxThumbW, (monitorDip - 40) / Math.Max(1, shown.Count) - Pad - 8));

        foreach (var w in shown)
        {
            var src = new HWND((nint)w.Hwnd);
            nint thumb = 0;
            var size = new SIZE(16, 9);
            unsafe
            {
                if (PInvoke.DwmRegisterThumbnail(Hwnd, src, out var t).Succeeded)
                {
                    thumb = (nint)t;
                    PInvoke.DwmQueryThumbnailSourceSize(t, out size);
                }
            }

            // Velikost náhledu podle poměru stran okna.
            double aspect = size.cx > 0 && size.cy > 0 ? (double)size.cx / size.cy : 16.0 / 9;
            double tw = maxThumbW, th = tw / aspect;
            if (th > MaxThumbH) { th = MaxThumbH; tw = th * aspect; }

            var slot = new Border
            {
                Width = tw,
                Height = th,
                CornerRadius = new CornerRadius(6),
                Background = new SolidColorBrush(IsDark ? Color.FromArgb(0x14, 0xED, 0xED, 0xED) : Color.FromArgb(0x14, 0x17, 0x17, 0x17)),
                Child = new TextBlock
                {
                    Text = w.IsMinimized ? "Minimalizováno" : "",
                    Foreground = fg,
                    Opacity = 0.6,
                    FontSize = 11,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                },
            };

            var title = new TextBlock
            {
                Text = string.IsNullOrWhiteSpace(w.Title) ? w.AppName : w.Title,
                Foreground = fg,
                FontSize = 12,
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 0, 8, 0),
            };
            // Ikona zavření kreslená čárou (stejná sada jako v UI), ne textový znak.
            var cross = new System.Windows.Shapes.Path
            {
                Data = Geometry.Parse("M0,0 L8,8 M8,0 L0,8"),
                Stroke = fg,
                StrokeThickness = 1.5,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                Width = 8,
                Height = 8,
            };
            var close = new Button
            {
                Content = cross,
                Width = 24,
                Height = 24,
                Padding = new Thickness(0),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Arrow,
                ToolTip = "Zavřít okno",
            };
            var hwnd = w.Hwnd;
            close.Click += (_, _) => CloseRequested?.Invoke(hwnd);

            var header = new DockPanel { Height = TitleH, Width = tw, LastChildFill = true };
            DockPanel.SetDock(close, Dock.Right);
            header.Children.Add(close);
            header.Children.Add(title);

            var item = new StackPanel { Margin = new Thickness(Pad / 2) };
            item.Children.Add(header);
            item.Children.Add(slot);

            var hover = new Border { CornerRadius = new CornerRadius(8), Padding = new Thickness(4), Child = item, Background = Brushes.Transparent };
            hover.MouseEnter += (_, _) => hover.Background = new SolidColorBrush(IsDark ? Color.FromArgb(0x12, 0xED, 0xED, 0xED) : Color.FromArgb(0x0D, 0x17, 0x17, 0x17));
            hover.MouseLeave += (_, _) => hover.Background = Brushes.Transparent;
            hover.MouseLeftButtonUp += (_, e) =>
            {
                if (e.OriginalSource is DependencyObject d && FindParent<Button>(d) is null)
                    ActivateRequested?.Invoke(hwnd);
            };
            _row.Children.Add(hover);
            _thumbs.Add((thumb, slot));
        }

        // Velikost okna podle obsahu (DIP -> fyzické px), nad ikonou a uvnitř monitoru.
        _frame.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var s = Monitor.Scale;
        int width = (int)Math.Ceiling(_frame.DesiredSize.Width * s), height = (int)Math.Ceiling(_frame.DesiredSize.Height * s);
        // Bez Math.Clamp: kdyby byl panel širší než monitor, min > max by vyhodilo výjimku.
        int left = Math.Max(Monitor.Bounds.Left + 8, Math.Min(anchor.Left + anchor.Width / 2 - width / 2, Monitor.Bounds.Right - width - 8));
        int top = anchor.Top - height - (int)(10 * s);
        _rect = new PixelRect(left, top, left + width, top + height);
        Width = width / s;
        Height = height / s;
        Place();
        if (!IsVisible)
        {
            Show();
            Place();
        }

        // Po rozvržení umístit DWM náhledy přesně do vyhrazených obdélníků.
        Dispatcher.InvokeAsync(UpdateThumbnailRects, System.Windows.Threading.DispatcherPriority.Loaded);
    }

    private void UpdateThumbnailRects()
    {
        var s = Monitor.Scale;
        foreach (var (thumb, slot) in _thumbs)
        {
            if (thumb == 0 || !slot.IsVisible)
                continue;
            var p = slot.TranslatePoint(new Point(0, 0), this);
            var props = new DWM_THUMBNAIL_PROPERTIES
            {
                dwFlags = PInvoke.DWM_TNP_RECTDESTINATION | PInvoke.DWM_TNP_VISIBLE | PInvoke.DWM_TNP_OPACITY | PInvoke.DWM_TNP_SOURCECLIENTAREAONLY,
                rcDestination = new RECT
                {
                    left = (int)Math.Round(p.X * s),
                    top = (int)Math.Round(p.Y * s),
                    right = (int)Math.Round((p.X + slot.ActualWidth) * s),
                    bottom = (int)Math.Round((p.Y + slot.ActualHeight) * s),
                },
                fVisible = true,
                opacity = 255,
                fSourceClientAreaOnly = false,
            };
            PInvoke.DwmUpdateThumbnailProperties(thumb, props);
        }
    }

    public void HidePreview()
    {
        ClearThumbnails();
        if (IsVisible)
            Hide();
    }

    private void ClearThumbnails()
    {
        foreach (var (thumb, _) in _thumbs)
            if (thumb != 0)
                PInvoke.DwmUnregisterThumbnail(thumb);
        _thumbs.Clear();
    }

    private static T? FindParent<T>(DependencyObject d) where T : DependencyObject
    {
        for (var cur = d; cur is not null; cur = VisualTreeHelper.GetParent(cur))
            if (cur is T t) return t;
        return null;
    }
}
