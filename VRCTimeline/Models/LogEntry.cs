namespace VRCTimeline.Models;

/// <summary>ログ行の種別</summary>
public enum LogEntryType
{
    /// <summary>ワールド入室またはインスタンス接続</summary>
    RoomJoin,
    /// <summary>他プレイヤーの入室</summary>
    PlayerJoined,
    /// <summary>他プレイヤーの退室</summary>
    PlayerLeft,
    /// <summary>システム情報（VRChat終了通知など）</summary>
    Info,
    /// <summary>Invite / RequestInvite / Boop 等の通知受信</summary>
    Notification,
    /// <summary>ワールド内での動画再生検出</summary>
    VideoUrl
}

/// <summary>
/// VRChat ログから解析された1行分のイベントデータ。
/// LogWatcher がリアルタイムに生成し、ViewModel へ通知する。
/// </summary>
public class LogEntry
{
    /// <summary>ログ行のタイムスタンプ</summary>
    public DateTime Timestamp { get; set; }

    /// <summary>イベント種別</summary>
    public LogEntryType Type { get; set; }

    /// <summary>UI 表示用のメッセージ文字列</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>関連プレイヤーの表示名（usr_xxx 除去済み）</summary>
    public string? PlayerName { get; set; }

    /// <summary>関連プレイヤーの VRChat ユーザーID（例: usr_xxx）</summary>
    public string? PlayerUserId { get; set; }

    /// <summary>入室先ワールド名</summary>
    public string? WorldName { get; set; }

    /// <summary>インスタンスID（wrld_xxx:nonce 形式）</summary>
    public string? InstanceId { get; set; }

    /// <summary>通知種別（invite / requestInvite / boop）</summary>
    public string? NotificationType { get; set; }

    /// <summary>再生された動画の URL</summary>
    public string? VideoUrl { get; set; }
}
