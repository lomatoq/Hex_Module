using System.Collections;
using HexWords.Core;
using HexWords.UI;
using UnityEngine;
using UnityEngine.UI;

namespace HexWords.Gameplay
{
    /// <summary>
    /// Manages the 5-step interactive tutorial per wiki spec.
    ///
    /// Step 1 (Level 1) — Animated swipe-path hint over the first target word tiles.
    ///                    Loops until the player touches the screen.
    /// Step 2 (Level 1) — Player swipes the word; word preview + badge appear.
    /// Step 3 (Level 1) — After first word: darken UI, highlight progress bar,
    ///                    show "Gain points to pass the level". Tap to continue.
    /// Step 4 (Level 1) — Level complete (normal flow).
    /// Step 5 (Level 2) — Darken UI, pulse Hint button, show "Hint reveals letters
    ///                    of the longest word". Player taps hint → free demo.
    /// </summary>
    public class TutorialController : MonoBehaviour
    {
        [Header("Overlay")]
        [SerializeField] private GameObject dimOverlay;          // semi-transparent dark panel
        [SerializeField] private Text instructionText;           // floating instruction label
        [SerializeField] private GameObject tapToContinuePrompt; // "Tap to continue"

        [Header("Swipe hint animation")]
        [SerializeField] private GameObject swipeTrailPrefab;    // animated glowing trail

        [Header("Hint button highlight")]
        [SerializeField] private GameObject hintButtonHighlight; // pulse ring around hint btn

        private LevelDefinition _level;
        private GridView        _gridView;
        private LevelHudView    _hudView;
        private Coroutine       _activeRoutine;
        private bool            _waitingForTap;

        // ── Public entry points ────────────────────────────────────────────

        /// <summary>Begins Step 1 tutorial for Level 1.</summary>
        public void StartSwipeTutorial(LevelDefinition level, GridView gridView)
        {
            _level    = level;
            _gridView = gridView;

            if (_activeRoutine != null) StopCoroutine(_activeRoutine);
            _activeRoutine = StartCoroutine(RunSwipeTutorialStep1());
        }

        /// <summary>Begins Step 5 tutorial for Level 2 (hint booster onboarding).</summary>
        public void StartHintTutorial(LevelHudView hudView)
        {
            _hudView = hudView;

            if (_activeRoutine != null) StopCoroutine(_activeRoutine);
            _activeRoutine = StartCoroutine(RunHintTutorialStep5());
        }

        /// <summary>Called by GameBootstrap when Step 3 should trigger (first word accepted).</summary>
        public void TriggerProgressBarStep()
        {
            if (_activeRoutine != null) StopCoroutine(_activeRoutine);
            _activeRoutine = StartCoroutine(RunProgressBarStep3());
        }

        // ── Tutorial steps ─────────────────────────────────────────────────

        private IEnumerator RunSwipeTutorialStep1()
        {
            if (_level?.targetWords == null || _level.targetWords.Length == 0)
                yield break;

            // Pick the first target word as the tutorial word
            string tutorialWord = _level.targetWords[0];

            ShowInstruction($"Swipe to spell  {tutorialWord}");

            // Loop the animated swipe trail until the player touches
            bool playerTouched = false;
            while (!playerTouched)
            {
                if (swipeTrailPrefab != null)
                    yield return StartCoroutine(AnimateSwipeHint(tutorialWord));
                else
                    yield return new WaitForSeconds(1.5f);

                // Check for any touch/mouse input
                if (Input.touchCount > 0 || Input.GetMouseButtonDown(0))
                    playerTouched = true;
            }

            HideInstruction();
            // Steps 2 & 4 play out via normal gameplay — no extra coroutine needed.
        }

        private IEnumerator RunProgressBarStep3()
        {
            // Darken UI
            SetDim(true);
            ShowInstruction("Gain points to pass the level!");

            if (tapToContinuePrompt != null)
                tapToContinuePrompt.SetActive(true);

            // Wait for tap
            yield return WaitForTap();

            // Restore
            SetDim(false);
            HideInstruction();
            if (tapToContinuePrompt != null)
                tapToContinuePrompt.SetActive(false);
        }

        private IEnumerator RunHintTutorialStep5()
        {
            yield return new WaitForSeconds(0.5f); // brief pause before tutorial

            SetDim(true);
            ShowInstruction("Tap Hint to reveal letters of the best word!");

            if (hintButtonHighlight != null)
                hintButtonHighlight.SetActive(true);

            // Wait until the player taps Hint (GameBootstrap routes hint clicks;
            // we simply wait a beat and then dismiss so the hint itself teaches.)
            yield return new WaitForSeconds(5f); // auto-dismiss after 5 s if no tap

            if (hintButtonHighlight != null)
                hintButtonHighlight.SetActive(false);

            SetDim(false);
            HideInstruction();
        }

        // ── Swipe hint animation ───────────────────────────────────────────

        private IEnumerator AnimateSwipeHint(string word)
        {
            // Resolve world positions of cells for this word (best effort)
            // GridView exposes cell transforms; we iterate them in word order.
            // If GridView doesn't provide a direct lookup, skip gracefully.

            yield return new WaitForSeconds(0.5f);

            float duration = 0.8f;
            float elapsed  = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                // Actual trail rendering requires cell positions from GridView.
                // When GridView exposes a GetCellPosition(int cellId) API,
                // use it here to move the swipeTrail along the word path.
                yield return null;
            }

            yield return new WaitForSeconds(0.6f); // pause before loop
        }

        // ── Helpers ────────────────────────────────────────────────────────

        private void ShowInstruction(string text)
        {
            if (instructionText != null)
            {
                instructionText.gameObject.SetActive(true);
                instructionText.text = text;
            }
        }

        private void HideInstruction()
        {
            if (instructionText != null)
                instructionText.gameObject.SetActive(false);
        }

        private void SetDim(bool dim)
        {
            if (dimOverlay != null)
                dimOverlay.SetActive(dim);
        }

        private IEnumerator WaitForTap()
        {
            _waitingForTap = true;
            while (_waitingForTap)
            {
                if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
                    _waitingForTap = false;
                else if (Input.GetMouseButtonDown(0))
                    _waitingForTap = false;

                yield return null;
            }
        }
    }
}
