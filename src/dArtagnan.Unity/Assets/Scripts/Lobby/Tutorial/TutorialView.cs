using Audio;
using Networking;
using R3;
using R3.Triggers;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Lobby.Tutorial
{
    public class TutorialView : MonoBehaviour, IPointerClickHandler
    {
        public Transform[] contents;
        public SerializableReactiveProperty<int> index;
        public Image leftArrow;
        public Image rightArrow;
        private float _elapsedTime;
        public Slider progressSlider;
        public TextMeshProUGUI progressText;

        private void Awake()
        {
            progressSlider.value = 1;
            progressSlider.maxValue = contents.Length;
            index.Subscribe(i =>
            {
                for (var j = 0; j < contents.Length; j++)
                    contents[j].gameObject.SetActive(j == i);
                leftArrow.enabled = i > 0;
                progressText.text = $"{i + 1} / {contents.Length}";
                var elapsed = 0f;
                progressSlider.UpdateAsObservable()
                    .TakeUntil(_ => Mathf.Approximately(progressSlider.value, i + 1))
                    .Subscribe(_ =>
                        {
                            elapsed += Time.deltaTime;
                            progressSlider.value = Mathf.Lerp(progressSlider.value, i + 1, elapsed);
                        },
                        _ => progressSlider.value = i + 1);
            });
        }

        private void OnEnable()
        {
            index.Value = 0;
            _elapsedTime = 0;
        }

        private void Update()
        {
            _elapsedTime += Time.deltaTime;
            var a = Mathf.Cos(_elapsedTime / Mathf.PI * 9) / 3 + 0.66f;
            var color = leftArrow.color;
            color.a = a;
            leftArrow.color = color;
            rightArrow.color = color;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            AudioClipPlayer.Instance.Play(AudioClipType.ButtonClicked);
            if (eventData.position.x < Screen.width / 2f)
            {
                if (index.Value > 0)
                    index.Value--;
            }
            else
            {
                if (index.Value >= contents.Length - 1)
                {
                    WebsocketManager.Instance.showTutorial.Value = false;
                    LobbySceneScreenManager.Instance.currentScreen.Value = LobbyCanvasScreenType.Lobby;
                }
                else
                    index.Value++;
            }
        }
    }
}