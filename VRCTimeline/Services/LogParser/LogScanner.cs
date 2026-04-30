using System.Globalization;
using Microsoft.EntityFrameworkCore;
using VRCTimeline.Data;
using VRCTimeline.Models;
using VRCTimeline.Services;

namespace VRCTimeline.Services.LogParser;

/// <summary>
/// VRChat ログファイルのバッチスキャナー。
/// 過去のログファイルを順番に読み込み、ワールド訪問・プレイヤーセッション等を DB に保存する。
/// ProcessedLogFile テーブルで処理済み位置を記録し、差分スキャンに対応。
/// </summary>
public class LogScanner
{
    /// <summary>
    /// 指定ディレクトリ内の全ログファイルをスキャンし、未処理部分を解析して DB に保存する。
    /// </summary>
    public async Task ScanAllLogsAsync(string logDirectory, IProgress<string>? progress = null, CancellationToken ct = default)
    {
        if (!Directory.Exists(logDirectory)) return;

        var logFiles = Directory.GetFiles(logDirectory, "output_log_*.txt")
            .OrderBy(f => new FileInfo(f).CreationTime)
            .ToList();

        await using var db = new AppDbContext();
        await db.Database.EnsureCreatedAsync(ct);

        foreach (var logFile in logFiles)
        {
            ct.ThrowIfCancellationRequested();

            var fileName = Path.GetFileName(logFile);
            progress?.Report($"スキャン中: {fileName}");

            // 前回の処理済み位置を取得（なければ先頭から）
            var processed = await db.ProcessedLogFiles
                .FirstOrDefaultAsync(p => p.FileName == fileName, ct);

            long startPosition = processed?.LastPosition ?? 0;
            var fileInfo = new FileInfo(logFile);

            if (startPosition >= fileInfo.Length) continue;

            await ScanFileAsync(db, logFile, startPosition, ct);

            // 処理済み位置を更新
            if (processed == null)
            {
                db.ProcessedLogFiles.Add(new ProcessedLogFile
                {
                    FileName = fileName,
                    LastPosition = fileInfo.Length,
                    ProcessedAt = DateTime.Now
                });
            }
            else
            {
                processed.LastPosition = fileInfo.Length;
                processed.ProcessedAt = DateTime.Now;
            }

            await db.SaveChangesAsync(ct);
        }
    }

    /// <summary>
    /// 1つのログファイルを指定位置から読み込み、イベントを解析して DB に保存する。
    /// </summary>
    private static async Task ScanFileAsync(AppDbContext db, string filePath, long startPosition, CancellationToken ct)
    {
        // 前回から継続中の未閉ワールド訪問を取得
        WorldVisit? currentVisit = await db.WorldVisits
            .Include(v => v.PlayerSessions)
            .Where(v => v.LeftAt == null)
            .OrderByDescending(v => v.JoinedAt)
            .FirstOrDefaultAsync(ct);

        // 動画 URL の重複検出用
        var lastVideoUrl = await db.VideoRecords
            .OrderByDescending(v => v.DetectedAt)
            .Select(v => v.Url)
            .FirstOrDefaultAsync(ct);

        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        stream.Position = startPosition;
        using var reader = new StreamReader(stream);

        string? line;
        int lineCount = 0;

        while ((line = await reader.ReadLineAsync(ct)) != null)
        {
            ct.ThrowIfCancellationRequested();

            // タイムスタンプのない行はスキップ
            var timestampMatch = LogPatterns.TimestampRegex().Match(line);
            if (!timestampMatch.Success) continue;

            if (!DateTime.TryParseExact(timestampMatch.Groups[1].Value,
                LogPatterns.TimestampFormat, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var timestamp))
                continue;

            // ── ワールド入室 ──
            var roomMatch = LogPatterns.EnteringRoomRegex().Match(line);
            if (roomMatch.Success)
            {
                // 前のワールド訪問を閉じる
                if (currentVisit is { LeftAt: null })
                {
                    currentVisit.LeftAt = timestamp;
                    foreach (var s in currentVisit.PlayerSessions.Where(s => s.LeftAt == null))
                        s.LeftAt = timestamp;
                }

                currentVisit = new WorldVisit
                {
                    WorldName = roomMatch.Groups[1].Value.Trim(),
                    JoinedAt = timestamp
                };
                db.WorldVisits.Add(currentVisit);
                lastVideoUrl = null;
                await db.SaveChangesAsync(ct);
                continue;
            }

            // ── インスタンス接続（ワールドID・インスタンスID の補完） ──
            var instanceMatch = LogPatterns.JoiningInstanceRegex().Match(line);
            if (instanceMatch.Success && currentVisit != null)
            {
                var fullId = instanceMatch.Groups[1].Value.Trim();
                currentVisit.InstanceId = fullId;
                currentVisit.WorldId = LogPatterns.ExtractWorldId(fullId);
                await db.SaveChangesAsync(ct);
                continue;
            }

            // ── プレイヤー入室 ──
            var joinMatch = LogPatterns.PlayerJoinedRegex().Match(line);
            if (joinMatch.Success && currentVisit != null)
            {
                var rawName = joinMatch.Groups[1].Value.Trim();
                currentVisit.PlayerSessions.Add(new PlayerSession
                {
                    DisplayName = LogPatterns.CleanPlayerName(rawName),
                    UserId = LogPatterns.ExtractUserId(rawName),
                    JoinedAt = timestamp
                });
                lineCount++;
                if (lineCount % 50 == 0)
                    await db.SaveChangesAsync(ct);
                continue;
            }

            // ── プレイヤー退室（UserId 優先でセッションを照合） ──
            var leftMatch = LogPatterns.PlayerLeftRegex().Match(line);
            if (leftMatch.Success && currentVisit != null)
            {
                var rawName = leftMatch.Groups[1].Value.Trim();
                var userId = LogPatterns.ExtractUserId(rawName);
                var playerName = LogPatterns.CleanPlayerName(rawName);

                var session = currentVisit.PlayerSessions
                    .Where(s => (!string.IsNullOrEmpty(userId) ? s.UserId == userId : s.DisplayName == playerName) && s.LeftAt == null)
                    .OrderByDescending(s => s.JoinedAt)
                    .FirstOrDefault();

                if (session != null)
                    session.LeftAt = timestamp;

                lineCount++;
                if (lineCount % 50 == 0)
                    await db.SaveChangesAsync(ct);
                continue;
            }

            // ── 通知受信 ──
            var notifMatch = LogPatterns.NotificationRegex().Match(line);
            if (notifMatch.Success)
            {
                var sender = LogPatterns.CleanPlayerName(notifMatch.Groups[1].Value.Trim());
                var notifType = notifMatch.Groups[2].Value.Trim();
                if (notifType is "invite" or "requestInvite" or "boop")
                {
                    db.NotificationRecords.Add(new NotificationRecord
                    {
                        ReceivedAt = timestamp,
                        SenderName = sender,
                        NotificationType = notifType,
                        WorldVisitId = currentVisit?.Id
                    });
                    lineCount++;
                    if (lineCount % 50 == 0)
                        await db.SaveChangesAsync(ct);
                }
                continue;
            }

            // ── 動画再生検出 ──
            var videoMatch = LogPatterns.VideoPlaybackRegex().Match(line);
            if (videoMatch.Success)
            {
                var url = videoMatch.Groups[1].Value.Trim();
                if (url != lastVideoUrl)
                {
                    var exists = await db.VideoRecords.AnyAsync(v => v.Url == url && v.DetectedAt == timestamp, ct);
                    if (!exists)
                    {
                        db.VideoRecords.Add(new VideoRecord
                        {
                            DetectedAt = timestamp,
                            Url = url,
                            WorldVisitId = currentVisit?.Id
                        });
                        lineCount++;
                        if (lineCount % 50 == 0)
                            await db.SaveChangesAsync(ct);
                    }
                    lastVideoUrl = url;
                }
            }
        }

        await db.SaveChangesAsync(ct);
    }
}
