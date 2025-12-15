using Cysharp.Threading.Tasks;
using Game.Player.Data;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Player.Components
{
    public class PlayerShoot : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer targetHighlightCircle;
        [SerializeField] private TextMeshProUGUI hitMissText;
        [SerializeField] private Color hitTextColor;
        [SerializeField] private Color missTextColor;
        [SerializeField] private float hitMissShowingDuration;
        public SerializableReactiveProperty<bool> hideRange;
        public Image rangeCircle;
        public Image rangeCircleBackground;
        private PlayerModel _model;
        // public Image reloadingImage;
        // public Image targetImage;
        public Slider reloadingSlider;
        public Color rangeCircleReloadingColor;
        public Color rangeCircleLoadedColor;
        public Color rangeCircleAttackableColor;

        private void Awake()
        {
            hitMissText.enabled = false;
            var model = GetComponent<PlayerModel>();
            _model = model;
            model.Range.Subscribe(SetRange);
            model.Fire.Subscribe(ShowHitOrMiss);
            model.Highlighted.Subscribe(HighlightAsTarget);
            // model.TotalReloadingTime.Subscribe(newTotal => reloadingSlider.maxValue = newTotal);
            model.RemainingReloadingTime
                .CombineLatest(_model.TotalReloadingTime, (r, t) => 1 - r / t)
                .Subscribe(progress =>
                {
                    reloadingSlider.value = progress;
                    // rangeCircle.fillAmount = progress;
                    // rangeCircleBackground.enabled = progress < 1;
                });
            model.RemainingReloadingTime.Subscribe(r => reloadingSlider.gameObject.SetActive(r > 0));
            // model.RemainingReloadingTime.Select(time => time > 0).Subscribe(reloading => reloadingImage.enabled = reloading);
            // model.RemainingReloadingTime.Select(time => time <= 0).Subscribe(loaded => targetImage.enabled = loaded);
            model.RemainingReloadingTime.CombineLatest(model.TotalReloadingTime, (r, t) => 1 - r / t).Subscribe(ratio =>
            {
                if (ratio < 1)
                    rangeCircle.color = rangeCircleReloadingColor;
                else if (model.CalculateTarget(Vector2.zero) is null)
                    rangeCircle.color = rangeCircleLoadedColor;
                else
                    rangeCircle.color = rangeCircleAttackableColor;
            });
            model.HideAccuracyAndRange
                .CombineLatest(GameModel.Instance.LocalPlayer.Select(local => local == model), (hide, isLocal) => hide && !isLocal)
                .Subscribe(hide =>
                {
                    rangeCircle.gameObject.SetActive(!hide);
                    // reloadingImage.gameObject.SetActive(!hide);
                    // targetImage.gameObject.SetActive(!hide);
                });
        }

        private void Update()
        {
            _model.RemainingReloadingTime.Value = Mathf.Max(0, _model.RemainingReloadingTime.Value - Time.deltaTime);
        }

        private void SetRange(float newRange)
        {
            rangeCircle.transform.localScale = new Vector3(newRange, newRange, 1);
        }

        private void HighlightAsTarget(bool show)
        {
            targetHighlightCircle.enabled = show;
        }

        private void ShowHitOrMiss(FireInfo info)
        {
            hitMissText.text = info.Hit ? "HIT!" : "MISS!";
            hitMissText.color = info.Hit ? hitTextColor : missTextColor;
            hitMissText.fontSize = info.Hit ? 0.6f : 0.4f;
            hitMissText.enabled = true;
            HideHitMissFx().Forget();
        }

        private async UniTask HideHitMissFx()
        {
            await UniTask.WaitForSeconds(1);
            hitMissText.enabled = false;
        }
    }
}