using System;

namespace HexWords.Core
{
    public static class WordNormalizer
    {
        public static string Normalize(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return string.Empty;
            }

            return raw.Trim().ToUpperInvariant();
        }

        public static bool IsAsciiOrCyrillicLetterString(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            for (var i = 0; i < value.Length; i++)
            {
                var c = value[i];
                var isLatin = c is >= 'A' and <= 'Z';
                var isCyrillic = c is >= 'А' and <= 'Я' || c == 'Ё';
                if (!isLatin && !isCyrillic)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
