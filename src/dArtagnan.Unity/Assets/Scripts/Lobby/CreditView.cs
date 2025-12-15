using UnityEngine;
using UnityEngine.UI;

namespace Lobby
{
    public class CreditView : MonoBehaviour
    {
        public Button closeButton;

        private void Awake()
        {
            closeButton.onClick.AddListener(() => gameObject.SetActive(false));
        }
    }
}