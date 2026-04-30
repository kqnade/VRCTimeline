namespace VRCTimeline.Helpers;

/// <summary>
/// 写真や動画の撮影時刻から、対応するワールド訪問を二分探索で特定するユーティリティ。
/// </summary>
public static class WorldVisitMatcher
{
    /// <summary>
    /// JoinedAt 昇順でソート済みのワールド訪問リストから、
    /// 指定タイムスタンプが含まれる訪問のIDを二分探索で返す。
    /// 該当する訪問がなければ null。
    /// </summary>
    public static int? FindWorldVisitId<T>(
        IList<T> sortedVisits,
        DateTime timestamp,
        Func<T, DateTime> getJoinedAt,
        Func<T, DateTime?> getLeftAt,
        Func<T, int> getId)
    {
        int lo = 0, hi = sortedVisits.Count - 1, idx = -1;
        while (lo <= hi)
        {
            int mid = (lo + hi) / 2;
            if (getJoinedAt(sortedVisits[mid]) <= timestamp)
            {
                idx = mid;
                lo = mid + 1;
            }
            else
            {
                hi = mid - 1;
            }
        }

        if (idx < 0) return null;

        var visit = sortedVisits[idx];
        var leftAt = getLeftAt(visit);
        return (leftAt == null || leftAt >= timestamp) ? getId(visit) : null;
    }
}
