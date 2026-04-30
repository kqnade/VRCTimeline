using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using VRCTimeline.ViewModels;

namespace VRCTimeline.Views;

/// <summary>アクティビティ履歴画面のコードビハインド</summary>
public partial class ActivityHistoryView : UserControl
{
    public ActivityHistoryView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    /// <summary>画面表示時に ViewModel の初期データ読み込みを実行する</summary>
    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ActivityHistoryViewModel vm)
            await vm.InitializeAsync();
    }

    /// <summary>プレイヤー一覧パネルのリサイズ操作を処理する</summary>
    private void PlayerResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
        var newHeight = PlayerScrollViewer.Height + e.VerticalChange;
        PlayerScrollViewer.Height = Math.Clamp(newHeight, 40, 500);
    }
}
