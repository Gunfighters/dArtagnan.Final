using Audio;
using Lobby.LobbyAlertMessage;
using R3;
using UnityEngine;

namespace Lobby.SettingsPopup
{
    public static class SettingsPopupPresenter
    {
        public static void Initialize(SettingsPopupModel model, SettingsPopupView view)
        {
            view.bgmVolumeSlider.value = model.settingsSo.bgmVolume.CurrentValue;
            view.fxVolumeSlider.value = model.settingsSo.fxVolume.CurrentValue;
            view.hapticToggle.isOn = model.settingsSo.haptic.CurrentValue;
            model.settingsSo.bgmVolume.Select(v => v > 0)
                .Select(on => on ? view.bgmVolumeOnSprite : view.bgmVolumeOffSprite)
                .Subscribe(sprite => view.bgmVolumeImage.sprite = sprite);
            model.settingsSo.fxVolume.Select(v => v > 0)
                .Select(on => on ? view.fxVolumeOnSprite : view.fxVolumeOffSprite)
                .Subscribe(sprite => view.fxVolumeImage.sprite = sprite);
            model.settingsSo.haptic
                .Select(on => on ? view.hapticToggleOnSprite : view.hapticToggleOffSprite)
                .Subscribe(sprite => view.hapticToggleImage.sprite = sprite);
            view.fxVolumeSlider
                .OnValueChangedAsObservable()
                .Subscribe(newVal => model.settingsSo.fxVolume.Value = newVal);
            view.bgmVolumeSlider
                .OnValueChangedAsObservable()
                .Subscribe(newVal => model.settingsSo.bgmVolume.Value = newVal);
            view.hapticToggle
                .OnValueChangedAsObservable()
                .Subscribe(newVal => model.settingsSo.haptic.Value = newVal);
            view.closeButton
                .OnClickAsObservable()
                .Subscribe(_ =>
                {
                    AudioClipPlayer.Instance.Play(AudioClipType.ButtonClicked);
                    view.gameObject.SetActive(false);
                });
            view.probabilityLookupButton
                .OnClickAsObservable()
                .Subscribe(_ => Application.OpenURL("https://trulybright.github.io/posts/달타냥-확률-일람/"));
            // view.languageButton
            //     .OnClickAsObservable()
            //     .Subscribe(_ =>
            //     {
            //         AudioClipPlayer.Instance.Play(AudioClipType.ButtonClicked);
            //         LobbyAlertMessageModel.Instance.Text.OnNext("준비 중입니다!");
            //     });
            // view.rateButton
            //     .OnClickAsObservable()
            //     .Subscribe(_ =>
            //     {
            //         AudioClipPlayer.Instance.Play(AudioClipType.ButtonClicked);
            //         LobbyAlertMessageModel.Instance.Text.OnNext("준비 중입니다!");
            //     });
            view.tutorialButton
                .OnClickAsObservable()
                .Subscribe(_ =>
                {
                    AudioClipPlayer.Instance.Play(AudioClipType.ButtonClicked);
                    LobbySceneScreenManager.Instance.currentScreen.Value = LobbyCanvasScreenType.Tutorial;
                });
            view.quitButton
                .OnClickAsObservable()
                .Subscribe(_ =>
                {
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
                });
            view.creditButton
                .OnClickAsObservable()
                .Subscribe(_ =>
                {
                    AudioClipPlayer.Instance.Play(AudioClipType.ButtonClicked);
                    view.creditPopup.gameObject.SetActive(true);
                });
            view.changeNicknameButton
                .OnClickAsObservable()
                .Subscribe(_ =>
                {
                    AudioClipPlayer.Instance.Play(AudioClipType.ButtonClicked);
                    LobbySceneScreenManager.Instance.currentScreen.Value = LobbyCanvasScreenType.UsernameSetup;
                });
        }
    }
}