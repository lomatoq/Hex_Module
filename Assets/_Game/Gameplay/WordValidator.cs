using System.Collections.Generic;
using HexWords.Core;

namespace HexWords.Gameplay
{
    public class WordValidator : IWordValidator
    {
        private readonly Dictionary<Language, HashSet<string>> _dictionaryMap;

        public WordValidator(DictionaryDatabase dictionary)
        {
            _dictionaryMap = BuildMap(dictionary);
        }

        public bool TryValidate(string word, LevelDefinition level, LevelSessionState sessionState, out ValidationReason reason)
        {
            var normalized = WordNormalizer.Normalize(word);
            if (normalized.Length < 2)
            {
                reason = ValidationReason.TooShort;
                return false;
            }

            if (!WordNormalizer.IsAsciiOrCyrillicLetterString(normalized))
            {
                reason = ValidationReason.InvalidCharacters;
                return false;
            }

            if (sessionState.acceptedWords.Contains(normalized))
            {
                reason = ValidationReason.AlreadyAccepted;
                return false;
            }

            if (level.validationMode == ValidationMode.LevelOnly)
            {
                if (level.targetWords == null)
                {
                    reason = ValidationReason.NotInLevelTargets;
                    return false;
                }

                for (var i = 0; i < level.targetWords.Length; i++)
                {
                    if (WordNormalizer.Normalize(level.targetWords[i]) == normalized)
                    {
                        reason = ValidationReason.None;
                        return true;
                    }
                }

                reason = ValidationReason.NotInLevelTargets;
                return false;
            }

            if (_dictionaryMap.TryGetValue(level.language, out var words) && words.Contains(normalized))
            {
                reason = ValidationReason.None;
                return true;
            }

            reason = ValidationReason.NotInDictionary;
            return false;
        }

        private static Dictionary<Language, HashSet<string>> BuildMap(DictionaryDatabase dictionary)
        {
            var map = new Dictionary<Language, HashSet<string>>
            {
                { Language.RU, new HashSet<string>() },
                { Language.EN, new HashSet<string>() }
            };

            if (dictionary == null)
            {
                return map;
            }

            for (var i = 0; i < dictionary.entries.Count; i++)
            {
                var entry = dictionary.entries[i];
                var normalized = WordNormalizer.Normalize(entry.word);
                if (normalized.Length > 0)
                {
                    map[entry.language].Add(normalized);
                }
            }

            return map;
        }
    }
}
