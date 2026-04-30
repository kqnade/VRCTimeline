using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace VRCTimeline.Controls;

/// <summary>
/// HSV カラーホイールによるカラーピッカーコントロール。
/// 円形のカラーホイールと明度スライダーで色を選択し、Hex 値を双方向バインドする。
/// </summary>
public partial class ColorCirclePicker : UserControl
{
    /// <summary>デフォルトカラー</summary>
    private const string DefaultColor = "#262626";

    /// <summary>選択中のカラー Hex 値（双方向バインド対応）</summary>
    public static readonly DependencyProperty SelectedHexProperty = DependencyProperty.Register(
        nameof(SelectedHex), typeof(string), typeof(ColorCirclePicker),
        new FrameworkPropertyMetadata(DefaultColor, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            OnSelectedHexChanged));

    public string SelectedHex
    {
        get => GetValue(SelectedHexProperty) as string ?? DefaultColor;
        set => SetValue(SelectedHexProperty, value);
    }

    /// <summary>カラーホイールの描画用ビットマップ</summary>
    private WriteableBitmap? _wheelBitmap;

    /// <summary>ホイール上でドラッグ中かどうか</summary>
    private bool _isDragging;

    /// <summary>プログラムによる更新中フラグ（循環更新防止）</summary>
    private bool _isUpdating;

    /// <summary>現在選択中の色相 (0-360)</summary>
    private double _hue;

    /// <summary>現在選択中の彩度 (0-1)</summary>
    private double _saturation;

    /// <summary>現在選択中の明度 (0-1)</summary>
    private double _brightness = 1.0;

    /// <summary>カラーホイールの描画サイズ（ピクセル）</summary>
    private const int WheelSize = 200;

    /// <summary>カラーホイールの半径（ピクセル）</summary>
    private const int Radius = 95;

    public ColorCirclePicker()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            GenerateWheel();
            UpdateFromHex(SelectedHex);
        };
    }

    /// <summary>HSV カラーホイールのビットマップを生成して表示する</summary>
    private void GenerateWheel()
    {
        _wheelBitmap = new WriteableBitmap(WheelSize, WheelSize, 96, 96, PixelFormats.Bgra32, null);
        var pixels = new byte[WheelSize * WheelSize * 4];
        int cx = WheelSize / 2, cy = WheelSize / 2;

        for (int y = 0; y < WheelSize; y++)
        {
            for (int x = 0; x < WheelSize; x++)
            {
                double dx = x - cx, dy = y - cy;
                double dist = Math.Sqrt(dx * dx + dy * dy);
                int idx = (y * WheelSize + x) * 4;

                if (dist <= Radius)
                {
                    double hue = (Math.Atan2(dy, dx) * 180.0 / Math.PI + 360) % 360;
                    double sat = dist / Radius;
                    var c = HsvToRgb(hue, sat, _brightness);
                    pixels[idx] = c.B;
                    pixels[idx + 1] = c.G;
                    pixels[idx + 2] = c.R;
                    pixels[idx + 3] = 255;
                }
                else if (dist <= Radius + 1.5)
                {
                    double alpha = Math.Max(0, 1.0 - (dist - Radius) / 1.5);
                    double hue = (Math.Atan2(dy, dx) * 180.0 / Math.PI + 360) % 360;
                    var c = HsvToRgb(hue, 1.0, _brightness);
                    pixels[idx] = c.B;
                    pixels[idx + 1] = c.G;
                    pixels[idx + 2] = c.R;
                    pixels[idx + 3] = (byte)(255 * alpha);
                }
            }
        }

        _wheelBitmap.WritePixels(new Int32Rect(0, 0, WheelSize, WheelSize), pixels, WheelSize * 4, 0);
        WheelImage.Source = _wheelBitmap;
    }

    /// <summary>カラーホイール上のマウスダウンでドラッグ開始</summary>
    private void OnWheelMouseDown(object sender, MouseButtonEventArgs e)
    {
        _isDragging = true;
        Mouse.Capture(WheelImage);
        PickColor(e.GetPosition(WheelImage));
    }

    /// <summary>ドラッグ中のマウス移動で色を更新</summary>
    private void OnWheelMouseMove(object sender, MouseEventArgs e)
    {
        if (_isDragging)
            PickColor(e.GetPosition(WheelImage));
    }

    /// <summary>マウスアップでドラッグ終了</summary>
    private void OnWheelMouseUp(object sender, MouseButtonEventArgs e)
    {
        _isDragging = false;
        Mouse.Capture(null);
    }

    /// <summary>ホイール上の座標から色相と彩度を算出する</summary>
    private void PickColor(Point pos)
    {
        int cx = WheelSize / 2, cy = WheelSize / 2;
        double dx = pos.X - cx, dy = pos.Y - cy;
        double dist = Math.Sqrt(dx * dx + dy * dy);
        if (dist > Radius) dist = Radius;

        _hue = (Math.Atan2(dy, dx) * 180.0 / Math.PI + 360) % 360;
        _saturation = dist / Radius;
        UpdateColor();
    }

    /// <summary>明度スライダーの値変更時にホイールを再描画して色を更新する</summary>
    private void OnBrightnessChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isUpdating) return;
        _brightness = e.NewValue;
        GenerateWheel();
        UpdateColor();
    }

    /// <summary>現在の HSV 値から Hex 値を計算し、UI を更新する</summary>
    private void UpdateColor()
    {
        if (_isUpdating || !IsLoaded) return;
        _isUpdating = true;

        var color = HsvToRgb(_hue, _saturation, _brightness);
        var hex = $"#{color.R:X2}{color.G:X2}{color.B:X2}";

        SelectedHex = hex;
        PreviewRect.Fill = new SolidColorBrush(color);
        UpdateSelectorPosition();

        if (HexInput.Text != hex)
            HexInput.Text = hex;

        _isUpdating = false;
    }

    /// <summary>セレクターの位置を色相・彩度に対応する座標に更新する</summary>
    private void UpdateSelectorPosition()
    {
        double cx = WheelSize / 2.0;
        double angle = _hue * Math.PI / 180.0;
        double r = _saturation * Radius;
        Canvas.SetLeft(Selector, cx + r * Math.Cos(angle) - 7);
        Canvas.SetTop(Selector, cx + r * Math.Sin(angle) - 7);
    }

    /// <summary>Hex 入力テキスト変更時に色を更新する</summary>
    private void OnHexInputChanged(object sender, TextChangedEventArgs e)
    {
        if (_isUpdating) return;
        UpdateFromHex(HexInput.Text, fromInput: true);
    }

    /// <summary>Hex 文字列を # 付きの形式に正規化する</summary>
    private static string NormalizeHex(string hex)
    {
        var trimmed = hex.Trim().TrimStart('#');
        return $"#{trimmed}";
    }

    /// <summary>Hex 文字列が有効な 6 桁カラーコードかどうかを判定する</summary>
    private static bool IsValidHex(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return false;
        var trimmed = hex.Trim().TrimStart('#');
        return trimmed.Length == 6 && trimmed.All(c => Uri.IsHexDigit(c));
    }

    /// <summary>Hex 文字列から内部の HSV 値と UI を更新する</summary>
    private void UpdateFromHex(string? hex, bool fromInput = false)
    {
        if (string.IsNullOrWhiteSpace(hex)) return;

        var normalized = NormalizeHex(hex);
        if (!IsValidHex(normalized))
        {
            if (fromInput) return;
            normalized = DefaultColor;
        }

        try
        {
            var color = (Color)ColorConverter.ConvertFromString(normalized);
            var (h, s, v) = RgbToHsv(color);
            _hue = h;
            _saturation = s;
            _brightness = v;

            if (!IsLoaded) return;

            _isUpdating = true;
            BrightnessSlider.Value = v;
            PreviewRect.Fill = new SolidColorBrush(color);
            UpdateSelectorPosition();
            if (HexInput.Text != normalized)
                HexInput.Text = normalized;
            SelectedHex = normalized;
            _isUpdating = false;
        }
        catch
        {
            if (!fromInput)
                UpdateFromHex(DefaultColor);
        }
    }

    /// <summary>SelectedHex の依存関係プロパティ変更コールバック</summary>
    private static void OnSelectedHexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var picker = (ColorCirclePicker)d;
        if (!picker._isUpdating && e.NewValue is string hex)
            picker.UpdateFromHex(hex);
    }

    /// <summary>HSV 色空間から RGB Color に変換する</summary>
    private static Color HsvToRgb(double h, double s, double v)
    {
        int hi = (int)(h / 60.0) % 6;
        double f = h / 60.0 - Math.Floor(h / 60.0);
        double p = v * (1 - s);
        double q = v * (1 - f * s);
        double t = v * (1 - (1 - f) * s);

        var (r, g, b) = hi switch
        {
            0 => (v, t, p),
            1 => (q, v, p),
            2 => (p, v, t),
            3 => (p, q, v),
            4 => (t, p, v),
            _ => (v, p, q)
        };

        return Color.FromRgb((byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
    }

    /// <summary>RGB Color から HSV 色空間に変換する</summary>
    private static (double H, double S, double V) RgbToHsv(Color c)
    {
        double r = c.R / 255.0, g = c.G / 255.0, b = c.B / 255.0;
        double max = Math.Max(r, Math.Max(g, b));
        double min = Math.Min(r, Math.Min(g, b));
        double delta = max - min;

        double h = 0;
        if (delta > 0)
        {
            if (max == r) h = 60 * (((g - b) / delta) % 6);
            else if (max == g) h = 60 * ((b - r) / delta + 2);
            else h = 60 * ((r - g) / delta + 4);
        }
        if (h < 0) h += 360;

        double s = max > 0 ? delta / max : 0;
        return (h, s, max);
    }
}
