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

        public WordValidationResult Validate(string word, LevelDefinition level, LevelSessionState sessionState)
        {
            if (level == null)
            {
                return Reject(string.Empty, ValidationReason.NotInLevelTargets);
            }

            if (sessionState == null)
            {
                sessionState = new LevelSessionState();
            }

            var normalized = WordNormalizer.Normalize(word);
            if (normalized.Length < 2)
            {
                return Reject(normalized, ValidationReason.TooShort);
            }

            if (!WordNormalizer.IsAsciiOrCyrillicLetterString(normalized))
            {
                return Reject(normalized, ValidationReason.InvalidCharacters);
            }

            if (sessionState.acceptedWords.Contains(normalized))
            {
                return new WordValidationResult
                {
                    accepted = false,
                    outcome = WordSubmitOutcome.AlreadyAccepted,
                    reason = ValidationReason.AlreadyAccepted,
                    normalizedWord = normalized
                };
            }

            if (IsTargetWord(normalized, level))
            {
                return new WordValidationResult
                {
                    accepted = true,
                    outcome = WordSubmitOutcome.TargetAccepted,
                    reason = ValidationReason.None,
                    normalizedWord = normalized
                };
            }

            if (level.validationMode == ValidationMode.LevelOnly)
            {
                if (!level.allowBonusWords || !level.allowBonusInLevelOnly)
                {
                    return Reject(normalized, ValidationReason.NotInLevelTargets);
                }

                if (level.bonusRequiresEmbeddedInLevelOnly && !IsEmbeddedInTargets(normalized, level.targetWords))
                {
                    return Reject(normalized, ValidationReason.NotInLevelTargets);
                }

                return new WordValidationResult
                {
                    accepted = true,
                    outcome = WordSubmitOutcome.BonusAccepted,
                    reason = ValidationReason.None,
                    normalizedWord = normalized
                };
            }

            if (level.allowBonusWords &&
                _dictionaryMap.TryGetValue(level.language, out var words) &&
                words.Contains(normalized))
            {
                return new WordValidationResult
                {
                    accepted = true,
                    outcome = WordSubmitOutcome.BonusAccepted,
                    reason = ValidationReason.None,
                    normalizedWord = normalized
                };
            }

            return Reject(normalized, ValidationReason.NotInDictionary);
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

        private static WordValidationResult Reject(string normalizedWord, ValidationReason reason)
        {
            return new WordValidationResult
            {
                accepted = false,
                outcome = WordSubmitOutcome.Rejected,
                reason = reason,
                normalizedWord = normalizedWord
            };
        }

        private static bool IsTargetWord(string normalizedWord, LevelDefinition level)
        {
            if (level == null || level.targetWords == null)
            {
                return false;
            }

            for (var i = 0; i < level.targetWords.Length; i++)
            {
                if (WordNormalizer.Normalize(level.targetWords[i]) == normalizedWord)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsEmbeddedInTargets(string normalizedWord, IReadOnlyList<string> targetWords)
        {
            if (targetWords == null || targetWords.Count == 0)
            {
                return false;
            }

            for (var i = 0; i < targetWords.Count; i++)
            {
                var target = WordNormalizer.Normalize(targetWords[i]);
                if (target.IndexOf(normalizedWord, System.StringComparison.Ordinal) >= 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
