namespace VRCTimeline.Services;

/// <summary>
/// 画面間ナビゲーションのイベント仲介サービス。
/// ViewModel 間の直接参照を避け、疎結合な画面遷移を実現する。
/// </summary>
public class NavigationService
{
    /// <summary>指定ワールド訪問IDの写真画面表示をリクエストするイベント</summary>
    public event Action<int?>? ShowPhotosRequested;

    /// <summary>指定ワールド訪問IDのアクティビティ画面表示をリクエストするイベント</summary>
    public event Action<int?>? ShowActivityRequested;

    /// <summary>データインポート完了を通知するイベント</summary>
    public event Action? DataImported;

    /// <summary>写真画面へ遷移し、指定ワールド訪問の写真を表示する</summary>
    public void ShowPhotosForVisit(int? worldVisitId) => ShowPhotosRequested?.Invoke(worldVisitId);

    /// <summary>アクティビティ画面へ遷移し、指定ワールド訪問を表示する</summary>
    public void ShowActivityForVisit(int? worldVisitId) => ShowActivityRequested?.Invoke(worldVisitId);

    /// <summary>データインポート完了を各画面に通知する</summary>
    public void NotifyDataImported() => DataImported?.Invoke();
}
