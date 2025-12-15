using System.Linq;
using Audio;
using dArtagnan.Shared;
using Game;
using ObservableCollections;
using R3;
using UnityEngine;

namespace UI.InitialRoulette
{
    public static class InitialRoulettePresenter
    {
        public static void Initialize(InitialRouletteModel model, InitialRouletteView view)
        {
            var l = model.AccuracyPool.ToList();
            for (var i = 0; i < l.Count; i++)
                view.Slots[i].Text.text = $"{l[i]}%";
            model.AccuracyPool
                .ObserveAdd()
                .Subscribe(added =>
                {
                    if (GameModel.Instance.State.CurrentValue == GameState.InitialRoulette)
                        view.Slots[added.Index].Text.text = $"{added.Value}%";
                }).AddTo(view);
            model.Rotation
                .Subscribe(newRotation => view.spin.rotation = Quaternion.Euler(0, 0, newRotation))
                .AddTo(view);
            view.spinButton.OnClickAsObservable().Subscribe(_ =>
            {
                AudioClipPlayer.Instance.Play(AudioClipType.ButtonClicked);
                model.Spin.OnNext(true);
                view.spinButton.interactable = false;
                view.spinButtonGraphics.ForEach(c =>
                {
                    var color = c.color;
                    color.a = 0.5f;
                    c.color = color;
                });
            });
        }
    }
}