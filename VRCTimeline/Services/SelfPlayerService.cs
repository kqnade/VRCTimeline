using VRCTimeline.Services.LogParser;

namespace VRCTimeline.Services;

/// <summary>
/// ログファイルから自分自身のプレイヤー名とユーザーIDを特定するサービス。
/// "User Authenticated:" 行で表示名を取得し、続く OnPlayerJoined 行からユーザーIDを抽出する。
/// 結果はキャッシュされ、アプリ起動中は再解析しない。
/// </summary>
public class SelfPlayerService
{
    private readonly SettingsService _settingsService;

    /// <summary>解析済みの自プレイヤー表示名</summary>
    private string? _cachedName;

    /// <summary>解析済みの自プレイヤーユーザーID</summary>
    private string? _cachedUserId;

    public SelfPlayerService(SettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    /// <summary>自分のプレイヤー表示名を返す。未取得の場合はログファイルを解析する</summary>
    public async Task<string> GetSelfPlayerNameAsync()
    {
        if (!string.IsNullOrEmpty(_cachedName))
            return _cachedName;

        await Task.Run(FindSelfFromLog);
        return _cachedName ?? "";
    }

    /// <summary>自分の VRChat ユーザーIDを返す。未取得の場合はログファイルを解析する</summary>
    public async Task<string> GetSelfUserIdAsync()
    {
        if (_cachedUserId != null)
            return _cachedUserId;

        await Task.Run(FindSelfFromLog);
        return _cachedUserId ?? "";
    }

    /// <summary>
    /// ログファイルを新しい順に走査し、自プレイヤーの表示名とユーザーIDを特定する。
    /// 手順: "User Authenticated:" で表示名を取得 → その名前の OnPlayerJoined 行から usr_xxx を抽出。
    /// </summary>
    private void FindSelfFromLog()
    {
        var logDir = _settingsService.Settings.VRChatLogDirectory;
        if (string.IsNullOrEmpty(logDir) || !Directory.Exists(logDir))
            return;

        var logFiles = Directory.GetFiles(logDir, "output_log_*.txt")
            .OrderByDescending(f => new FileInfo(f).LastWriteTime);

        foreach (var logFile in logFiles)
        {
            try
            {
                using var stream = new FileStream(logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(stream);

                string? authenticatedName = null;
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    // まず認証行を探す
                    if (authenticatedName == null)
                    {
                        var authMatch = LogPatterns.UserAuthenticatedRegex().Match(line);
                        if (authMatch.Success)
                            authenticatedName = LogPatterns.CleanPlayerName(authMatch.Groups[1].Value.Trim());
                        continue;
                    }

                    // 認証名と一致する最初の OnPlayerJoined から UserId を取得
                    var joinMatch = LogPatterns.PlayerJoinedRegex().Match(line);
                    if (joinMatch.Success)
                    {
                        var rawName = joinMatch.Groups[1].Value.Trim();
                        var cleanName = LogPatterns.CleanPlayerName(rawName);
                        if (cleanName == authenticatedName)
                        {
                            _cachedName = authenticatedName;
                            _cachedUserId = LogPatterns.ExtractUserId(rawName);
                            return;
                        }
                    }
                }

                // OnPlayerJoined が見つからなかった場合は表示名だけ保存
                if (authenticatedName != null)
                {
                    _cachedName = authenticatedName;
                    _cachedUserId = "";
                    return;
                }
            }
            catch { continue; }
        }
    }
}
