namespace VRCTimeline.Models;

/// <summary>
/// 処理済みログファイルの記録。
/// 同一ログファイルの重複解析を防ぐために、ファイル名と読み取り位置を保存する。
/// </summary>
public class ProcessedLogFile
{
    /// <summary>主キー</summary>
    public int Id { get; set; }

    /// <summary>ログファイル名（output_log_*.txt）</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>最後に読み取ったバイト位置（次回はここから再開）</summary>
    public long LastPosition { get; set; }

    /// <summary>最後に処理した日時</summary>
    public DateTime ProcessedAt { get; set; }
}
