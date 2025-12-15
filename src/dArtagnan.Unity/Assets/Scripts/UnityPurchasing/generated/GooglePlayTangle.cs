// WARNING: Do not modify! Generated file.

namespace UnityEngine.Purchasing.Security {
    public class GooglePlayTangle
    {
        private static byte[] data = System.Convert.FromBase64String("3Wb6aQcOpsp3YrSv/DZGNJna8OQSFBmhK/L3/2kJGdl7hietaEqatJIgo4CSr6SriCTqJFWvo6Ojp6KhZeW1C4X/2RS8bTVsHoYL33ak+p5Feo3PAZy4u8b+XaaTPFnVwyphnGLEXScooqRxMvVHidhflROKEkmXyLzS07hbEG0WeJa84ScDd1Ugmq7J3tuqNkqZLfHEcMx6RY9qTILMXKGoko5dbzHuiUtwuUBokEFyz7GXqnZQM54A5uzBYZW1l4jGdetPkI4go62ikiCjqKAgo6OiFvdA3H+1olc2pHZCFmlIUZ8g0pgIal7Opty0cWeo+Hg4kr286bMJ+szJ2okJz5Siwa4+o4p0ovYmgBCW1P21d+w0zjp4kZwRlS85s6Cho6Kj");
        private static int[] order = new int[] { 12,13,12,3,11,11,12,10,12,10,13,13,12,13,14 };
        private static int key = 162;

        public static readonly bool IsPopulated = true;

        public static byte[] Data() {
        	if (IsPopulated == false)
        		return null;
            return Obfuscator.DeObfuscate(data, order, key);
        }
    }
}
