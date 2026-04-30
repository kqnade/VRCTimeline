namespace VRCTimeline.Models;

/// <summary>
/// VRChat ワールド内で再生された動画の DB エンティティ。
/// URL、タイトル、サムネイル情報を保持する。
/// </summary>
public class VideoRecord
{
    /// <summary>主キー</summary>
    public int Id { get; set; }

    /// <summary>動画 URL が検出された日時</summary>
    public DateTime DetectedAt { get; set; }

    /// <summary>動画の URL</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>動画タイトル（外部 API から取得、未取得時は null）</summary>
    public string? Title { get; set; }

    /// <summary>サムネイル画像の URL</summary>
    public string? ThumbnailUrl { get; set; }

    /// <summary>ローカルにキャッシュされたサムネイル画像のファイルパス</summary>
    public string? ThumbnailPath { get; set; }

    /// <summary>動画再生時に滞在していたワールド訪問の外部キー（nullable）</summary>
    public int? WorldVisitId { get; set; }

    /// <summary>関連するワールド訪問（ナビゲーションプロパティ）</summary>
    public WorldVisit? WorldVisit { get; set; }
}
