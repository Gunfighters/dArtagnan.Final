using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using R3.Triggers;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Unit
{
    [RequireComponent(typeof(Image))]
    [RequireComponent(typeof(LayoutElement))]
    [RequireComponent(typeof(RectTransform))]
    public class BalanceUnit : MonoBehaviour
    {
        public Image Icon;
        public LayoutElement LayoutElement;
        public RectTransform RectTransform;
        public SerializableReactiveProperty<bool> toBeDeducted;

        private void Awake()
        {
            toBeDeducted.Subscribe(yes => Icon.color  = yes ?  Color.Lerp(Color.red, Color.white, 0.5f) : Color.white);
            toBeDeducted.Subscribe(yes =>
            {
                var elapsed = 0f;
                if (yes)
                    this.UpdateAsObservable().TakeUntil(_ => !toBeDeducted.CurrentValue).Subscribe(_ =>
                    {
                        elapsed += Time.deltaTime;
                        transform.rotation = Quaternion.Euler(0, 0, VibrateAngle(elapsed));
                    }, _ => transform.rotation = Quaternion.identity);
            });
        }

        private static float VibrateAngle(float t) => t % 1 < 0.5f ? 0 : Mathf.Sin(t * Mathf.PI * 16) * 15f;

        public void Reset()
        {
            Icon.enabled = true;
            transform.localScale = Vector3.one;
            transform.rotation = Quaternion.identity;
            toBeDeducted.Value = false;
            LayoutElement.ignoreLayout = false;
        }

        public async UniTask ScaleDown(float duration, CancellationToken token)
        {
            transform.localScale = Vector3.one;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                transform.localScale = Vector3.Lerp(transform.localScale, Vector3.zero, elapsed / duration);
                await UniTask.WaitForEndOfFrame(token);
            }
            transform.localScale = Vector3.zero;
        }

        public async UniTask ScaleUp(float duration, CancellationToken token)
        {
            transform.localScale = Vector3.zero;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one, elapsed / duration);
                await UniTask.WaitForEndOfFrame(token);
            }
            transform.localScale = Vector3.one;
        }
    }
}