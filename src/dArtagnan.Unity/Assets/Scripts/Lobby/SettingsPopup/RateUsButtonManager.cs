using Cysharp.Threading.Tasks;
using UnityEngine;
#if UNITY_IOS
using UnityEngine.iOS;
#elif UNITY_ANDROID
using Google.Play.Common;
using Google.Play.Review;
#endif
using UnityEngine.UI;

namespace Lobby.SettingsPopup
{
    public class RateUsButtonManager : MonoBehaviour
    {
        private Button _btn;
        #if UNITY_ANDROID
        private ReviewManager _reviewManager;
        private PlayAsyncOperation<PlayReviewInfo, ReviewErrorCode> _reviewInfoOp;
        #endif
        private void Awake()
        {
            _btn = GetComponent<Button>();
            #if UNITY_IOS
            _btn.onClick.AddListener(RequestReviewIOS);
            #elif UNITY_ANDROID
            _reviewManager = new ReviewManager();
            _btn.onClick.AddListener(() => RequestReviewAndroid().Forget());
            #endif
        }

        #if UNITY_ANDROID
        private void OnEnable()
        {
            _reviewInfoOp = _reviewManager.RequestReviewFlow();
        }
        #endif

        #if UNITY_IOS
        private void RequestReviewIOS()
        {
            if (!Device.RequestStoreReview())
                Debug.LogError("iOS version too low or StoreKit framework was not linked.");
        }
        #endif

        #if UNITY_ANDROID
        private async UniTask RequestReviewAndroid()
        {
            await _reviewInfoOp;
            var info = _reviewInfoOp.GetResult();
            var launchFlowOp = _reviewManager.LaunchReviewFlow(info);
            await launchFlowOp;
        }
        #endif
    }
}