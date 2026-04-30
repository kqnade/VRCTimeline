using CommunityToolkit.Mvvm.ComponentModel;

namespace VRCTimeline.Services;

/// <summary>
/// グローバルローディング UI の表示を制御するサービス。
/// 参照カウント方式で複数の非同期処理が同時にローディングを要求できる。
/// </summary>
public partial class LoadingService : ObservableObject
{
    /// <summary>ローディング表示の参照カウント</summary>
    private int _count;

    /// <summary>ローディング中かどうか</summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>ローディング中に表示するメッセージ</summary>
    [ObservableProperty]
    private string _loadingMessage = "";

    /// <summary>ローディング表示を開始する（参照カウントをインクリメント）</summary>
    public void Show(string message = "読み込み中...")
    {
        Interlocked.Increment(ref _count);
        LoadingMessage = message;
        IsLoading = true;
    }

    /// <summary>ローディングメッセージのみを更新する</summary>
    public void UpdateMessage(string message)
    {
        LoadingMessage = message;
    }

    /// <summary>ローディング表示を終了する（参照カウントが0になったら非表示）</summary>
    public void Hide()
    {
        if (Interlocked.Decrement(ref _count) <= 0)
        {
            Interlocked.Exchange(ref _count, 0);
            IsLoading = false;
        }
    }
}
