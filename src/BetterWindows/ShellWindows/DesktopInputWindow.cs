using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using BetterWindows.Monitors;

namespace BetterWindows.ShellWindows;

/// <summary>
/// Plovoucí vstupní pole přesně přes textové pole widgetu na ploše.
///
/// Proč: vrstva plochy je vložená do okna plochy Windows (jiný proces). Myš k ní projde, ale
/// klávesnice ne – fokus má vždy plocha Windows (stejně je na tom Lively / Wallpaper Engine).
/// Vynucovat fokus mezi procesy by byla nespolehlivá obezlička, proto se pro psaní ukáže tohle
/// obyčejné okno, které klávesnici dostane vždy. Enter = odeslat, Esc = zrušit, klik jinam = nechat rozepsané.
/// Je to čisté WPF (bez WebView), takže smí být průhledné (AllowsTransparency) a ukáže se okamžitě.
/// </summary>
internal sealed class DesktopInputWindow : Window
{
    private readonly TextBox _box;
    private readonly TextBlock _placeholder;
    private bool _closing;

    /// <summary>(text, odeslat?) – volá se jednou při zavření.</summary>
    public event Action<string, bool>? Finished;

    public DesktopInputWindow(PixelRect rect, double scale, string value, string? placeholder, double fontSizeCss, bool dark)
    {
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        Topmost = true;
        WindowStartupLocation = WindowStartupLocation.Manual;

        // Fyzické px -> WPF jednotky (DIP).
        var height = Math.Max(rect.Height, (int)Math.Round(26 * scale));
        Left = rect.Left / scale;
        Top = (rect.Top - (height - rect.Height) / 2.0) / scale;
        Width = Math.Max(rect.Width, (int)Math.Round(120 * scale)) / scale;
        Height = height / scale;

        var fg = dark ? Color.FromRgb(0xED, 0xED, 0xED) : Color.FromRgb(0x17, 0x17, 0x17);
        var bg = dark ? Color.FromArgb(0xF2, 0x26, 0x26, 0x2A) : Color.FromArgb(0xF2, 0xF7, 0xF7, 0xF8);
        var border = dark ? Color.FromArgb(0x55, 0xFF, 0xFF, 0xFF) : Color.FromArgb(0x33, 0x00, 0x00, 0x00);
        var font = new FontFamily("Inter, Segoe UI Variable Text, Segoe UI");
        var size = Math.Max(11, fontSizeCss);

        _box = new TextBox
        {
            Text = value,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Foreground = new SolidColorBrush(fg),
            CaretBrush = new SolidColorBrush(fg),
            FontFamily = font,
            FontSize = size,
            VerticalContentAlignment = VerticalAlignment.Center,
            Padding = new Thickness(6, 0, 6, 0),
        };
        _placeholder = new TextBlock
        {
            Text = placeholder ?? "",
            Foreground = new SolidColorBrush(Color.FromArgb(0x80, fg.R, fg.G, fg.B)),
            FontFamily = font,
            FontSize = size,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 0, 8, 0),
            IsHitTestVisible = false,
        };
        _box.TextChanged += (_, _) => _placeholder.Visibility = _box.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        _placeholder.Visibility = value.Length == 0 ? Visibility.Visible : Visibility.Collapsed;

        Content = new Border
        {
            CornerRadius = new CornerRadius(6),
            Background = new SolidColorBrush(bg),
            BorderBrush = new SolidColorBrush(border),
            BorderThickness = new Thickness(1),
            Child = new Grid { Children = { _placeholder, _box } },
        };

        _box.PreviewKeyDown += OnKey;
        Deactivated += (_, _) => Finish(submit: false);
        Loaded += (_, _) =>
        {
            Activate();
            _box.Focus();
            Keyboard.Focus(_box);
            _box.CaretIndex = _box.Text.Length;
        };
    }

    private void OnKey(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            Finish(submit: true);
        }
        else if (e.Key == Key.Escape)
        {
            e.Handled = true;
            _box.Text = "";
            Finish(submit: false);
        }
    }

    private void Finish(bool submit)
    {
        if (_closing)
            return;
        _closing = true;
        Finished?.Invoke(_box.Text, submit);
        Close();
    }
}
