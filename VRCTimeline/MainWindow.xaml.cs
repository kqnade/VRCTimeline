using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace VRCTimeline;

/// <summary>
/// メインウィンドウ。
/// DWM API を使用してタイトルバーをダークテーマに合わせてカスタマイズする。
/// </summary>
public partial class MainWindow : Window
{
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_BORDER_COLOR = 34;
    private const int DWMWA_CAPTION_COLOR = 35;
    private const int DWMWA_TEXT_COLOR = 36;

    public MainWindow()
    {
        InitializeComponent();
    }

    /// <summary>ウィンドウハンドル取得後にタイトルバーの色を適用する</summary>
    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        ApplyTitleBarColor();
    }

    /// <summary>DWM API でタイトルバーの背景色・テキスト色をテーマに合わせて設定する</summary>
    private void ApplyTitleBarColor()
    {
        if (PresentationSource.FromVisual(this) is not HwndSource source) return;

        var hwnd = source.Handle;

        int darkMode = 1;
        DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));

        if (TryFindResource("MaterialDesignPaper") is SolidColorBrush bg)
        {
            int bgRef = bg.Color.R | (bg.Color.G << 8) | (bg.Color.B << 16);
            DwmSetWindowAttribute(hwnd, DWMWA_CAPTION_COLOR, ref bgRef, sizeof(int));
            DwmSetWindowAttribute(hwnd, DWMWA_BORDER_COLOR, ref bgRef, sizeof(int));
        }

        if (TryFindResource("MaterialDesignBody") is SolidColorBrush fg)
        {
            int fgRef = fg.Color.R | (fg.Color.G << 8) | (fg.Color.B << 16);
            DwmSetWindowAttribute(hwnd, DWMWA_TEXT_COLOR, ref fgRef, sizeof(int));
        }
    }
}
