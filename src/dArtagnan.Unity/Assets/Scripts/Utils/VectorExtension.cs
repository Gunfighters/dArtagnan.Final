using System.Linq;
using UnityEngine;

namespace Utils
{
    public static class VectorExtension
    {
        public static UnityEngine.Vector2 ToUnityVec(this System.Numerics.Vector2 vec)
        {
            return new UnityEngine.Vector2(vec.X, vec.Y);
        }

        public static System.Numerics.Vector2 ToSystemVec(this UnityEngine.Vector2 vec)
        {
            return new System.Numerics.Vector2(vec.x, vec.y);
        }
        
        public static System.Numerics.Vector2 ToSystemVec(this UnityEngine.Vector3 vec)
        {
            return ToSystemVec(new Vector2(vec.x, vec.y));
        }

        public static UnityEngine.Vector2 SnapToCardinalDirection(this UnityEngine.Vector3 dir)
        {
            return dir.x switch
            {
                > 0 => UnityEngine.Vector2.right,
                < 0 => UnityEngine.Vector2.left,
                _ => dir.y switch
                {
                    > 0 => UnityEngine.Vector2.up,
                    < 0 => UnityEngine.Vector2.down,
                    _ => UnityEngine.Vector2.zero
                }
            };
        }

        public static UnityEngine.Vector2 SnapToCardinalDirection(this UnityEngine.Vector2 dir)
        {
            return SnapToCardinalDirection(new UnityEngine.Vector3(dir.x, dir.y, 0));
        }
    }
}