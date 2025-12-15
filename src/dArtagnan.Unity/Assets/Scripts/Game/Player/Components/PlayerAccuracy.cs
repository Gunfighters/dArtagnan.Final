using System.Linq;
using dArtagnan.Shared;
using ObservableCollections;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Player.Components
{
    public class PlayerAccuracy : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI accuracyText;
        // [SerializeField] private Image accuracyStateIcon;
        [SerializeField] private Sprite upIcon;
        [SerializeField] private Sprite downIcon;
        [SerializeField] private Color upColor;
        [SerializeField] private Color downColor;
        [SerializeField] private Color colorWhileHiding;

        private void Awake()
        {
            var model = GetComponent<PlayerModel>();
            model.Accuracy.Subscribe(SetAccuracy);
            // model.AccuracyState.Subscribe(SetAccuracyState);
            // model.HideAccuracyAndRange.Subscribe(hide => accuracyStateIcon.gameObject.SetActive(!hide));
            model.OwnedItems.Select(arr => arr.Contains(ItemId.HideAccuracy))
                .CombineLatest(GameModel.Instance.LocalPlayer.Select(local => local == model),
                    (hiding, isLocal) => new { hiding, isLocal })
                .Subscribe(v =>
                {
                    if (!v.hiding)
                    {
                        accuracyText.color = Color.white;
                        SetAccuracy(model.Accuracy.CurrentValue);
                    }
                    else if (v.isLocal)
                        accuracyText.color = colorWhileHiding;
                    else
                        accuracyText.text = $"???%";
                });
        }

        private void SetAccuracy(int newAccuracy)
        {
            accuracyText.text = $"{newAccuracy}%";
        }

        // private void SetAccuracyState(int newAccuracyState)
        // {
        //     accuracyStateIcon.enabled = newAccuracyState != 0;
        //     switch (newAccuracyState)
        //     {
        //         case 1:
        //             accuracyStateIcon.sprite = upIcon;
        //             accuracyStateIcon.color = upColor;
        //             break;
        //         case -1:
        //             accuracyStateIcon.sprite = downIcon;
        //             accuracyStateIcon.color = downColor;
        //             break;
        //     }
        // }
    }
}