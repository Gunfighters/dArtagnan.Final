using System.Linq;
using Assets.HeroEditor4D.Common.Scripts.EditorScripts;
using Cysharp.Threading.Tasks;
using Networking;
using R3;
using UnityEngine;

namespace Lobby.Dressroom
{
    public class ShopModel : MonoBehaviour
    {
        public int spinDuration;
        public int spinSpeed;
        public static ReadOnlyReactiveProperty<int> Gold => WebsocketManager.Instance.gold;
        public static ReadOnlyReactiveProperty<int> Silver => WebsocketManager.Instance.silver;
        public static ReadOnlyReactiveProperty<int> Crystal => WebsocketManager.Instance.crystal;
        public SerializableReactiveProperty<int> oneTimeRouletteGoldPrice;
        public SerializableReactiveProperty<int> oneTimeRouletteCrystalPrice;
        public SerializableReactiveProperty<int[]> costumeRoulettePool;
        public SerializableReactiveProperty<int> targetCostume;
        public SerializableReactiveProperty<float> rotation;
        public SerializableReactiveProperty<bool> spin;
        public SerializableReactiveProperty<bool> showCostumeRoulette;
        public SerializableReactiveProperty<bool> showGoToClosetButton;

        private void Awake()
        {
            spin.Subscribe(isSpinning =>
            {
                if (isSpinning)
                    StartSpin().Forget();
                else
                    rotation.Value = 0;
            });
        }

        private void Start()
        {
            WebsocketManager.Instance.ShopConstantsDict
                .WhereNotNull()
                .CombineLatest(CharacterEditor.Instance.SelectedTabPartString, (d, a) => d[a])
                .Subscribe(c =>
                {
                    oneTimeRouletteGoldPrice.Value = c.ROULETTE_GOLD_COST;
                    oneTimeRouletteCrystalPrice.Value = c.ROULETTE_CRYSTAL_COST;
                });
        }

        private async UniTask StartSpin()
        {
            var targetIndex = costumeRoulettePool.CurrentValue.ToList().IndexOf(targetCostume.CurrentValue);
            var slotAngle = 360f / costumeRoulettePool.CurrentValue.Length;
            var halfSlotAngle = slotAngle / 2;
            var halfSlotAngleWithPaddings = halfSlotAngle * 0.97f;
            var targetAngle = slotAngle * targetIndex;
            var leftOffset = targetAngle - halfSlotAngleWithPaddings;
            var rightOffset = targetAngle + halfSlotAngleWithPaddings;
            var randomAngle = Random.Range(leftOffset, rightOffset);
            var finalAngle = randomAngle + 360 * spinDuration * spinSpeed;
            var elapsed = 0f;
            while (elapsed / spinDuration < 1)
            {
                elapsed += Time.deltaTime;
                rotation.Value = Mathf.Lerp(0, finalAngle, SpinRotation(elapsed / spinDuration));
                await UniTask.WaitForEndOfFrame();
            }
            await UniTask.WaitForSeconds(0.5f);
            showGoToClosetButton.Value = true;
        }
        
        private static float SpinRotation(float x) => -Mathf.Pow(x - 1, 4) + 1;
    }
}