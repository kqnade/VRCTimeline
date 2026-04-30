using System.Text.Json;
using VRCTimeline.Models;

namespace VRCTimeline.Services;

/// <summary>
/// アプリケーション設定の読み込み・保存を管理するサービス。
/// 設定は AppData フォルダ内の JSON ファイルに永続化される。
/// </summary>
public class SettingsService
{
    /// <summary>アプリケーション設定ファイルの保存ディレクトリ</summary>
    private static readonly string AppDataDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "VRCTimeline");

    /// <summary>設定 JSON ファイルのフルパス</summary>
    private static readonly string SettingsPath = Path.Combine(AppDataDir, "settings.json");

    /// <summary>JSON シリアライズオプション（整形出力有効）</summary>
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    /// <summary>現在の設定値</summary>
    public AppSettings Settings { get; private set; } = new();

    /// <summary>設定保存時に発火するイベント</summary>
    public event Action? SettingsChanged;

    /// <summary>設定ファイルを読み込む。未設定のパスにはデフォルト値を補填する。</summary>
    public async Task LoadAsync()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = await File.ReadAllTextAsync(SettingsPath);
                Settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch
        {
            Settings = new AppSettings();
        }

        if (string.IsNullOrEmpty(Settings.VRChatLogDirectory))
            Settings.VRChatLogDirectory = GetDefaultLogDirectory();

        if (string.IsNullOrEmpty(Settings.PhotoDirectory))
            Settings.PhotoDirectory = GetDefaultPhotoDirectory();
    }

    /// <summary>現在の設定を JSON ファイルに保存し、変更イベントを発火する</summary>
    public async Task SaveAsync()
    {
        Directory.CreateDirectory(AppDataDir);
        var json = JsonSerializer.Serialize(Settings, JsonOptions);
        await File.WriteAllTextAsync(SettingsPath, json);
        SettingsChanged?.Invoke();
    }

    /// <summary>VRChat ログフォルダのデフォルトパスを返す</summary>
    public static string GetDefaultLogDirectory()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "AppData", "LocalLow", "VRChat", "VRChat");
    }

    /// <summary>VRChat 写真フォルダのデフォルトパスを返す</summary>
    public static string GetDefaultPhotoDirectory()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
            "VRChat");
    }
}
