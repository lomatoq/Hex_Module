using UnityEngine;

namespace HexWords.Core
{
    public enum SoundEffect
    {
        Tap,
        WordAccepted,
        WordRejected,
        LevelComplete,
        Coins,
        HintUsed,
    }

    /// <summary>
    /// Plays SFX and background music. Persists enabled state via PlayerPrefs.
    /// Attach to a persistent GameObject or use via a reference from GameBootstrap.
    /// </summary>
    public class SoundManager : MonoBehaviour
    {
        private const string PrefSfxEnabled   = "HexWords.SfxEnabled";
        private const string PrefMusicEnabled  = "HexWords.MusicEnabled";

        [Header("SFX Clips")]
        [SerializeField] private AudioClip tapClip;
        [SerializeField] private AudioClip wordAcceptedClip;
        [SerializeField] private AudioClip wordRejectedClip;
        [SerializeField] private AudioClip levelCompleteClip;
        [SerializeField] private AudioClip coinsClip;
        [SerializeField] private AudioClip hintUsedClip;

        [Header("Music")]
        [SerializeField] private AudioClip ambientMusicClip;
        [SerializeField] private AudioSource musicSource;

        [Header("SFX Pool")]
        [SerializeField] private AudioSource[] sfxSources;

        private bool _sfxEnabled;
        private bool _musicEnabled;
        private int  _sfxIndex;

        public bool SfxEnabled   => _sfxEnabled;
        public bool MusicEnabled => _musicEnabled;

        private void Awake()
        {
            _sfxEnabled   = PlayerPrefs.GetInt(PrefSfxEnabled, 1) == 1;
            _musicEnabled = PlayerPrefs.GetInt(PrefMusicEnabled, 1) == 1;

            if (musicSource != null && ambientMusicClip != null)
            {
                musicSource.clip   = ambientMusicClip;
                musicSource.loop   = true;
                musicSource.volume = 0.4f;
                if (_musicEnabled) musicSource.Play();
            }
        }

        public void PlaySound(SoundEffect effect)
        {
            if (!_sfxEnabled) return;

            var clip = GetClip(effect);
            if (clip == null) return;

            var source = GetNextSource();
            if (source != null)
                source.PlayOneShot(clip);
        }

        public void SetSfxEnabled(bool enabled)
        {
            _sfxEnabled = enabled;
            PlayerPrefs.SetInt(PrefSfxEnabled, enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void SetMusicEnabled(bool enabled)
        {
            _musicEnabled = enabled;
            PlayerPrefs.SetInt(PrefMusicEnabled, enabled ? 1 : 0);
            PlayerPrefs.Save();

            if (musicSource == null) return;
            if (enabled) musicSource.Play();
            else         musicSource.Stop();
        }

        private AudioClip GetClip(SoundEffect effect) => effect switch
        {
            SoundEffect.Tap           => tapClip,
            SoundEffect.WordAccepted  => wordAcceptedClip,
            SoundEffect.WordRejected  => wordRejectedClip,
            SoundEffect.LevelComplete => levelCompleteClip,
            SoundEffect.Coins         => coinsClip,
            SoundEffect.HintUsed      => hintUsedClip,
            _                         => null
        };

        private AudioSource GetNextSource()
        {
            if (sfxSources == null || sfxSources.Length == 0) return null;
            var source = sfxSources[_sfxIndex % sfxSources.Length];
            _sfxIndex++;
            return source;
        }
    }
}
