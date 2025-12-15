using System.Linq;
using Cysharp.Threading.Tasks;
using dArtagnan.Shared;
using Game;
using Networking;
using R3;
using UI.HUD.Leaderboard;
using UnityEngine;
using UnityEngine.UI;

namespace UI.HUD
{
    public static class HUDPresenter
    {
        public static void Initialize(HUDView view, HUDModel model)
        {
            model.alive.Subscribe(a => view.AliveTags.ForEach(o => o.gameObject.SetActive(a)));
            model.host.Subscribe(a =>
            {
                view.HostTags.ForEach(o => o.gameObject.SetActive(a));
                view.roomNameChangeButton.interactable = a;
            });
            model.dead.Subscribe(a => view.DeadTags.ForEach(o => o.gameObject.SetActive(a)));
            model.waiting.Subscribe(a => view.WaitingTags.ForEach(o => o.gameObject.SetActive(a)));
            model.inRound.Subscribe(a => view.InRoundTags.ForEach(o => o.gameObject.SetActive(a)));
            model.always.Subscribe(a => view.AlwaysTags.ForEach(o => o.gameObject.SetActive(a)));
            model.aliveInRound.Subscribe(a => view.AliveInRoundTags.ForEach(o => o.gameObject.SetActive(a)));
            model.showRibbon.Subscribe(view.ribbon.gameObject.SetActive);
            model.ribbonText.SubscribeToText(view.ribbonText);
            model.ribbonRewardText.SubscribeToText(view.ribbonRewardText);
            // 방제 표시 (RoomName이 변경될 때마다 UI 업데이트)
            GameModel.Instance.RoomName.SubscribeToText(view.roomName);

            // 방제 변경 버튼 클릭 시 팝업 열기
            view.roomNameChangeButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    view.roomNameInputField.text = GameModel.Instance?.RoomName.CurrentValue;
                    view.roomNameChangePopup.gameObject.SetActive(true);
                    view.roomNameConfirmButton.interactable = true;
                    view.roomNameConfirmButtonText.text = "변경";
                });

            // 팝업 닫기 버튼
            view.roomNameChagenPopupCloseButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    view.roomNameChangePopup.gameObject.SetActive(false);
                });

            // 방제 변경 확인 버튼 - TCP로 게임 서버에 요청
            view.roomNameConfirmButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    view.roomNameConfirmButtonText.text = "변경 중...";
                    view.roomNameConfirmButton.interactable = false;
                    PacketChannel.Raise(new dArtagnan.Shared.UpdateRoomNameFromClient
                    {
                        RoomName = view.roomNameInputField.text
                    });
                });
            
            PacketChannel.On<UpdateRoomNameResponse>(e =>
            {
                view.roomNameConfirmButton.interactable = true;
                view.roomNameConfirmButtonText.text = "변경";
                view.roomNameChangeError.gameObject.SetActive(!e.ok);
                view.roomNameChangeError.text = e.errorMessage;
            });

            // 방제가 변경되면 팝업 닫기 (서버에서 UpdateRoomNameBroadcast 받았을 때)
            GameModel.Instance.RoomName
                .Subscribe(_ =>
                {
                    view.roomNameChangePopup.gameObject.SetActive(false);
                });
            model.showTimerForBankrupt.Subscribe(view.timerForBankruptContainer.gameObject.SetActive);
            model.timerForBankruptRatio.Subscribe(r => view.timerForBankrupt.value = r);
            GameModel.Instance.RoundWinners.Select(e => !e.PlayerIds.Any())
                .Subscribe(view.nobodyWonTheRound.gameObject.SetActive);
            GameModel.Instance.ShowVictory.Subscribe(_ => view.nobodyWonTheRound.gameObject.SetActive(false));
            GameModel.Instance.IsPrivateRoom.Subscribe(yes =>
            {
                view.passwordText.enabled = yes;
                view.passwordText.GetComponent<Button>().interactable = yes;
            });
            model.showPassword
                .CombineLatest(GameModel.Instance.Password, (show, pw) => new { show, pw })
                .Subscribe(ops => view.passwordText.text = ops.show ? $"비밀번호: {ops.pw}" : $"비밀번호: 눌러서 보기");
            view.passwordText.GetComponent<Button>()
                .OnClickAsObservable()
                .Subscribe(_ => model.showPassword.Value = !model.showPassword.Value);
            model.showLeaderboard.Subscribe(yes => view.leaderBoardManager.gameObject.SetActive(yes));
        }
    }
}