using System;
using System.Linq;
using Assets.HeroEditor4D.Common.Scripts.EditorScripts;
using Assets.HeroEditor4D.Common.Scripts.Enums;
using Audio;
using Cysharp.Threading.Tasks;
using Networking;
using R3;
using R3.Triggers;
using TMPro;

namespace Lobby.Dressroom
{
    public static class ShopPresenter
    {
        public static void Initialize(ShopModel model, ShopView view)
        {
            view.rouletteOnceScreen.OnEnableAsObservable().Subscribe(_ =>
            {
                view.silverBar.gameObject.SetActive(false);
                view.goldbar.gameObject.SetActive(false);
            });
            view.rouletteOnceScreen.OnDisableAsObservable().Subscribe(_ =>
            {
                view.silverBar.gameObject.SetActive(true);
                view.goldbar.gameObject.SetActive(true);
            });
            ShopModel.Gold.Subscribe(g => view.gold.text = $"{g:N0}").AddTo(view);
            ShopModel.Silver.Subscribe(s => view.silver.text = $"{s:N0}").AddTo(view);
            ShopModel.Gold
                .CombineLatest(model.oneTimeRouletteGoldPrice, (has, needs) => has >= needs)
                .Subscribe(yes => view.buyRouletteOnceBanknote.interactable = yes);
            ShopModel.Crystal
                .CombineLatest(model.oneTimeRouletteCrystalPrice, (has, needs) => has >= needs)
                .Subscribe(yes => view.buyRouletteOnceCrystal.interactable = yes);
            model.oneTimeRouletteGoldPrice.Subscribe(needs => view.buyRouletteOnceBanknote.GetComponentInChildren<TextMeshProUGUI>().text = $"{needs:N0}").AddTo(view);
            model.oneTimeRouletteCrystalPrice.Subscribe(needs => view.buyRouletteOnceCrystal.GetComponentInChildren<TextMeshProUGUI>().text = $"{needs:N0}").AddTo(view);
            view.closeShopButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    LobbySceneScreenManager.Instance.currentScreen.Value = LobbyCanvasScreenType.Lobby;
                    AudioClipPlayer.Instance.Play(AudioClipType.ButtonClicked);
                }).AddTo(model);
            view.homeButton.OnClickAsObservable().Subscribe(_ =>
            {
                LobbySceneScreenManager.Instance.currentScreen.Value = LobbyCanvasScreenType.Lobby;
                AudioClipPlayer.Instance.Play(AudioClipType.ButtonClicked);
            }).AddTo(model);
            WebsocketManager.Instance.onShopBuyParRouletteResponse
                .WhereNotNull()
                .Where(resp => resp.ok)
                .Subscribe(resp =>
                {
                    view.rouletteOnceScreen.gameObject.SetActive(true);
                    view.rouletteOnceScreen.Initialize(resp);
                });
            view.buyRouletteOnceBanknote.OnClickAsObservable().Subscribe(_ =>
            {
                if (Enum.GetNames(typeof(EquipmentPart)).Contains(CharacterEditor.Instance.SelectedTabPartString.CurrentValue))
                    WebsocketManager.Instance.PurchaseCostumeRoulette(Enum.Parse<EquipmentPart>(CharacterEditor.Instance.SelectedTabPartString.CurrentValue), "gold").Forget();
                if (Enum.GetNames(typeof(BodyPart)).Contains(CharacterEditor.Instance.SelectedTabPartString.CurrentValue))
                    WebsocketManager.Instance.PurchaseCostumeRoulette(Enum.Parse<BodyPart>(CharacterEditor.Instance.SelectedTabPartString.CurrentValue), "gold").Forget();
            }).AddTo(model);
            view.buyRouletteOnceCrystal.OnClickAsObservable().Subscribe(_ =>
            {
                if (Enum.GetNames(typeof(EquipmentPart)).Contains(CharacterEditor.Instance.SelectedTabPartString.CurrentValue))
                    WebsocketManager.Instance.PurchaseCostumeRoulette(Enum.Parse<EquipmentPart>(CharacterEditor.Instance.SelectedTabPartString.CurrentValue), "crystal").Forget();
                if (Enum.GetNames(typeof(BodyPart)).Contains(CharacterEditor.Instance.SelectedTabPartString.CurrentValue))
                    WebsocketManager.Instance.PurchaseCostumeRoulette(Enum.Parse<BodyPart>(CharacterEditor.Instance.SelectedTabPartString.CurrentValue), "crystal").Forget();
            }).AddTo(model);
        }
    }
}