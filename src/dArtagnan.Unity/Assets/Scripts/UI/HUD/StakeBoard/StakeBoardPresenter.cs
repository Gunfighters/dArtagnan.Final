using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

namespace UI.HUD.StakeBoard
{
    public static class StakeBoardPresenter
    {
        public static void Initialize(StakeBoardModel model, StakeBoardView view)
        {
            model.round.Subscribe(r => view.roundText.text = $"{r}라운드 상금:");
            model.GivePrize.Subscribe(amount => view.RequestAnimation(StakeBoardAnimationType.Award, amount, 0));
            model.AmountChanged
                .Subscribe(delta =>
                {
                    if (delta.Current > delta.Previous)
                        view.RequestAnimation(StakeBoardAnimationType.Increase, delta.Previous, delta.Current);
                });
            model.progressToDeduction
                .Subscribe(progress => view.deductionProgressSlider.value = progress);
            model.deductionUnit
                .Where(u => u > 0)
                .Select(view.monetaryUnitCollection.CalculateUnitsByAmount)
                .Subscribe(unitData =>
                {
                    view.deductionUnitImage.Icon.sprite = unitData[0];
                    view.deductionUnitCount.text = unitData.Count.ToString();
                });
            model.deductionPeriod
                .Subscribe(newPeriod =>
                {
                    view.deductionProgressSlider.minValue = 0;
                    view.deductionProgressSlider.maxValue = newPeriod;
                });
            model.progressToDeduction
                .CombineLatest(model.deductionPeriod, (progress, period) => Mathf.Ceil(period - progress))
                .Subscribe(remainingTime => view.timeUntilNextDeduction.text = $"{remainingTime}초");
            model.progressToDeduction
                .CombineLatest(model.deductionPeriod, (progress, period) => progress / period)
                .Select(ratio => ratio > 0.7f)
                .Subscribe(emergency =>
                {
                    view.deductionProgressSliderFill.sprite =
                        emergency ? view.emergencyProgressSliderFill : view.normalProgressSliderFill;
                    view.deductionUnitImage.toBeDeducted.Value = emergency;
                });
        }
    }
}