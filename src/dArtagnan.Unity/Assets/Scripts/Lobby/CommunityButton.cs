using UnityEngine;
using UnityEngine.UI;

namespace Lobby
{
    public class CommunityButton : MonoBehaviour
    {
        private Button _btn;
        public string href;

        private void Awake()
        {
            _btn = GetComponent<Button>();
            _btn.onClick.AddListener(() => Application.OpenURL(href));
        }
    }
}