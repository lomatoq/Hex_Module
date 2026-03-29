using UnityEngine;

namespace HexWords.Gameplay
{
    [CreateAssetMenu(menuName = "HexWords/Hint Animation Config", fileName = "HintAnimationConfig")]
    public class HintAnimationConfig : ScriptableObject
    {
        [Header("Reveal")]
        [Tooltip("Колькі літар (клетак) падсвяціць")]
        public int revealCount = 2;

        [Header("Pulse")]
        [Tooltip("Колькі разоў мільгае кожная клетка")]
        public int pulseCount = 3;

        [Tooltip("Доўгасць ease-in фазы аднаго пульсу (секунды)")]
        public float pulseFadeIn = 0.22f;

        [Tooltip("Доўгасць ease-out фазы аднаго пульсу (секунды)")]
        public float pulseFadeOut = 0.22f;

        [Tooltip("Пауза паміж паўторнымі пульсамі адной клеткі (секунды)")]
        public float pauseBetweenPulses = 0.10f;

        [Tooltip("Колькі разоў паўтараецца ўся серыя цалкам (1 = без паўтору)")]
        [Min(1)]
        public int repetitionCount = 2;

        [Tooltip("Пауза паміж паўторамі ўсёй серыі (секунды)")]
        public float delayBetweenRepetitions = 0.6f;

        [Header("Sequence")]
        [Tooltip("Задрымка паміж пачаткам анімацыі суседніх клетак (секунды)")]
        public float delayBetweenCells = 0.18f;

        [Header("Scale")]
        [Tooltip("Маштаб клеткі ў піку пульсу")]
        [Range(1f, 1.5f)]
        public float peakScale = 1.12f;
    }
}
