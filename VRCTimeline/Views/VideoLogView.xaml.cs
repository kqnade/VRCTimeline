using System.Windows;
using System.Windows.Controls;
using VRCTimeline.ViewModels;

namespace VRCTimeline.Views;

/// <summary>動画ログ画面のコードビハインド</summary>
public partial class VideoLogView : UserControl
{
    public VideoLogView()
    {
        InitializeComponent();
    }

    /// <summary>画面表示時に ViewModel の初期データ読み込みを実行する</summary>
    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is VideoLogViewModel vm)
            await vm.InitializeAsync();
    }
}
