using System;

namespace OAuth
{
    [Serializable]
    public struct OAuthLoginResponse
    {
        public bool success;
        public string sessionId;
        public string nickname;
        public bool isNewUser;
    }
}