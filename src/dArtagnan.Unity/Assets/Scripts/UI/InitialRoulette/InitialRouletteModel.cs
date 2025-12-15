using System.Linq;
using Cysharp.Threading.Tasks;
using dArtagnan.Shared;
using Game;
using ObservableCollections;
using R3;
using UnityEngine;
using Random = UnityEngine.Random;

namespace UI.InitialRoulette
{
    public class InitialRouletteModel : MonoBehaviour
    {
        public int SpinDuration;
        public int SpinSpeed;
        public IReadOnlyObservableList<int> AccuracyPool => GameModel.Instance.AccuracyPool;
        public ReadOnlyReactiveProperty<int> TargetAccuracy => GameModel.Instance.TargetAccuracy;
        public SerializableReactiveProperty<float> Rotation;
        public SerializableReactiveProperty<bool> Spin;

        private void Start()
        {
            Spin.Subscribe(isSpinning =>
            {
                if (isSpinning)
                    StartSpin(TargetAccuracy.CurrentValue).Forget();
                else
                    Rotation.Value = 0;
            });
        }

        private async UniTask StartSpin(int targetAccuracy)
        {
            var targetIndex = AccuracyPool.ToList().IndexOf(targetAccuracy);
            var slotAngle = 360f / AccuracyPool.Count;
            var halfSlotAngle = slotAngle / 2;
            var halfSlotAngleWithPaddings = halfSlotAngle * 0.97f;
            var targetAngle = slotAngle * targetIndex;
            var leftOffset = targetAngle - halfSlotAngleWithPaddings;
            var rightOffset = targetAngle + halfSlotAngleWithPaddings;
            var randomAngle = Random.Range(leftOffset, rightOffset);
            var finalAngle = randomAngle + 360 * SpinDuration * SpinSpeed;
            var elapsed = 0f;
            while (elapsed / SpinDuration < 1)
            {
                elapsed += Time.deltaTime;
                Rotation.Value = Mathf.Lerp(0, finalAngle, SpinRotation(elapsed / SpinDuration));
                await UniTask.WaitForEndOfFrame();
            }
            await UniTask.WaitForSeconds(0.5f);
            PacketChannel.Raise(new InitialRouletteDoneFromClient());
        }

        private static float SpinRotation(float x) => -Mathf.Pow(x - 1, 4) + 1;
    }
}