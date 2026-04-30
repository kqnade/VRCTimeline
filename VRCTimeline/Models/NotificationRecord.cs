namespace VRCTimeline.Models;

/// <summary>
/// VRChat の通知（フレンドリクエスト、招待など）を記録する DB エンティティ。
/// </summary>
public class NotificationRecord
{
    /// <summary>主キー</summary>
    public int Id { get; set; }

    /// <summary>通知を受信した日時</summary>
    public DateTime ReceivedAt { get; set; }

    /// <summary>通知の送信元プレイヤー名</summary>
    public string SenderName { get; set; } = string.Empty;

    /// <summary>通知種別（"friendRequest", "invite" など）</summary>
    public string NotificationType { get; set; } = string.Empty;

    /// <summary>通知受信時に滞在していたワールド訪問の外部キー（nullable）</summary>
    public int? WorldVisitId { get; set; }

    /// <summary>関連するワールド訪問（ナビゲーションプロパティ）</summary>
    public WorldVisit? WorldVisit { get; set; }
}
