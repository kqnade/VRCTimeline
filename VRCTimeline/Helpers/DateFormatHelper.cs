using System.Globalization;

namespace VRCTimeline.Helpers;

/// <summary>
/// 日付・時刻のフォーマットユーティリティ。
/// 日本語カルチャでの曜日付き表示などを提供する。
/// </summary>
public static class DateFormatHelper
{
    /// <summary>日本語カルチャ情報（曜日表示に使用）</summary>
    public static readonly CultureInfo JaCulture = CultureInfo.GetCultureInfo("ja-JP");

    /// <summary>日付＋曜日＋時刻フォーマット (例: 2024/01/15 (月) 14:30)</summary>
    public const string DateWithDayAndTime = "yyyy/MM/dd (ddd) HH:mm";

    /// <summary>日付＋曜日フォーマット (例: 2024/01/15 (月))</summary>
    public const string DateWithDay = "yyyy/MM/dd (ddd)";

    /// <summary>日付＋曜日＋秒付き時刻フォーマット (例: 2024/01/15 (月) 14:30:45)</summary>
    public const string DateWithDayAndSeconds = "yyyy/MM/dd (ddd) HH:mm:ss";

    /// <summary>時刻のみフォーマット (例: 14:30:45)</summary>
    public const string TimeOnly = "HH:mm:ss";

    /// <summary>日時を曜日付きの日本語形式で文字列化する</summary>
    public static string FormatDateWithDayAndTime(DateTime dt)
        => dt.ToString(DateWithDayAndTime, JaCulture);

    /// <summary>入室〜退室の時間範囲を文字列化する（未退室は "滞在中"）</summary>
    public static string FormatTimeRange(DateTime from, DateTime? to)
        => $"{FormatDateWithDayAndTime(from)} ～ {(to.HasValue ? FormatDateWithDayAndTime(to.Value) : "滞在中")}";
}
