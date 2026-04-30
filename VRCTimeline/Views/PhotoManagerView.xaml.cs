using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using VRCTimeline.ViewModels;

namespace VRCTimeline.Views;

/// <summary>写真管理画面のコードビハインド</summary>
public partial class PhotoManagerView : UserControl
{
    public PhotoManagerView()
    {
        InitializeComponent();
    }

    /// <summary>画面表示時に ViewModel の初期データ読み込みを実行する</summary>
    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is PhotoManagerViewModel vm)
            await vm.InitializeAsync();
    }

    /// <summary>プレイヤー一覧パネルのリサイズ操作を処理する</summary>
    private void PlayerResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
        var newHeight = PlayerScrollViewer.Height + e.VerticalChange;
        PlayerScrollViewer.Height = Math.Clamp(newHeight, 75, 500);
    }

    /// <summary>写真スクロール領域のマウスホイールを高速化する（1.25倍）</summary>
    private void PhotoScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is ScrollViewer sv)
        {
            sv.ScrollToVerticalOffset(sv.VerticalOffset - e.Delta * 1.25);
            e.Handled = true;
        }
    }
}
