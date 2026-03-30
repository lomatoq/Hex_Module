using DG.Tweening;
using UnityEngine;

namespace HexWords.Core
{
    /// <summary>
    /// Initializes DOTween once at app start.
    /// Attach to any persistent GameObject in the first scene (e.g. GameBootstrap).
    ///
    /// After first run, DOTween creates a DOTweenSettings asset — open it via
    /// Tools → Demigiant → DOTween Utility Panel to fine-tune capacity/safety checks.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class DOTweenInit : MonoBehaviour
    {
        [Tooltip("Max concurrent tweens (increase if you see 'max tweens exceeded')")]
        [SerializeField] private int maxTweeners = 200;
        [SerializeField] private int maxSequences = 50;
        [SerializeField] private bool safeMode    = true;
        [SerializeField] private LogBehaviour logBehaviour = LogBehaviour.ErrorsOnly;

        private void Awake()
        {
            DOTween.Init(recycleAllByDefault: true,
                         useSafeMode: safeMode,
                         logBehaviour: logBehaviour)
                   .SetCapacity(maxTweeners, maxSequences);

            DOTween.defaultEaseType         = Ease.OutCubic;
            DOTween.defaultAutoPlay         = AutoPlay.All;
            DOTween.defaultRecyclable       = true;
            DOTween.defaultAutoKillOnComplete = true;
        }
    }
}
