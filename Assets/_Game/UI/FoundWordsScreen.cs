using System.Collections.Generic;
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

        private readonly List<GameObject> _spawnedEntries = new List<GameObject>();

        private void Awake()
        {
            // Only hide the root child panel; never deactivate own GO here.
            // (Deactivating own GO in Awake() prevents OnEnable from firing at scene start.)
            if (root != null)
                root.SetActive(false);
        }

        private void OnEnable()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Hide);
                closeButton.onClick.AddListener(Hide);
            }
        }

        private void OnDisable()
        {
            if (closeButton != null)
                closeButton.onClick.RemoveListener(Hide);
        }

        // ── Public API ─────────────────────────────────────────────────────

        public void Show(LevelSessionState state)
        {
            SetRootVisible(true);
            PopulateList(state);
        }

        public void Hide()
        {
            SetRootVisible(false);
        }

        // ── Internal ───────────────────────────────────────────────────────

        private void PopulateList(LevelSessionState state)
        {
            if (wordListContainer == null || wordEntryPrefab == null) return;

            // Destroy previously spawned entries immediately so the list is
            // clean before we add new ones (avoids double-entries on re-open).
            foreach (var entry in _spawnedEntries)
                if (entry != null) DestroyImmediate(entry);
            _spawnedEntries.Clear();

            // Target words first
            foreach (var word in state.acceptedTargetWords)
                AddEntry(word, targetWordColor);

            // Then bonus words
            foreach (var word in state.acceptedBonusWords)
                AddEntry(word, bonusWordColor);

            // Force layout rebuild so items are positioned correctly on first show
            Canvas.ForceUpdateCanvases();
            if (wordListContainer is RectTransform rt)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
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
            go.SetActive(true);
            _spawnedEntries.Add(go);
        }

        private void SetRootVisible(bool visible)
        {
            // Always ensure the GO itself is active when showing —
            // it may start inactive in the scene (set by SceneHierarchyBuilder).
            if (visible && !gameObject.activeSelf)
                gameObject.SetActive(true);

            if (root != null)
                root.SetActive(visible);
            else
                gameObject.SetActive(visible);
        }
    }
}
