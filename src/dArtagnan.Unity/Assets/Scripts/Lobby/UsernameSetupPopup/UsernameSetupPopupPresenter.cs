using Audio;
using Cysharp.Threading.Tasks;
using Networking;
using R3;
using TMPro;
using UnityEngine;

namespace Lobby.UsernameSetupPopup
{
    public static class UsernameSetupPopupPresenter
    {
        public static void Initialize(UsernameSetupPopupModel model, UsernameSetupPopupView view)
        {
            WebsocketManager.Instance.isNewUser.Subscribe(yes => view.closeButton.gameObject.SetActive(!yes)).AddTo(view);
            view.closeButton.OnClickAsObservable().Subscribe(_ =>
            {
                AudioClipPlayer.Instance.Play(AudioClipType.ButtonClicked);
                LobbySceneScreenManager.Instance.currentScreen.Value = LobbyCanvasScreenType.Lobby;
            });
            model.canSubmitNickname.Subscribe(can => view.confirmButton.interactable = can).AddTo(view);
            view.nicknameInputField.OnValueChangedAsObservable()
                .Subscribe(newValue => model.nicknameInput.Value = newValue).AddTo(model);
            view.privacyPolicyToggle.OnValueChangedAsObservable().Subscribe(on => model.privacyPolicyAgreed.Value = on).AddTo(model);
            model.nicknameInput
                .CombineLatest(model.privacyPolicyAgreed, (name, agreed) => model.ValidateNickname(name) && agreed)
                .Subscribe(canSubmit => model.canSubmitNickname.Value = canSubmit).AddTo(model);
            view.confirmButton.OnClickAsObservable().Subscribe(_ =>
            {
                view.confirmButton.interactable = false;
                view.confirmButton.GetComponentInChildren<TextMeshProUGUI>().text = "변경 중...";
                AudioClipPlayer.Instance.Play(AudioClipType.ButtonClicked);
                model.SubmitNickname().Forget();
            }).AddTo(model);
            view.privacyPolicyLinkButton.OnClickAsObservable()
                .Subscribe(_ => Application.OpenURL(model.privacyPolicyLink));
            model.NicknameSetFailureReason.Subscribe(reason =>
            {
                view.confirmButton.interactable = true;
                view.confirmButton.GetComponentInChildren<TextMeshProUGUI>().text = "변경";
                view.errorLog.text = reason;
                view.errorLog.color = Color.red;
            });
        }
    }
}