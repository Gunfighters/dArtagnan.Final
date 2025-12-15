using System.Collections.Generic;
using TMPro;
using UI.HUD.Leaderboard;
using UnityEngine;
using UnityEngine.UI;

namespace UI.HUD
{
    public class HUDView : MonoBehaviour
    {
        public readonly List<GameObject> AliveTags = new();
        public readonly List<GameObject> DeadTags = new();
        public readonly List<GameObject> HostTags = new();
        public readonly List<GameObject> AlwaysTags = new();
        public readonly List<GameObject> WaitingTags = new();
        public readonly List<GameObject> InRoundTags = new();
        public readonly List<GameObject> AliveInRoundTags = new();

        [Header("Room Name")]
        public TextMeshProUGUI roomName;
        public Button roomNameChangeButton;
        public Transform roomNameChangePopup;
        public Button roomNameChagenPopupCloseButton; 
        public TMP_InputField roomNameInputField;
        public Button roomNameConfirmButton;
        public TextMeshProUGUI roomNameConfirmButtonText;
        public TextMeshProUGUI roomNameChangeError;

        [Header("Ribbon")]
        public Transform ribbon;
        public TextMeshProUGUI ribbonText;
        public TextMeshProUGUI ribbonRewardText;

        [Header("Timer for the Bankrupts")]
        public Transform timerForBankruptContainer;
        public Slider timerForBankrupt;

        [Header("Misc")]
        public RectTransform nobodyWonTheRound;
        public LeaderboardManager leaderBoardManager;

        [Header("UI")]
        public TextMeshProUGUI passwordText;
        private void Awake()
        {
            foreach (Transform child in transform)
            {
                if (child.CompareTag("Alive"))
                    AliveTags.Add(child.gameObject);
                else if (child.CompareTag("Dead"))
                    DeadTags.Add(child.gameObject);
                else if (child.CompareTag("Host"))
                    HostTags.Add(child.gameObject);
                else if (child.CompareTag("Always"))
                    AlwaysTags.Add(child.gameObject);
                else if (child.CompareTag("Waiting"))
                    WaitingTags.Add(child.gameObject);
                else if (child.CompareTag("InRound"))
                    InRoundTags.Add(child.gameObject);
                else  if (child.CompareTag("AliveInRound"))
                    AliveInRoundTags.Add(child.gameObject);
                else
                    Debug.LogWarning($"Unknown tag { child.tag }");
            }
            nobodyWonTheRound.gameObject.SetActive(false);
        }

        private void Start()
        {
            HUDPresenter.Initialize(this, GetComponent<HUDModel>());
        }
    }
}