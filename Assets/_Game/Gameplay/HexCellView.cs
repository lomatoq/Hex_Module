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

        public void Bind(CellDefinition cellDefinition)
        {
            CellId = cellDefinition.cellId;
            if (letterText != null)
            {
                letterText.text = WordNormalizer.Normalize(cellDefinition.letter);
            }

            if (background != null)
            {
                _baseColor = background.color;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            PointerDownOnCell?.Invoke(this);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (eventData.dragging)
            {
                PointerEnterOnCell?.Invoke(this);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            PointerUpOnCell?.Invoke(this);
        }

        public void OnSelected()
        {
            if (background != null)
            {
                background.color = new Color(0.85f, 0.95f, 1f, 1f);
                transform.localScale = Vector3.one * 1.05f;
            }
        }

        public void OnPathAccepted()
        {
            if (background != null)
            {
                background.color = new Color(0.75f, 1f, 0.75f, 1f);
            }
        }

        public void OnPathRejected()
        {
            if (background != null)
            {
                background.color = new Color(1f, 0.8f, 0.8f, 1f);
            }
        }

        public void ResetFx()
        {
            transform.localScale = Vector3.one;
            if (background != null)
            {
                background.color = _baseColor;
            }
        }
    }
}
