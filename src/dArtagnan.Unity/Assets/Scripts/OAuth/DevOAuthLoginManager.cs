using System;
using Cysharp.Threading.Tasks;
using dArtagnan.Shared;
using UnityEngine;
using UnityEngine.Networking;

namespace OAuth
{
    public static class DevOAuthLoginManager
    {
        public static async UniTask<OAuthLoginResponse> Login(string endpoint, string providerId)
        {
            var payload = new LoginRequest { providerId = providerId, clientVersion = Application.version };
            var serialized = JsonUtility.ToJson(payload);
            using var req = UnityWebRequest.Post(
                endpoint + "/login",
                serialized,
                "application/json");
            await req.SendWebRequest();
            if (req.result == UnityWebRequest.Result.Success)
                return JsonUtility.FromJson<OAuthLoginResponse>(req.downloadHandler.text);
            var parsed = JsonUtility.FromJson<ErrorResponse>(req.downloadHandler.text).message;
            throw new Exception(parsed);
        }
    }
}