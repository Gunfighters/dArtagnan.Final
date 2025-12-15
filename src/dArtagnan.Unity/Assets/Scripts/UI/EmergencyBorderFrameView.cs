using dArtagnan.Shared;
using Game;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class EmergencyBorderFrameView : MonoBehaviour
    {
        private Image _frame;
        private float _elapsedTime;

        private void Awake()
        {
            _frame = GetComponent<Image>();
        }

        private void Start()
        {
            GameModel.Instance.LocalPlayer
                .WhereNotNull()
                .Select(local => local.Fury)
                .Subscribe(observableFury => observableFury
                    .CombineLatest(GameModel.Instance.LocalPlayer.CurrentValue.Alive, (isFurious, isAlive) => isFurious && isAlive)
                    .CombineLatest(GameModel.Instance.State.Select(s => s == GameState.Round), (inRound, isReady) => inRound && isReady)
                    .Subscribe(show => _frame.enabled = show));
        }

        private void OnEnable()
        {
            _elapsedTime = 0;
            var color = _frame.color;
            color.a = 1f;
            _frame.color = color;
        }

        private void Update()
        {
            _elapsedTime += Time.deltaTime;
            var a = Mathf.Cos(_elapsedTime / Mathf.PI * 12) / 4 + 0.875f;
            var color = _frame.color;
            color.a = a;
            _frame.color = color;
        }
    }
}