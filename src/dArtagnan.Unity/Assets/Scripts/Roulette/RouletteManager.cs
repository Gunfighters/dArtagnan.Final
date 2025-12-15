using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using DG.Tweening;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace Roulette
{
    public class RouletteManager : MonoBehaviour
    {
        public int spinCount = 5;
        [SerializeField] public float spinDuration = 4f;
        [SerializeField] private RectTransform spin;
        public Button button;
        public TextMeshProUGUI buttonText;
        private Graphic[] _graphics;
        private Color[] _graphicsColorOriginal;
        public RouletteSlot[] Slots { get; private set; }
        public RouletteDragHandler DragHandler { get; private set; }

        private void Awake()
        {
            DragHandler = GetComponent<RouletteDragHandler>();
            Slots = spin.GetComponentsInChildren<RouletteSlot>();
            _graphics = button.GetComponentsInChildren<Graphic>(true);
            _graphicsColorOriginal = _graphics.Select(g => g.color).ToArray();
        }

        public void CreateSlots<TD, TC>(TC prefab, TD[] data, Action<TC, TD, int> setupFunction) where TC : Object
        {
            if (data.Length != Slots.Length)
                throw new Exception($"data.Length ({data.Length}) is not equal to Slots.Length ({Slots.Length})");
            
            ClearExistingSlots();
            
            for (var i = 0; i < data.Length; i++)
            {
                var instance = Instantiate(prefab, Slots[i].holder);
                setupFunction?.Invoke(instance, data[i], i);
            }
        }

        private void ClearExistingSlots()
        {
            foreach (var t in Slots)
            {
                if (t.holder.childCount > 0)
                {
                    Destroy(t.holder.GetChild(0).gameObject);
                }
            }
        }

        public async UniTask SpinToValue<T>(Predicate<T> predicate)
        {
            var valuePools = Slots.Select(s => s.holder.GetComponentInChildren<T>()).ToArray();
            var targetIndex = Array.FindIndex(valuePools, predicate);
            if (targetIndex == -1) throw new Exception("no item in the pool matches the predicate.");
            await SpinToSlot(targetIndex);
        }

        public async UniTask SpinToSlot(int targetIndex)
        {
            if (targetIndex < 0 || targetIndex >= Slots.Length)
                throw new IndexOutOfRangeException($"Index {targetIndex} out of range. Slots.Length={Slots.Length}");

            var anglePerSlot = 360f / Slots.Length;
            var halfAnglePerSlot = anglePerSlot / 2;
            var halfAnglePerSlotWithPaddings = halfAnglePerSlot * 0.97f;
            var targetAngle = targetIndex * anglePerSlot + UnityEngine.Random.Range(-halfAnglePerSlotWithPaddings, halfAnglePerSlotWithPaddings);
            var randomSpins = spinCount * 360;
            var totalRotation = randomSpins + targetAngle;
            
            await spin
                .DORotate(new Vector3(0, 0, totalRotation), spinDuration, RotateMode.FastBeyond360)
                .SetEase(Ease.OutQuart)
                .AsyncWaitForCompletion()
                .AsUniTask();
        }

        public void DisableButtonAndGraphicsInside()
        {
            button.interactable = false;
            foreach (var g in _graphics)
            {
                g.color = Color.Lerp(g.color, Color.clear, 0.5f);
            }
        }

        public void EnableButtonAndGraphicsInside()
        {
            button.interactable = true;
            for (var i = 0; i < _graphics.Length; i++)
            {
                _graphics[i].color = _graphicsColorOriginal[i];
            }
        }

        public async UniTask RotateByOneUnit(float duration)
        {
            var delta = 360f / Slots.Length;
            await spin.DORotate(new Vector3(0, 0, spin.rotation.eulerAngles.z + delta), duration).AsyncWaitForCompletion().AsUniTask();
        }

        public T GetSlotAtIndex<T>(int index)
        {
            return Slots[index].holder.GetComponentInChildren<T>();
        }

        public void ResetRotation()
        {
            spin.rotation = Quaternion.identity;
        }
    }
}