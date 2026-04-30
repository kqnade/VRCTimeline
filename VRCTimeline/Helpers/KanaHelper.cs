namespace VRCTimeline.Helpers;

/// <summary>
/// ひらがな・カタカナの正規化ユーティリティ。
/// かな文字の違いを吸収した検索に使用する。
/// </summary>
public static class KanaHelper
{
    /// <summary>ひらがな→カタカナ変換のオフセット値（U+3041→U+30A1）</summary>
    private const int KanaOffset = 0x60;

    /// <summary>ひらがなをすべてカタカナに変換する</summary>
    public static string NormalizeToKatakana(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        return string.Create(input.Length, input, (span, src) =>
        {
            for (int i = 0; i < src.Length; i++)
            {
                var c = src[i];
                span[i] = c is >= 'ぁ' and <= 'ゖ' ? (char)(c + KanaOffset) : c;
            }
        });
    }

    /// <summary>かな文字の差異と大文字小文字を無視して部分一致検索を行う</summary>
    public static bool ContainsKanaInsensitive(string source, string search)
    {
        return NormalizeToKatakana(source)
            .Contains(NormalizeToKatakana(search), StringComparison.OrdinalIgnoreCase);
    }
}
