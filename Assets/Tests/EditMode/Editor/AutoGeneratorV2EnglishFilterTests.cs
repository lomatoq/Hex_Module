using System;
using System.Collections.Generic;
using System.Reflection;
using HexWords.Core;
using HexWords.EditorTools;
using NUnit.Framework;
using UnityEngine;

namespace HexWords.Tests.EditMode
{
    public class AutoGeneratorV2EnglishFilterTests
    {
        private AutoLevelGeneratorWindow _window;
        private GenerationProfile _profile;
        private MethodInfo _isAllowedMethod;
        private MethodInfo _passesQualityMethod;

        [SetUp]
        public void SetUp()
        {
            _window = ScriptableObject.CreateInstance<AutoLevelGeneratorWindow>();
            _profile = ScriptableObject.CreateInstance<GenerationProfile>();
            _profile.language = Language.EN;

            var type = typeof(AutoLevelGeneratorWindow);
            var profileField = type.GetField("_profile", BindingFlags.NonPublic | BindingFlags.Instance);
            profileField.SetValue(_window, _profile);

            _isAllowedMethod = type.GetMethod("IsAllowedEnglishCandidate", BindingFlags.NonPublic | BindingFlags.Static);
            _passesQualityMethod = type.GetMethod("PassesWordSetQuality", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        [TearDown]
        public void TearDown()
        {
            if (_window != null)
            {
                ScriptableObject.DestroyImmediate(_window);
            }

            if (_profile != null)
            {
                ScriptableObject.DestroyImmediate(_profile);
            }
        }

        [Test]
        public void IsAllowedEnglishCandidate_RejectsCalendarAndAcronymTokens()
        {
            var popularity = new Dictionary<string, int>(StringComparer.Ordinal)
            {
                ["THU"] = 100,
                ["GMT"] = 120,
                ["SON"] = 300
            };

            Assert.IsFalse(CallIsAllowed("THU", popularity));
            Assert.IsFalse(CallIsAllowed("GMT", popularity));
            Assert.IsTrue(CallIsAllowed("SON", popularity));
        }

        [Test]
        public void IsAllowedEnglishCandidate_RejectsWeakRankShortWords()
        {
            var popularity = new Dictionary<string, int>(StringComparer.Ordinal)
            {
                ["AYE"] = 1523,
                ["THUS"] = 3168,
                ["ABOUT"] = 2200
            };

            Assert.IsFalse(CallIsAllowed("AYE", popularity));
            Assert.IsFalse(CallIsAllowed("THUS", popularity));
            Assert.IsTrue(CallIsAllowed("ABOUT", popularity));
        }

        [Test]
        public void PassesWordSetQuality_RejectsDerivativeChains()
        {
            var words = new[] { "BAY", "BAYE", "BAYER", "AYE" };
            var ok = CallPassesQuality(words, out var reason);

            Assert.IsFalse(ok);
            Assert.AreEqual("too-many-derivatives", reason);
        }

        [Test]
        public void PassesWordSetQuality_AcceptsBalancedSet()
        {
            var words = new[] { "ROUTE", "SOUL", "ROSE", "GOES" };
            var ok = CallPassesQuality(words, out var reason);

            Assert.IsTrue(ok, reason);
        }

        private bool CallIsAllowed(string word, Dictionary<string, int> popularity)
        {
            var args = new object[]
            {
                word,
                popularity,
                new HashSet<string>(StringComparer.Ordinal)
            };

            return (bool)_isAllowedMethod.Invoke(null, args);
        }

        private bool CallPassesQuality(IReadOnlyList<string> words, out string reason)
        {
            var args = new object[] { words, null };
            var ok = (bool)_passesQualityMethod.Invoke(_window, args);
            reason = (string)args[1];
            return ok;
        }
    }
}
