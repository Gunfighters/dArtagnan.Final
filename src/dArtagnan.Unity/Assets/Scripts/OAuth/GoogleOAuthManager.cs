#if UNITY_ANDROID
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif
using System.Text;
using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using UnityEngine.Networking;

namespace OAuth
{
    public static class GoogleOAuthManager
    {
        #if UNITY_ANDROID
        public static async UniTask<OAuthLoginResponse> Login(string endpoint)
        {
            var tcs = new UniTaskCompletionSource<OAuthLoginResponse>();
            PlayGamesPlatform.Activate();
            PlayGamesPlatform.Instance.Authenticate(status =>
            {
                if (status == SignInStatus.Success)
                {
                    PlayGamesPlatform.Instance.RequestServerSideAccess(true, async code =>
                    {
                        try
                        {
                            var result = await SendAuthCodeToServer(endpoint, code);
                            tcs.TrySetResult(result);
                        }
                        catch (Exception e)
                        {
                            tcs.TrySetException(e);
                        }
                    });
                }
                else
                {
                    tcs.TrySetException(new Exception($"Error authenticating GPGS: {status}"));
                }
            });
            return await tcs.Task;
        }

        private static async UniTask<OAuthLoginResponse> SendAuthCodeToServer(string endpoint, string code)
        {
            var payload = new OAuthTokenPayload { token = code, clientVersion = Application.version };
            var serialized = JsonUtility.ToJson(payload);
            using var req = UnityWebRequest.Post($"{endpoint}/auth/google/verify-token", serialized, "application/json");
            await req.SendWebRequest().ToUniTask();

            if (req.result != UnityWebRequest.Result.Success)
            {
                var errorResponse = JsonUtility.FromJson<dArtagnan.Shared.ErrorResponse>(req.downloadHandler.text);
                throw new Exception(errorResponse.message);
            }

            var response = JsonUtility.FromJson<OAuthLoginResponse>(req.downloadHandler.text);
            return response;
        }
        #endif
    }
}

[Serializable]
public struct OAuthTokenPayload
{
    public string token;
    public string clientVersion;
}