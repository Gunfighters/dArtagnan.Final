using System;
#if UNITY_IOS
using Apple.GameKit;
#endif
using Cysharp.Threading.Tasks;
using Networking;
using UnityEngine;
using UnityEngine.Networking;

namespace OAuth
{
    public static class AppleOAuthManager
    {
    #if UNITY_IOS
        public static async UniTask<OAuthLoginResponse> Login(string endpoint)
        {
            if (!GKLocalPlayer.Local.IsAuthenticated)
                await GKLocalPlayer.Authenticate();
            var data = await FetchGameCenterData();
            var response = await SendAuthDataToServer(endpoint, data);
            return response;
        }

        private static async UniTask<AppleAuthData> FetchGameCenterData()
        {
            var localPlayer = GKLocalPlayer.Local;
            var fetchItemsResponse = await localPlayer.FetchItemsForIdentityVerificationSignature();
            return new AppleAuthData
            {
                teamPlayerId = localPlayer.TeamPlayerId,
                signature = Convert.ToBase64String(fetchItemsResponse.GetSignature()),
                salt = Convert.ToBase64String(fetchItemsResponse.GetSalt()),
                publicKeyUrl = fetchItemsResponse.PublicKeyUrl,
                timestamp = fetchItemsResponse.Timestamp.ToString(),
                clientVersion = Application.version
            };
        }

        private static async UniTask<OAuthLoginResponse> SendAuthDataToServer(string endpoint, AppleAuthData authData)
        {
            var serialized = JsonUtility.ToJson(authData);
            using var request = UnityWebRequest.Post($"{endpoint}/auth/apple/verify-identity", serialized,
                "application/json");
            await request.SendWebRequest().ToUniTask();

            if (request.result != UnityWebRequest.Result.Success)
            {
                var errorResponse = JsonUtility.FromJson<dArtagnan.Shared.ErrorResponse>(request.downloadHandler.text);
                throw new Exception(errorResponse.message);
            }

            var response = JsonUtility.FromJson<OAuthLoginResponse>(request.downloadHandler.text);
            if (!response.success) throw new Exception("Error fetching apple identity");
            return response;
        }
    #endif
    }

    public struct AppleAuthData
    {
        public string teamPlayerId;
        public string signature;
        public string salt;
        public string publicKeyUrl;
        public string timestamp;
        public string clientVersion;
    }
}