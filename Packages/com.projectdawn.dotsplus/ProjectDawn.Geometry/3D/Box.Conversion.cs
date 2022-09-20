#if !UNITY_DOTSPLAYER
using UnityEngine;

namespace ProjectDawn.Geometry3D
{
    public partial struct Box
    {
        public static implicit operator Bounds(Box v) => new Bounds(v.Center, v.Size);
        public static implicit operator Box(Bounds v) => new Box(v.min, v.size);
    }
}
#endif