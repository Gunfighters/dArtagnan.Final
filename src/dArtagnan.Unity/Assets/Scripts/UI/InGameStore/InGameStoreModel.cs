using System.Linq;
using Cysharp.Threading.Tasks;
using dArtagnan.Shared;
using Game;
using ObservableCollections;
using R3;
using UnityEngine;
using Random = UnityEngine.Random;

namespace UI.InGameStore
{
    public class InGameStoreModel : MonoBehaviour
    {
        public IReadOnlyObservableList<int> AccuracyPool => GameModel.Instance.AccuracyPool;
        public ReadOnlyReactiveProperty<int> AccuracyResetCost => GameModel.Instance.AccuracyResetCost;
        public IReadOnlyObservableList<ShopItem> Items => GameModel.Instance.ItemPool;
        public ReadOnlyReactiveProperty<int> TargetAccuracy => GameModel.Instance.TargetAccuracy;
        public SerializableReactiveProperty<int> CurrentBalance => GameModel.Instance.LocalPlayer.CurrentValue.Balance;
        public SerializableReactiveProperty<int> selectedItemIndex;
        public int SpinDuration;
        public int SpinSpeed;
        public SerializableReactiveProperty<float> Rotation;
        public SerializableReactiveProperty<bool> Spin;
        public ReadOnlyReactiveProperty<float> RemainingTime => GameModel.Instance.RemainingShopTime;
        public ReadOnlyReactiveProperty<float> MaxShopTime => GameModel.Instance.MaxShopTime;
        public ReadOnlyReactiveProperty<ItemId[]> OwnedItems => GameModel.Instance.LocalPlayer.CurrentValue.OwnedItems;
        private void Start()
        {
            Spin.Subscribe(nowSpinning =>
            {
                if (nowSpinning)
                    StartSpin(TargetAccuracy.CurrentValue).Forget();
            });
            GameModel.Instance.SpinShopRoulette.Subscribe(_ => Spin.Value = true);
        }
        
        private async UniTask StartSpin(int targetAccuracy)
        {
            var targetIndex = AccuracyPool.ToList().IndexOf(targetAccuracy);
            var slotAngle = 360f / AccuracyPool.Count;
            var halfSlotAngle = slotAngle / 2;
            var halfSlotAngleWithPaddings = halfSlotAngle * 0.97f;
            var targetAngle = slotAngle * targetIndex;
            var leftOffset = (targetAngle - halfSlotAngleWithPaddings) % 360;
            var rightOffset = (targetAngle + halfSlotAngleWithPaddings) % 360;
            var randomAngle = Random.Range(leftOffset, rightOffset);
            var finalAngle = randomAngle + 360 * SpinSpeed * SpinDuration;
            var elapsed = 0f;
            while (elapsed / SpinDuration < 1 && Spin.CurrentValue)
            {
                elapsed += Time.deltaTime;
                Rotation.Value = Mathf.Lerp(0, finalAngle, SpinRotation(elapsed / SpinDuration));
                await UniTask.WaitForEndOfFrame();
            }
            Spin.Value = false;
        }
        private static float SpinRotation(float x) => -Mathf.Pow(x - 1, 4) + 1;
    }
}