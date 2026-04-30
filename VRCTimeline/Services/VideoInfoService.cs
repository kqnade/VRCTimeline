using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace VRCTimeline.Services;

/// <summary>
/// 動画 URL からタイトルとサムネイルを取得するサービス。
/// noembed.com API を使用し、サムネイル画像をローカルにキャッシュする。
/// </summary>
public class VideoInfoService
{
    /// <summary>HTTP クライアント（アプリケーション全体で共有）</summary>
    private static readonly HttpClient Http = new();

    /// <summary>API レート制限用セマフォ（同時リクエスト数: 1）</summary>
    private static readonly SemaphoreSlim RateLimiter = new(1, 1);

    /// <summary>サムネイルキャッシュの保存ディレクトリ</summary>
    public static readonly string CacheDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "VRCTimeline", "cache", "thumbnails");

    /// <summary>指定 URL が YouTube の動画かどうかを判定する</summary>
    public static bool IsYouTubeUrl(string url) =>
        url.Contains("youtube.com", StringComparison.OrdinalIgnoreCase)
        || url.Contains("youtu.be", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// 動画 URL からタイトルとサムネイルを取得する。
    /// サムネイルはローカルにキャッシュされ、そのパスを返す。
    /// レート制限のため、リクエスト間に 600ms の遅延を挿入する。
    /// </summary>
    public async Task<(string? Title, string? ThumbnailPath)> FetchInfoAsync(string url)
    {
        await RateLimiter.WaitAsync();
        try
        {
            await Task.Delay(600);

            var response = await Http.GetStringAsync(
                $"https://noembed.com/embed?url={Uri.EscapeDataString(url)}");

            using var doc = JsonDocument.Parse(response);
            var root = doc.RootElement;

            var title = root.TryGetProperty("title", out var t) ? t.GetString() : null;
            var thumbUrl = root.TryGetProperty("thumbnail_url", out var th) ? th.GetString() : null;

            string? localPath = null;
            if (thumbUrl != null)
            {
                Directory.CreateDirectory(CacheDir);
                var fileName = GetCacheFileName(url);
                localPath = Path.Combine(CacheDir, fileName);
                if (!File.Exists(localPath))
                {
                    var imageBytes = await Http.GetByteArrayAsync(thumbUrl);
                    await File.WriteAllBytesAsync(localPath, imageBytes);
                }
            }

            return (title, localPath);
        }
        catch
        {
            return (null, null);
        }
        finally
        {
            RateLimiter.Release();
        }
    }

    /// <summary>URL の SHA256 ハッシュから一意なキャッシュファイル名を生成する</summary>
    private static string GetCacheFileName(string url)
    {
        var hash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(url)))[..16];
        return $"{hash}.jpg";
    }

    /// <summary>使用されていないサムネイルキャッシュファイルを削除する</summary>
    public static void CleanupThumbnails(HashSet<string> pathsToKeep)
    {
        if (!Directory.Exists(CacheDir)) return;
        foreach (var file in Directory.EnumerateFiles(CacheDir))
        {
            if (!pathsToKeep.Contains(file))
            {
                try { File.Delete(file); } catch { }
            }
        }
    }

    /// <summary>サムネイルキャッシュフォルダごと削除する</summary>
    public static void ClearCache()
    {
        if (Directory.Exists(CacheDir))
            Directory.Delete(CacheDir, true);
    }

    /// <summary>サムネイルキャッシュの合計サイズ（バイト）を返す</summary>
    public static long GetCacheSizeBytes()
    {
        if (!Directory.Exists(CacheDir)) return 0;
        return Directory.EnumerateFiles(CacheDir).Sum(f => new FileInfo(f).Length);
    }
}
