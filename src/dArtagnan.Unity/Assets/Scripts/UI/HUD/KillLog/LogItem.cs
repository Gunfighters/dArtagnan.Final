using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace UI.HUD.KillLog
{
    public abstract class LogItem : MonoBehaviour
    {
        public CancellationTokenSource Cts = new();
        private readonly Dictionary<Graphic, Color> _colorsOriginal = new();

        private void Awake()
        {
            foreach (var g in GetComponentsInChildren<Graphic>(true))
            {
                _colorsOriginal[g] = g.color;
            }
        }

        public void Reset()
        {
            Cts.Cancel();
            foreach (var g in _colorsOriginal)
            {
                g.Key.color = g.Value;
            }
            Cts = new CancellationTokenSource();
        }

        public async UniTask FadeOut(float duration)
        {
            var elapsed = 0f;
            while (elapsed <= duration)
            {
                await UniTask.WaitForEndOfFrame(cancellationToken: Cts.Token);
                foreach (var g in _colorsOriginal.Keys)
                    g.color = Color.Lerp(g.color, Color.clear, elapsed / duration);
                elapsed += Time.deltaTime;
            }
            Cts.Token.ThrowIfCancellationRequested();
            gameObject.SetActive(false);
        }
    }
}