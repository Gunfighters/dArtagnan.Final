using System;
using System.Linq;
using System.Threading;
using Assets.HeroEditor4D.Common.Scripts.Collections;
using Assets.HeroEditor4D.Common.Scripts.Data;
using Assets.HeroEditor4D.Common.Scripts.EditorScripts;
using Audio;
using Cysharp.Threading.Tasks;
using dArtagnan.Shared;
using Networking;
using Roulette;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utils;

namespace Lobby.Dressroom
{
    public class OneTimePartRouletteManager : MonoBehaviour
    {
        public CharacterEditor characterEditor;
        public RouletteManager rouletteManager;
        public PartRouletteItem itemPrefab;
        public ItemSprite[] itemSprites;
        public ItemSprite target;
        public float rotateByOneUnitDuration;
        public Button closeButton;
        public SpriteCollection spriteCollection;
        public IconCollection iconCollection;
        public Button rewardScreen;
        public Image rewardImage;
        public TextMeshProUGUI rewardText;
        public TextMeshProUGUI touchAnywhereToClose;
        public RectTransform compensationTextContainer;
        public TextMeshProUGUI compensationText;
        public TextMeshProUGUI probabilityInformationText;
        private CancellationTokenSource _cts = new();
        public ParticleSystem resultFx;

        private void Awake()
        {
            rouletteManager.button.onClick.AddListener(() =>
            {
                rouletteManager.DisableButtonAndGraphicsInside();
                SpinToTarget().Forget();
            });
            rewardScreen.onClick.AddListener(() => rewardScreen.gameObject.SetActive(false));
            closeButton.onClick.AddListener(() => gameObject.SetActive(false));
        }

        private void OnDisable()
        {
            WebsocketManager.Instance.onShopBuyParRouletteResponse.Value = null;
        }

        public void Initialize(ShopBuyPartRouletteResponse resp)
        {
            rouletteManager.EnableButtonAndGraphicsInside();
            closeButton.gameObject.SetActive(false);
            rewardScreen.gameObject.SetActive(false);
            touchAnywhereToClose.gameObject.SetActive(false);
            rouletteManager.button.gameObject.SetActive(false);
            probabilityInformationText.gameObject.SetActive(false);
            _cts.Cancel();
            _cts = new CancellationTokenSource();
            SetupPlaceholders(characterEditor.SelectedTabIcon.CurrentValue);
            rouletteManager.ResetRotation();
            itemSprites = resp.roulettePool
                .Select(item => spriteCollection.AllSprites.First(i => i.Id == item))
                .ToArray();
            target = spriteCollection.AllSprites.First(i => i.Id == resp.wonCostume);
            rewardImage.sprite = iconCollection.GetIcon(characterEditor.GetIconId(target));
            rewardText.text = $"<{ItemNameTranslatorData.Translate(characterEditor.SelectedTabPartString.CurrentValue, target.Name)}> 아이템 획득!!";
            var tiers = WebsocketManager.Instance.ShopConstantsDict.CurrentValue[CharacterEditor.Instance.SelectedTabPartString.CurrentValue].TIERS;
            var main = resultFx.main;
            if (tiers["COMMON"].Contains(target.Id))
            {
                resultFx.gameObject.SetActive(true);
                main.startColor = new Color(0.1f, 0.6f, 0.2f);
            }
            else if (tiers["RARE"].Contains(target.Id))
            {
                resultFx.gameObject.SetActive(true);
                main.startColor = new Color(0.3f, 0.6f, 1f);
            }
            else if (tiers["EPIC"].Contains(target.Id))
            {
                resultFx.gameObject.SetActive(true);
                main.startColor = new Color(0.7f, 0.37f, 1f);
            }
            else if (tiers["LEGENDARY"].Contains(target.Id))
            {
                resultFx.gameObject.SetActive(true);
                main.startColor = Color.yellow;
            }
            compensationTextContainer.gameObject.SetActive(resp.isDuplicate);
            compensationText.text = resp.silverGained.ToString("N0");
            FillItems().ContinueWith(() =>
            {
                rouletteManager.button.gameObject.SetActive(true);
                probabilityInformationText.gameObject.SetActive(true);
            });
        }

        private async UniTask FillItems()
        {
            await UniTask.WaitForSeconds(rotateByOneUnitDuration);
            for (var i = 0; i < rouletteManager.Slots.Length; i++)
            {
                var slot = rouletteManager.GetSlotAtIndex<PartRouletteItem>(i);
                SetupActualItem(slot, itemSprites[i], i);
                await UniTask.WaitForSeconds(rotateByOneUnitDuration);
                await rouletteManager.RotateByOneUnit(rotateByOneUnitDuration);
            }
        }

        private async UniTask SpinToTarget()
        {
            await rouletteManager.SpinToValue<PartRouletteItem>(item => item.actualItem.sprite == iconCollection.GetIcon(characterEditor.GetIconId(target)));
            // closeButton.gameObject.SetActive(true);
            rewardScreen.gameObject.SetActive(true);
            AudioClipPlayer.Instance.Play(AudioClipType.GachaResult);
            rouletteManager.button.gameObject.SetActive(true);
            await UniTask.WaitForSeconds(3, cancellationToken: _cts.Token);
            if (!_cts.Token.IsCancellationRequested)
                touchAnywhereToClose.gameObject.SetActive(true);
        }

        private void SetupPlaceholders(Sprite sprite)
        {
            var data = new Sprite[rouletteManager.Slots.Length];
            Array.Fill(data, sprite);
            rouletteManager.CreateSlots(itemPrefab, data, SetupPlaceholder);
        }

        private static void SetupPlaceholder(PartRouletteItem rouletteItem, Sprite sprite, int index)
        {
            rouletteItem.actualItem.enabled = false;
            rouletteItem.silhouette.enabled = true;
            rouletteItem.silhouette.sprite = sprite;
            rouletteItem.fx.gameObject.SetActive(false);
        }

        private void SetupActualItem(PartRouletteItem rouletteItem, ItemSprite itemSprite, int index)
        {
            rouletteItem.silhouette.enabled = false;
            rouletteItem.actualItem.enabled = true;
            rouletteItem.actualItem.sprite = iconCollection.GetIcon(characterEditor.GetIconId(itemSprite));
            var tiers = WebsocketManager.Instance.ShopConstantsDict.CurrentValue[CharacterEditor.Instance.SelectedTabPartString.CurrentValue].TIERS;
            if (tiers["COMMON"].Contains(itemSprite.Id))
            {
                rouletteItem.fx.gameObject.SetActive(false);
            }
            else if (tiers["RARE"].Contains(itemSprite.Id))
            {
                rouletteItem.fx.gameObject.SetActive(true);
                rouletteItem.fx.color = new Color(0.3f, 0.6f, 1f);
            }
            else if (tiers["EPIC"].Contains(itemSprite.Id))
            {
                rouletteItem.fx.gameObject.SetActive(true);
                rouletteItem.fx.color = new Color(0.7f, 0.37f, 1f);
            }
            else if (tiers["LEGENDARY"].Contains(itemSprite.Id))
            {
                rouletteItem.fx.gameObject.SetActive(true);
                rouletteItem.fx.color = Color.yellow;
            }
            else throw new NotImplementedException(itemSprite.Id);
        }
    }
}