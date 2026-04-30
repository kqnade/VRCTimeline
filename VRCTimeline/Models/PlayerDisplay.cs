using VRCTimeline.Helpers;

namespace VRCTimeline.Models;

/// <summary>
/// プレイヤーカード UI に表示するための表示用モデル。
/// DB エンティティ (PlayerSession) から変換して使用する。
/// </summary>
public class PlayerDisplay
{
    /// <summary>プレイヤーの表示名</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>VRChat ユーザーID。カードクリック時の検索キーとして使用</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>入室日時</summary>
    public DateTime JoinedAt { get; set; }

    /// <summary>退室日時</summary>
    public DateTime? LeftAt { get; set; }

    /// <summary>入室時刻の表示文字列</summary>
    public string JoinedAtText => JoinedAt.ToString(DateFormatHelper.TimeOnly);

    /// <summary>退室時刻の表示文字列</summary>
    public string LeftAtText => LeftAt?.ToString(DateFormatHelper.TimeOnly) ?? "退出不明";

    /// <summary>入室〜退室の時間範囲表示（例: "21:00 ～ 23:30"）</summary>
    public string TimeRange => $"{JoinedAtText} ～ {LeftAtText}";
}
