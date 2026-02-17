using System.Collections;
using HexWords.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HexWords.Gameplay
{
    public class HexCellView : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerUpHandler, ICellFxPlayer
    {
        [SerializeField] private Text letterText;
        [SerializeField] private Image background;

        public string CellId { get; private set; }

        public delegate void CellEvent(HexCellView cell);

        public event CellEvent PointerDownOnCell;
        public event CellEvent PointerEnterOnCell;
        public event CellEvent PointerUpOnCell;

        private Color _baseColor = Color.white;
        private Coroutine _fxRoutine;

        public void Bind(CellDefinition cellDefinition)
        {
            CellId = cellDefinition.cellId;
            if (letterText != null)
            {
                EnsureLetterCentered();
                letterText.text = WordNormalizer.Normalize(cellDefinition.letter);
            }

            if (background != null)
            {
                _baseColor = background.color;
            }
        }

        private void EnsureLetterCentered()
        {
            letterText.alignment = TextAnchor.MiddleCenter;

            var textRect = letterText.rectTransform;
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = Vector2.zero;
            textRect.sizeDelta = background != null
                ? background.rectTransform.rect.size
                : new Vector2(120f, 120f);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            PointerDownOnCell?.Invoke(this);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            PointerEnterOnCell?.Invoke(this);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            PointerUpOnCell?.Invoke(this);
        }

        public void OnSelected()
        {
            StopFxRoutine();
            if (background != null)
            {
                background.color = new Color(0.85f, 0.95f, 1f, 1f);
                transform.localScale = Vector3.one * 1.05f;
            }
        }

        public void OnPathAccepted()
        {
            PlayFlashAndReturn(new Color(0.75f, 1f, 0.75f, 1f), 0.2f);
        }

        public void OnPathRejected()
        {
            PlayFlashAndReturn(new Color(1f, 0.8f, 0.8f, 1f), 0.2f);
        }

        public void ResetFx()
        {
            StopFxRoutine();
            transform.localScale = Vector3.one;
            if (background != null)
            {
                background.color = _baseColor;
            }
        }

        private void PlayFlashAndReturn(Color flashColor, float duration)
        {
            StopFxRoutine();
            _fxRoutine = StartCoroutine(FlashAndReturn(flashColor, duration));
        }

        private IEnumerator FlashAndReturn(Color flashColor, float duration)
        {
            if (background == null)
            {
                yield break;
            }

            var startColor = flashColor;
            var startScale = transform.localScale;
            background.color = startColor;

            var t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                var k = Mathf.Clamp01(t / duration);
                background.color = Color.Lerp(startColor, _baseColor, k);
                transform.localScale = Vector3.Lerp(startScale, Vector3.one, k);
                yield return null;
            }

            background.color = _baseColor;
            transform.localScale = Vector3.one;
            _fxRoutine = null;
        }

        private void StopFxRoutine()
        {
            if (_fxRoutine != null)
            {
                StopCoroutine(_fxRoutine);
                _fxRoutine = null;
            }
        }
    }
}
