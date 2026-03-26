using HexWords.Core;
using UnityEngine;
using UnityEngine.UI;

namespace HexWords.UI
{
    /// <summary>
    /// Scrollable list of words found in the current session.
    /// Accessible via the book icon in the gameplay header.
    /// Target words are shown in one colour, bonus words in another.
    /// </summary>
    public class FoundWordsScreen : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject root;

        [Header("List")]
        [SerializeField] private Transform  wordListContainer; // parent for word entries
        [SerializeField] private GameObject wordEntryPrefab;   // prefab with a Text child

        [Header("Close")]
        [SerializeField] private Button closeButton;

        [Header("Colours")]
        [SerializeField] private Color targetWordColor = new Color(0.2f, 0.6f, 0.2f);
        [SerializeField] private Color bonusWordColor  = new Color(0.1f, 0.55f, 0.65f);

        private void Awake()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(Hide);

            SetRootVisible(false);
        }

        // ── Public API ─────────────────────────────────────────────────────

        public void Show(LevelSessionState state)
        {
            PopulateList(state);
            SetRootVisible(true);
        }

        public void Hide()
        {
            SetRootVisible(false);
        }

        // ── Internal ───────────────────────────────────────────────────────

        private void PopulateList(LevelSessionState state)
        {
            if (wordListContainer == null || wordEntryPrefab == null) return;

            // Clear previous entries
            foreach (Transform child in wordListContainer)
                Destroy(child.gameObject);

            // Target words first
            foreach (var word in state.acceptedTargetWords)
                AddEntry(word, targetWordColor);

            // Then bonus words
            foreach (var word in state.acceptedBonusWords)
                AddEntry(word, bonusWordColor);
        }

        private void AddEntry(string word, Color color)
        {
            var go   = Instantiate(wordEntryPrefab, wordListContainer);
            var text = go.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.text  = word;
                text.color = color;
            }
        }

        private void SetRootVisible(bool visible)
        {
            if (root != null)
                root.SetActive(visible);
            else
                gameObject.SetActive(visible);
        }
    }
}
