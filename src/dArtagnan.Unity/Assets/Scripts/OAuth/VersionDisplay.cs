using TMPro;
using UnityEngine;

namespace OAuth
{
    public class VersionDisplay : MonoBehaviour
    {
        private TextMeshProUGUI _versionText;

        private void Awake()
        {
            _versionText = GetComponent<TextMeshProUGUI>();
            _versionText.text = $"{Application.version}";
        }
    }
}