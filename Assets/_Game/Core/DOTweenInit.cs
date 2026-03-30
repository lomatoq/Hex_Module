// Remove this comment after installing DOTween via Asset Store
#define DOTWEEN

using UnityEngine;
#if DOTWEEN
using DG.Tweening;
#endif

namespace HexWords.Core
{
    /// <summary>
    /// Initializes DOTween once at app start.
    ///
    /// HOW TO ACTIVATE:
    /// 1. Download DOTween from Asset Store (free) or https://dotween.demigiant.com
    /// 2. Import the .unitypackage into the project
    /// 3. Run Tools → Demigiant → DOTween Utility Panel → Setup DOTween
    /// 4. Uncomment #define DOTWEEN at the top of:
    ///      - DOTweenInit.cs
    ///      - HexCellView.cs
    ///      - SwipeTrailView.cs
    ///      - GridView.cs
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class DOTweenInit : MonoBehaviour
    {
#if DOTWEEN
        [SerializeField] private int          maxTweeners  = 200;
        [SerializeField] private int          maxSequences = 50;
        [SerializeField] private bool         safeMode     = true;

        private void Awake()
        {
            DOTween.Init(recycleAllByDefault: true, useSafeMode: safeMode, logBehaviour: LogBehaviour.ErrorsOnly)
                   .SetCapacity(maxTweeners, maxSequences);

            DOTween.defaultEaseType           = Ease.OutCubic;
            DOTween.defaultAutoPlay           = AutoPlay.All;
            DOTween.defaultRecyclable         = true;
            DOTween.defaultAutoKillOnComplete = true;
        }
#endif
    }
}
