using Audio;
using Cysharp.Threading.Tasks;
using dArtagnan.Shared;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Player.Components
{
    public class PlayerFx : MonoBehaviour
    {
        private PlayerModel _model;
        [SerializeField] private ParticleSystem aura;
        [SerializeField] private Image shieldImage;
        [SerializeField] private Image fearBulletImage;
        [SerializeField] private TextMeshProUGUI accuracyText;
        
        private void Awake()
        {
            _model = GetComponent<PlayerModel>();
            var renderer1 = aura.GetComponent<ParticleSystemRenderer>();
            var renderer2 = aura.GetComponentInChildren<ParticleSystemRenderer>();
            _model.Fury.CombineLatest(_model.Alive, (furious, alive) => furious && alive).Subscribe(yes =>
            {
                aura.gameObject.SetActive(yes);
                renderer1.enabled = renderer2.enabled = yes;
            });
            _model.ItemTriggeredOnThisPlayer.Subscribe(e =>
            {
                switch (e.ItemId)
                {
                    case ItemId.BodyArmor:
                        BodyArmorFx();
                        break;
                    case ItemId.FearBullet:
                        FearBulletFx();
                        break;
                    case ItemId.GiftBox:
                        if (_model == GameModel.Instance.LocalPlayer.CurrentValue)
                            GameModel.Instance.GiftBoxResult.OnNext(e.Success);
                        break;
                }
            });
        }

        [ContextMenu("Show Body Armor Fx")]
        private void BodyArmorFx()
        {
            shieldImage.enabled = true;
            AudioClipPlayer.Instance.Play(AudioClipType.ShieldTriggered);
            UniTask.WaitForSeconds(0.5f).ContinueWith(() => shieldImage.enabled = false);
        }

        [ContextMenu("Show Fear Bullet Fx")]
        private void FearBulletFx()
        {
            fearBulletImage.enabled = true;
            accuracyText.color = Color.red;
            AudioClipPlayer.Instance.Play(AudioClipType.FearBulletEffect);
            UniTask.WaitForSeconds(1f).ContinueWith(() =>
            {
                fearBulletImage.enabled = false;
                accuracyText.color = Color.white;
            });
        }
    }
}