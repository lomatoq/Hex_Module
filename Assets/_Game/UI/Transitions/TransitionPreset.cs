using UnityEngine;

namespace HexWords.UI.Transitions
{
    /// <summary>
    /// Reusable ScriptableObject that defines the default element animation config
    /// and block-level settings for appear, disappear, and quick transitions.
    ///
    /// Assign to ScreenAnimator.preset; individual elements can override per-element.
    /// </summary>
    [CreateAssetMenu(menuName = "HexWords/UI/Transition Preset", fileName = "TransitionPreset")]
    public class TransitionPreset : ScriptableObject
    {
        [Header("Appear")]
        [Tooltip("Block-level settings (stagger, playMode, events) for the appear transition.")]
        public BlockSettings appearBlock = new BlockSettings();
        [Tooltip("Default config applied to elements with usePresetForAppear = true.")]
        public TransitionElementConfig appearDefaultConfig = new TransitionElementConfig();

        [Header("Disappear")]
        [Tooltip("Block-level settings for the disappear transition.")]
        public BlockSettings disappearBlock = new BlockSettings { stagger = 0.04f };
        [Tooltip("Default config applied to elements with usePresetForDisappear = true.")]
        public TransitionElementConfig disappearDefaultConfig = new TransitionElementConfig
        {
            alphaEnabled = true,
            alphaFrom    = 1f,
            alphaTo      = 0f,
        };

        [Header("Quick  (tab switches / lightweight transitions)")]
        [Tooltip("Block-level settings for the quick transition.")]
        public BlockSettings quickBlock = new BlockSettings { stagger = 0.03f };
        [Tooltip("Default config applied to elements in a quick transition.")]
        public TransitionElementConfig quickDefaultConfig = new TransitionElementConfig
        {
            duration     = 0.18f,
            alphaEnabled = true,
            alphaFrom    = 0f,
            alphaTo      = 1f,
        };
    }
}
