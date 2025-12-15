using Networking;
using R3;

namespace Lobby.ResourceBar
{
    public static class ResourceBarPresenter
    {
        public static void Initialize(ResourceBarModel model, ResourceBarView view)
        {
            WebsocketManager.Instance.crystal.Subscribe(c => view.crystalText.text = c.ToString("N0"));
            WebsocketManager.Instance.gold.Subscribe(g => view.goldText.text = g.ToString("N0"));
            WebsocketManager.Instance.silver.Subscribe(s => view.silverText.text = s.ToString("N0"));
            view.addGoldButton.onClick.AddListener(() => LobbySceneScreenManager.Instance.currentScreen.Value = LobbyCanvasScreenType.Shop);
            view.addCrystalButton.onClick.AddListener(() => LobbySceneScreenManager.Instance.currentScreen.Value = LobbyCanvasScreenType.Shop);
            view.showBanknoteDescriptionBtn.onClick.AddListener(() => view.banknoteDescription.gameObject.SetActive(true));
            view.showCoinDescriptionBtn.onClick.AddListener(() => view.coinDescription.gameObject.SetActive(true));
        }
    }
}