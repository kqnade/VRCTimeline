using System.Diagnostics;

namespace VRCTimeline.Services;

/// <summary>
/// VRChat のプロトコルリンク (vrchat://) を使用してインスタンスに参加するユーティリティ。
/// </summary>
public static class VRChatLauncher
{
    /// <summary>指定インスタンスIDの VRChat ワールドに参加する</summary>
    public static void LaunchInstance(string instanceId)
    {
        var url = $"vrchat://launch?id={instanceId}";
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }
}
