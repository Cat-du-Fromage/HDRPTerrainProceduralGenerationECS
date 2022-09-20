using System.Runtime.CompilerServices;
using Unity.Mathematics;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using Vector4 = UnityEngine.Vector4;

namespace ProjectDawn.Mathematics
{
    /// <summary>
    /// A static class to contain various math functions.
    /// </summary>
    public static partial class math2
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 asvector3(this float2 value) => new Vector3(value.x, value.y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 asvector4(this float2 value) => new Vector4(value.x, value.y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 asvector4(this float3 value) => new Vector4(value.x, value.y, value.z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 asfloat3(this float2 value) => new float3(value.x, value.y, 0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 asfloat4(this float2 value) => new float4(value.x, value.y, 0, 0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 asfloat4(this float3 value) => new float4(value.x, value.y, value.z, 0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 asfloat(this Vector2 value) => value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 asfloat(this Vector3 value) => value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 asfloat(this Vector4 value) => value;
    }
}
