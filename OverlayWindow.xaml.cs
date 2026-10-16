using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Documents;
using System.Text.RegularExpressions;

namespace FiveMOverlay;

public partial class OverlayWindow : Window
{
    private readonly OverlayProfile _settings;
    private bool _isLocked;
    private bool _isShutdown;
    private const int GwlExstyle = -20;
    private const int WsExTransparent = 0x00000020;
    private const int WsExLayered = 0x00080000;

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongW", SetLastError = true)]
    private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongW", SetLastError = true)]
    private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

    public event Action<bool>? OverlayVisibilityChanged;

    public OverlayWindow(OverlayProfile settings)
    {
        InitializeComponent();
        _settings = settings;
        Left = settings.Left;
        Top = settings.Top;
        Width = Math.Clamp(settings.Width, MinWidth, 1600);
        Height = Math.Clamp(settings.Height, MinHeight, 900);
        OverlayText.FontSize = Math.Clamp(settings.FontSize, 12, 96);
        UpdateLineSpacing();
        SetMessage(settings.Message);
        OverlayText.Foreground = BrushFromHex(settings.TextColor, Colors.White);
        Panel.Background = BrushFromHex(settings.BackgroundColor, Color.FromArgb(204, 21, 25, 34));
        _isLocked = settings.IsLocked;
        MouseLeftButtonDown += (_, e) =>
        {
            if (!_isLocked && e.ButtonState == System.Windows.Input.MouseButtonState.Pressed &&
                !IsWithinResizeGrip(e.OriginalSource as DependencyObject)) DragMove();
        };
        SizeChanged += (_, _) => OverlayText.MaxWidth = Math.Max(60, ActualWidth - 52);
        SourceInitialized += (_, _) => ApplyMousePassThrough();
    }

    private bool IsWithinResizeGrip(DependencyObject? source)
    {
        while (source != null)
        {
            if (ReferenceEquals(source, ResizeGrip)) return true;
            try { source = VisualTreeHelper.GetParent(source); }
            catch { return false; }
        }
        return false;
    }

    private static Brush BrushFromHex(string hex, Color fallback)
    {
        try
        {
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            brush.Freeze();
            return brush;
        }
        catch { return new SolidColorBrush(fallback); }
    }

    public bool IsLocked => _isLocked;

    public void SetLocked(bool locked)
    {
        _isLocked = locked;
        _settings.IsLocked = locked;
        ResizeGrip.Visibility = locked ? Visibility.Collapsed : Visibility.Visible;
        ApplyMousePassThrough();
    }

    private void ApplyMousePassThrough()
    {
        var handle = new WindowInteropHelper(this).Handle;
        if (handle == IntPtr.Zero) return;
        long style = IntPtr.Size == 8
            ? GetWindowLongPtr64(handle, GwlExstyle).ToInt64()
            : GetWindowLong32(handle, GwlExstyle);
        if (_isLocked) style |= WsExTransparent | WsExLayered;
        else style &= ~((long)WsExTransparent);
        if (IntPtr.Size == 8) SetWindowLongPtr64(handle, GwlExstyle, new IntPtr(style));
        else SetWindowLong32(handle, GwlExstyle, unchecked((int)style));
    }

    private void ResizeGrip_DragDelta(object sender, DragDeltaEventArgs e)
    {
        if (_isLocked) return;
        Width = Math.Clamp(Width + e.HorizontalChange, MinWidth, 1600);
        Height = Math.Clamp(Height + e.VerticalChange, MinHeight, 900);
        _settings.Width = Width;
        _settings.Height = Height;
    }

    public void SetMessage(string message)
    {
        _settings.Message = message;
        OverlayText.Inlines.Clear();
        TableContent.Children.Clear();
        TableContent.RowDefinitions.Clear();
        TableContent.ColumnDefinitions.Clear();

        var lines = (message ?? string.Empty)
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n');

        bool isPipeList = lines.Any(line => line.Contains('|'));
        if (isPipeList)
        {
            OverlayText.Visibility = Visibility.Collapsed;
            TableContent.Visibility = Visibility.Visible;
            OverlayText.FontFamily = new FontFamily("Segoe UI");
            OverlayText.TextWrapping = TextWrapping.NoWrap;
            BuildPipeTable(lines);
        }
        else
        {
            TableContent.Visibility = Visibility.Collapsed;
            OverlayText.Visibility = Visibility.Visible;
            OverlayText.FontFamily = new FontFamily("Segoe UI");
            OverlayText.TextWrapping = TextWrapping.Wrap;

            var colorMap = CreateColorMap();
            var pattern = ColorTagPattern();
            for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                AppendColoredLine(lines[lineIndex], pattern, colorMap);
                if (lineIndex < lines.Length - 1)
                    OverlayText.Inlines.Add(new LineBreak());
            }
            OverlayText.VerticalAlignment = lines.Length > 1 ? VerticalAlignment.Top : VerticalAlignment.Center;
        }

        UpdateLineSpacing();
        SaveGeometry();
    }

    private void BuildPipeTable(string[] sourceLines)
    {
        var rows = sourceLines
            .Where(line => !string.IsNullOrWhiteSpace(line) && line.Contains('|'))
            .Select(line =>
            {
                var fields = line.Split('|').Select(part => part.Trim()).ToArray();
                string stars = fields.Length > 0 ? fields[0] : string.Empty;
                string description = fields.Length > 1 ? fields[1] : string.Empty;
                // Tolerate an empty extra field, for example "Drohung| | droh".
                string shortcut = fields.Length > 2
                    ? string.Join(" ", fields.Skip(2).Where(part => !string.IsNullOrWhiteSpace(part)))
                    : string.Empty;
                return (Stars: stars, Description: description, Shortcut: shortcut);
            })
            .ToArray();

        if (rows.Length == 0)
        {
            TableContent.Visibility = Visibility.Collapsed;
            OverlayText.Visibility = Visibility.Visible;
            return;
        }

        TableContent.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        TableContent.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        TableContent.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var colorMap = CreateColorMap();
        var pattern = ColorTagPattern();
        double fontSize = OverlayText.FontSize;
        double lineHeight = Math.Max(16, fontSize * 1.3);

        for (int i = 0; i < rows.Length; i++)
        {
            TableContent.RowDefinitions.Add(new RowDefinition { Height = new GridLength(lineHeight) });
            var row = rows[i];

            var stars = CreateCell(row.Stars, pattern, colorMap, fontSize, lineHeight);
            stars.Margin = new Thickness(0, 0, 14, 0);
            Grid.SetRow(stars, i); Grid.SetColumn(stars, 0);
            TableContent.Children.Add(stars);

            var description = CreateCell(row.Description, pattern, colorMap, fontSize, lineHeight);
            description.Margin = new Thickness(0, 0, 20, 0);
            Grid.SetRow(description, i); Grid.SetColumn(description, 1);
            TableContent.Children.Add(description);

            var shortcut = CreateCell(row.Shortcut, pattern, colorMap, fontSize, lineHeight);
            Grid.SetRow(shortcut, i); Grid.SetColumn(shortcut, 2);
            TableContent.Children.Add(shortcut);
        }

        // Do not auto-expand the window to fit the table: the user's chosen overlay
        // dimensions must remain exact and must survive saving/restarting. If the
        // content is wider/taller than the overlay, the panel clips it to its bounds.
        TableContent.HorizontalAlignment = HorizontalAlignment.Left;
        TableContent.VerticalAlignment = VerticalAlignment.Top;
    }

    private TextBlock CreateCell(string text, string pattern, Dictionary<string, string> colorMap, double fontSize, double lineHeight)
    {
        var cell = new TextBlock
        {
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = fontSize,
            FontWeight = FontWeights.SemiBold,
            Foreground = BrushFromHex(_settings.TextColor, Colors.White),
            TextWrapping = TextWrapping.NoWrap,
            LineStackingStrategy = LineStackingStrategy.BlockLineHeight,
            LineHeight = lineHeight,
            VerticalAlignment = VerticalAlignment.Center
        };
        AppendColoredText(cell, text, pattern, colorMap);
        return cell;
    }

    private static Dictionary<string, string> CreateColorMap() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["weiß"] = "#FFFFFFFF", ["weiss"] = "#FFFFFFFF",
        ["rot"] = "#FFFF5555", ["red"] = "#FFFF5555",
        ["grün"] = "#FF55FF77", ["gruen"] = "#FF55FF77", ["green"] = "#FF55FF77",
        ["blau"] = "#FF5599FF", ["blue"] = "#FF5599FF",
        ["gelb"] = "#FFFFFF55", ["yellow"] = "#FFFFFF55",
        ["türkis"] = "#FF55FFFF", ["turkis"] = "#FF55FFFF", ["cyan"] = "#FF55FFFF",
        ["orange"] = "#FFFFA040", ["pink"] = "#FFFF66CC", ["lila"] = "#FFBB88FF", ["purple"] = "#FFBB88FF"
    };

    private static string ColorTagPattern() => @"\[(weiß|weiss|rot|red|grün|gruen|green|blau|blue|gelb|yellow|türkis|turkis|cyan|orange|pink|lila|purple)\](.*?)\[/\1\]";

    private void AppendColoredLine(string line, string pattern, Dictionary<string, string> colorMap)
    {
        AppendColoredText(OverlayText, line, pattern, colorMap);
        if (line.Length == 0)
            OverlayText.Inlines.Add(new Run(string.Empty));
    }

    private void AppendColoredText(TextBlock target, string text, string pattern, Dictionary<string, string> colorMap)
    {
        int position = 0;
        foreach (Match match in Regex.Matches(text, pattern, RegexOptions.IgnoreCase))
        {
            if (match.Index > position)
                target.Inlines.Add(new Run(text.Substring(position, match.Index - position)));

            var colorName = match.Groups[1].Value;
            target.Inlines.Add(new Run(match.Groups[2].Value)
            {
                Foreground = BrushFromHex(colorMap[colorName], Colors.White)
            });
            position = match.Index + match.Length;
        }

        if (position < text.Length)
            target.Inlines.Add(new Run(text.Substring(position)));
    }

    private void UpdateLineSpacing()
    {
        // Keep a fixed, readable baseline distance across every line, including wrapped lines.
        OverlayText.LineStackingStrategy = LineStackingStrategy.BlockLineHeight;
        OverlayText.LineHeight = Math.Max(16, OverlayText.FontSize * 1.3);
    }

    public void SetFontSize(double fontSize)
    {
        var size = Math.Clamp(fontSize, 12, 72);
        OverlayText.FontSize = size;
        UpdateLineSpacing();
        _settings.FontSize = size;
        if (TableContent.Visibility == Visibility.Visible)
            SetMessage(_settings.Message);
        else
            UpdateLineSpacing();
    }

    public void SetTextColor(string hex)
    {
        OverlayText.Foreground = BrushFromHex(hex, Colors.White);
        foreach (var cell in TableContent.Children.OfType<TextBlock>())
            cell.Foreground = BrushFromHex(hex, Colors.White);
        _settings.TextColor = hex;
    }

    private void SaveGeometry()
    {
        _settings.Left = Left; _settings.Top = Top;
        _settings.Width = Width; _settings.Height = Height;
        _settings.IsLocked = _isLocked;
    }

    public void ShowOverlay()
    {
        if (!IsVisible) Show();
        Topmost = true;
        OverlayVisibilityChanged?.Invoke(true);
    }

    public void HideOverlay()
    {
        if (IsVisible) Hide();
        SaveGeometry();
        OverlayVisibilityChanged?.Invoke(false);
    }

    public void Shutdown()
    {
        if (_isShutdown) return;
        _isShutdown = true;
        // Hidden WPF windows also keep the dispatcher/process alive, so always close them.
        Close();
    }
}
