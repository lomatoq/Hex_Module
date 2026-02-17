using UnityEngine;

namespace HexWords.UI
{
    public class LevelCompleteView : MonoBehaviour
    {
        [SerializeField] private GameObject root;

        public void SetVisible(bool isVisible)
        {
            if (root != null)
            {
                root.SetActive(isVisible);
            }
            else
            {
                gameObject.SetActive(isVisible);
            }
        }
    }
}
