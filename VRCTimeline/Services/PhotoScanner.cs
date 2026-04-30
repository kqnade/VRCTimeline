using System.Text.RegularExpressions;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using VRCTimeline.Data;
using VRCTimeline.Helpers;
using VRCTimeline.Models;

namespace VRCTimeline.Services;

/// <summary>
/// VRChat 写真フォルダをバッチスキャンし、未登録の写真を DB に一括登録する。
/// ファイル名のタイムスタンプから撮影日時を解析し、対応するワールド訪問と紐づける。
/// </summary>
public class PhotoScanner
{
    /// <summary>ファイル名からタイムスタンプを抽出する正規表現（例: VRChat_2026-04-30_21-00-00）</summary>
    private static readonly Regex PhotoTimestampRegex = new(
        @"VRChat_(\d{4}-\d{2}-\d{2}_\d{2}-\d{2}-\d{2})",
        RegexOptions.Compiled);

    /// <summary>
    /// 指定ディレクトリ内の VRChat 写真をスキャンし、未登録のものを DB に追加する。
    /// 100枚ごとにバッチ保存する。
    /// </summary>
    public async Task ScanAsync(string photoDirectory, IProgress<string>? progress = null, CancellationToken ct = default)
    {
        if (!Directory.Exists(photoDirectory)) return;

        await using var db = new AppDbContext();

        progress?.Report("ワールド訪問データを読み込み中...");
        var worldVisits = await db.WorldVisits
            .OrderBy(v => v.JoinedAt)
            .Select(v => new { v.Id, v.JoinedAt, v.LeftAt })
            .ToListAsync(ct);

        // 既存パスのセットで重複チェックを高速化
        var existingPaths = new HashSet<string>(
            await db.PhotoRecords.Select(p => p.FilePath).ToListAsync(ct));

        var extensions = new[] { "*.png", "*.jpg", "*.jpeg" };
        var photoFiles = extensions
            .SelectMany(ext => Directory.EnumerateFiles(photoDirectory, ext, SearchOption.AllDirectories))
            .Where(f => Path.GetFileName(f).StartsWith("VRChat_", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var batch = new List<PhotoRecord>();
        int processed = 0;
        int added = 0;

        foreach (var filePath in photoFiles)
        {
            ct.ThrowIfCancellationRequested();

            if (existingPaths.Contains(filePath))
            {
                processed++;
                continue;
            }

            var fileName = Path.GetFileName(filePath);
            processed++;
            if (processed % 20 == 0)
                progress?.Report($"写真を処理中: {processed}/{photoFiles.Count}");

            var timestamp = ParsePhotoTimestamp(fileName);
            if (timestamp == null) continue;

            // 撮影時刻に対応するワールド訪問を検索
            var worldVisitId = WorldVisitMatcher.FindWorldVisitId(
                worldVisits, timestamp.Value,
                v => v.JoinedAt, v => v.LeftAt, v => v.Id);

            batch.Add(new PhotoRecord
            {
                FilePath = filePath,
                FileName = fileName,
                TakenAt = timestamp.Value,
                WorldVisitId = worldVisitId
            });
            added++;

            if (batch.Count >= 100)
            {
                db.PhotoRecords.AddRange(batch);
                await db.SaveChangesAsync(ct);
                batch.Clear();
            }
        }

        if (batch.Count > 0)
        {
            db.PhotoRecords.AddRange(batch);
            await db.SaveChangesAsync(ct);
        }

        progress?.Report($"完了: {added} 枚の新しい写真を登録しました");
    }

    /// <summary>
    /// VRChat 写真のファイル名からタイムスタンプを解析する。
    /// 例: "VRChat_2026-04-30_21-00-00.808_2560x1440.png" → 2026/04/30 21:00:00
    /// </summary>
    public static DateTime? ParsePhotoTimestamp(string fileName)
    {
        var match = PhotoTimestampRegex.Match(fileName);
        if (!match.Success) return null;

        return DateTime.TryParseExact(
            match.Groups[1].Value,
            "yyyy-MM-dd_HH-mm-ss",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var result) ? result : null;
    }
}
